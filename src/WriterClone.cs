using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Dec
{
    internal class WriterClone
    {
        private WriterUtil.PendingWriteCoordinator pendingWriteCoordinator = new WriterUtil.PendingWriteCoordinator();

        public void RegisterPendingWrite(Action action)
        {
            pendingWriteCoordinator.RegisterPendingWrite(action);
        }

        public void FinalizePendingWrites()
        {
            pendingWriteCoordinator.DequeuePendingWrites();
        }

        public bool AllowReflection { get => false; }
        public Recorder.IUserSettings UserSettings { get; }

        internal Dictionary<object, object> cloneReferences = new Dictionary<object, object>();

        public WriterClone(Recorder.IUserSettings userSettings)
        {
            this.UserSettings = userSettings;
        }

        public WriterNodeClone StartClone(Type type)
        {
            return WriterNodeClone.StartClone(this, type);
        }
    }

    // this process is pretty complicated
    // WriterNodeClone basically acts as a storage for all the data we need to clone something, which is, in some cases, accumulated gradually and not immediately
    // once it's all ready, *then* we can call GetResult() and get the actual clone, decorated with all its childrens' clones
    // thing is, this is a lazy process, so calling GetResult() also does all the work for its children
    // then we need some somewhat awkward ReaderNode subclasses for "this node, with children" and "the specific child that was requested"
    // I get the feeling that this implies a bunch of interfaces should be cleaned up, but I'm not doing that right now, so
    internal class WriterNodeClone : WriterNode
    {
        private WriterClone writer;

        // the object that we've been told to fill, and its eventual filled version
        private object model;
        private bool modelSet = false;

        // the object that we've been passed for cloning
        private object original;
        private Converter originalConverter;
        private bool originalSet = false;

        // our duplicate of original
        private object result;
        private bool resultReady = false;
        private bool resultIsValuelike;

        // Represents only the *active* depth in the program stack.
        // This is kind of painfully hacky, because when it's created, we don't know if it's going to represent a new stack start.
        // So we just kinda adjust it as we go.
        private int depth;
        private const int MaxRecursionDepth = 100;

        private List<(string key, WriterNodeClone value)> recorderChildren;

        public override bool AllowReflection { get => writer.AllowReflection; }
        public override bool AllowDecPath { get => true; }
        public override bool AllowAsThis { get => false; }
        public override bool AllowCloning { get => true;  }
        public override Recorder.Purpose Intent { get => Recorder.Purpose.Cloning; }
        public override Recorder.IUserSettings UserSettings { get => writer.UserSettings; }

        private WriterNodeClone(WriterClone writer, int depth, Recorder.Settings settings, Path path) : base(settings, path)
        {
            this.writer = writer;
            this.depth = depth;
        }

        internal void SetModel(object model)
        {
            if (modelSet)
            {
                Dbg.Err("Internal error: WriterNodeClone.SetModel() called twice");
            }

            // if it's valuelike, we already have the result, which is fine
            if (resultReady && !resultIsValuelike)
            {
                Dbg.Err("Internal error: WriterNodeClone.SetModel() called after result was ready");
            }

            this.model = model;
            modelSet = true;
        }

        private void SetOriginal(object original, Converter converter = null)
        {
            if (originalSet)
            {
                Dbg.Err("Internal error: WriterNodeClone.SetOriginal() called twice");
            }

            if (resultReady)
            {
                Dbg.Err("Internal error: WriterNodeClone.SetOriginal() called after result was ready");
            }

            this.original = original;
            this.originalConverter = converter;
            originalSet = true;
        }

        // perf hack for simple things
        private void SetValuelikeOriginalAndResult(object original)
        {
            if (originalSet)
            {
                Dbg.Err("Internal error: WriterNodeClone.SetOriginalAndResult() called after original set");
            }

            if (resultReady)
            {
                Dbg.Err("Internal error: WriterNodeClone.SetOriginalAndResult() called after result was ready");
            }

            this.original = original;
            originalSet = true;
            this.result = original;
            resultReady = true;
            resultIsValuelike = true;
        }

        public object GetResult(bool sharable, Type intendedType)
        {
            if (!resultReady)
            {
                if (!originalSet)
                {
                    Dbg.Err("Internal error: WriterNodeClone.GetResult() called before original was set");
                    // we'll just "clone" null, I guess
                }

                CreateResult(sharable, intendedType);

                resultReady = true;
            }

            return result;
        }

        private void CreateResult(bool sharable, Type intendedType)
        {
            // this is kind of copied from serialization
            if (original == null)
            {
                // it probably already was
                result = null;
                return;
            }

            // see if we already have a reference
            if (writer.cloneReferences.TryGetValue(original, out var clone))
            {
                result = clone;
                return;
            }

            // make our object
            // tuples are sort of busted and we're going to punt on them for now
            // this needs to deal with anything that has complicated constructor behavior
            var originalType = original.GetType();

            // make sure originalType can be converted to intendedType
            if (!intendedType.IsAssignableFrom(originalType))
            {
                Dbg.Err($"Attempting to clone type {originalType} into {intendedType}; this is currently not supported (ask me on Discord if you need it)");
                result = null;
                return;
            }

            bool done = false;
            if (UtilType.CanBeCloneCopied(originalType))
            {
                // okay
                result = original;
                done = true;
            }
            else if (originalConverter is ConverterFactory converterFactory)
            {
                // this calls CreateRecorderChild a bunch and fills it out
                converterFactory.WriteObj(original, new RecorderWriter(this));

                // now we create the object itself
                var readerClone = new ReaderNodeCloneRecorder(recorderChildren, UserSettings);
                result = converterFactory.CreateObj(new RecorderReader(readerClone, new ReaderGlobals()));
            }
            else if (originalConverter is ConverterString converterString)
            {
                var str = converterString.WriteObj(original);
                result = converterString.ReadObj(str, new Context(filename: "clone"));
                done = true;
            }
            else if (RecorderSettings.factories != null && original is IRecordable)
            {
                result = RecorderSettings.CreateRecordableFromFactory(originalType, "clone", new ReaderNodeCloneCreator(original, UserSettings));
            }
            else if ((model == null || model.GetType() != original.GetType()) && !typeof(ITuple).IsAssignableFrom(originalType))
            {
                // derive an appropriate type; we're just yanking this out of the original type right now (is this always right?)
                // this is kind of awful in terms of perf ;.;
                result = original.GetType().CreateInstanceSafe("recordable", new ReaderNodeCloneCreator(original, UserSettings));
            }
            else
            {
                // re-use!
                result = model;
            }

            // put this in first so we can use the reference if we need it through the recursion
            // this probably does not work at all for tuple.
            writer.cloneReferences[original] = result;

            // at this point we have the right object even if we don't have enough room on the stack
            // although that's not as useful if this is a struct
            bool deferred = false;
            if (!done)
            {
                bool doPending = false;
                if (depth > 20 && !result.GetType().IsValueType && sharable)
                {
                    doPending = true;
                }
                else if (depth > 100)
                {
                    Dbg.Err("Depth limiter ran into an unshareable node stack that's too deep. Recommend using more `.Shared()` calls to allow for stack splitting. This may cause data corruption.");
                    doPending = true;
                }
                else if (depth > 50)
                {
                    // this is currently going to be way too spammy, we'll fix it later
                    Dbg.Wrn("Depth limiter is running into an unshareable node stack that's too deep. Recommend using more `.Shared()` calls to allow for stack splitting.");
                }

                if (doPending)
                {
                    writer.RegisterPendingWrite(() =>
                    {
                        CreateResult_Resolve(originalType, resetDepth: true);
                        (original as IPostCloneOriginal)?.PostCloneOriginal();
                        (result as IPostCloneNew)?.PostCloneNew();
                    });
                    deferred = true;
                }
                else
                {
                    CreateResult_Resolve(originalType);
                }
            }

            if (!deferred)
            {
                (original as IPostCloneOriginal)?.PostCloneOriginal();
                (result as IPostCloneNew)?.PostCloneNew();
            }

            // this is a hacky way of getting around the Tuple problem. this should really be fixed.
            writer.cloneReferences[original] = result;
        }

        private void DoArrayRecursive(Array original, Array result, int[] dimensions, int[] index, int rank, bool resetDepth)
        {
            for (int i = 0; i < dimensions[rank]; ++i)
            {
                index[rank] = i;

                if (rank == dimensions.Length - 1)
                {
                    // we're at the bottom, just copy the value
                    result.SetValue(CloneChild(original.GetValue(index), resetDepth), index);
                }
                else
                {
                    // recurse
                    DoArrayRecursive(original, result, dimensions, index, rank + 1, resetDepth);
                }
            }
        }

        internal static ConcurrentDictionary<Type, Action<WriterNodeClone, bool>> ResolveStrategyCache = new ConcurrentDictionary<Type, Action<WriterNodeClone, bool>>();

        private static Action<WriterNodeClone, bool> BuildResolveStrategy(Type valType)
        {
            if (valType.IsArray)
            {
                bool elementCanBeCloneCopied = UtilType.CanBeCloneCopied(valType.GetElementType());

                if (elementCanBeCloneCopied)
                {
                    return (self, resetDepth) =>
                    {
                        // if the array members are valuelike, we can just copy the whole thing
                        Array.Copy(self.original as Array, self.result as Array, (self.original as Array).Length);
                    };
                }
                else
                {
                    return (self, resetDepth) =>
                    {
                        var originalArray = self.original as Array;
                        var resultArray = self.result as Array;
                        int[] dimensions = UtilType.GetArrayDimensions(originalArray);
                        int[] index = new int[originalArray.Rank];
                        self.DoArrayRecursive(originalArray, resultArray, dimensions, index, 0, resetDepth);
                    };
                }
            }

            if (typeof(IRecordable).IsAssignableFrom(valType))
            {
                return (self, resetDepth) =>
                {
                    if (self.result == null)
                    {
                        // we have presumably already printed an error explaining why we can't create this class, so just give up
                        return;
                    }

                    // this calls CreateRecorderChild a bunch and fills it out
                    (self.original as IRecordable).Record(new RecorderWriter(self));

                    var readerClone = new ReaderNodeCloneRecorder(self.recorderChildren, self.UserSettings);

                    // do the dupe
                    var resultAsIRecordable = self.result as IRecordable;
                    var recorderReader = new RecorderReader(readerClone, new ReaderGlobals());
                    try
                    {
                        resultAsIRecordable.Record(recorderReader);
                    }
                    catch (Exception e)
                    {
                        Dbg.Ex(e);
                    }
                };
            }

            if (typeof(IList).IsAssignableFrom(valType))
            {
                var listElementType = valType.GetGenericInterfaceArguments(typeof(IList<>))[0];
                bool isExactList = valType.IsGenericType && valType.GetGenericTypeDefinition() == typeof(List<>);
                var versionField = isExactList ? valType.GetField("_version", BindingFlags.Instance | BindingFlags.NonPublic) : null;

                // if the list members are valuelike, we can just copy the whole thing
                if (UtilType.CanBeCloneCopied(listElementType))
                {
                    if (isExactList)
                    {
                        var addRangeMethod = valType.GetMethod("AddRange");

                        return (self, resetDepth) =>
                        {
                            var originalList = self.original as IList;
                            var resultList = self.result as IList;

                            // just in case; maybe we should be reusing originals as models?
                            resultList.Clear();

                            // use AddRange to copy
                            addRangeMethod.Invoke(resultList, new object[] { originalList });

                            versionField.SetValue(resultList, Util.CollectionDeserializationVersion);
                        };
                    }
                    else
                    {
                        return (self, resetDepth) =>
                        {
                            var originalList = self.original as IList;
                            var resultList = self.result as IList;

                            // just in case; maybe we should be reusing originals as models?
                            resultList.Clear();

                            for (int i = 0; i < originalList.Count; i++)
                            {
                                resultList.Add(originalList[i]);
                            }
                        };
                    }
                }
                else
                {
                    return (self, resetDepth) =>
                    {
                        var originalList = self.original as IList;
                        var resultList = self.result as IList;

                        // just in case; maybe we should be reusing originals as models?
                        resultList.Clear();

                        for (int i = 0; i < originalList.Count; i++)
                        {
                            resultList.Add(self.CloneChild(originalList[i], resetDepth));
                        }

                        versionField?.SetValue(resultList, Util.CollectionDeserializationVersion);
                    };
                }
            }

            if (typeof(IDictionary).IsAssignableFrom(valType))
            {
                var dictArgs = valType.GetGenericInterfaceArguments(typeof(IDictionary<,>));
                bool canCloneKey = UtilType.CanBeCloneCopied(dictArgs[0]);
                bool canCloneValue = UtilType.CanBeCloneCopied(dictArgs[1]);

                // if the dictionary members are valuelike, we can just copy the whole thing
                // skipping the tests is important enough that we'll just specialcase the various options
                if (canCloneKey && canCloneValue)
                {
                    return (self, resetDepth) =>
                    {
                        var originalDict = self.original as IDictionary;
                        var resultDict = self.result as IDictionary;
                        resultDict.Clear();
                        foreach (DictionaryEntry kvp in originalDict)
                        {
                            resultDict[kvp.Key] = kvp.Value;
                        }
                    };
                }
                else if (canCloneKey)
                {
                    return (self, resetDepth) =>
                    {
                        var originalDict = self.original as IDictionary;
                        var resultDict = self.result as IDictionary;
                        resultDict.Clear();
                        foreach (DictionaryEntry kvp in originalDict)
                        {
                            resultDict[kvp.Key] = self.CloneChild(kvp.Value, resetDepth);
                        }
                    };
                }
                else if (canCloneValue)
                {
                    return (self, resetDepth) =>
                    {
                        var originalDict = self.original as IDictionary;
                        var resultDict = self.result as IDictionary;
                        resultDict.Clear();
                        foreach (DictionaryEntry kvp in originalDict)
                        {
                            resultDict[self.CloneChild(kvp.Key, resetDepth)] = kvp.Value;
                        }
                    };
                }
                else
                {
                    return (self, resetDepth) =>
                    {
                        var originalDict = self.original as IDictionary;
                        var resultDict = self.result as IDictionary;
                        resultDict.Clear();
                        foreach (DictionaryEntry kvp in originalDict)
                        {
                            resultDict[self.CloneChild(kvp.Key, resetDepth)] = self.CloneChild(kvp.Value, resetDepth);
                        }
                    };
                }
            }

            if (valType.ImplementsGenericInterface(typeof(ISet<>)))
            {
                var setElementType = valType.GetGenericInterfaceArguments(typeof(ISet<>))[0];
                var clearSet = UtilCollectionReflect.SetClear(setElementType);
                var addSet = UtilCollectionReflect.SetAdd(setElementType);

                // if the set members are valuelike, we can just copy the whole thing
                if (UtilType.CanBeCloneCopied(setElementType))
                {
                    return (self, resetDepth) =>
                    {
                        var originalSet = self.original as IEnumerable;
                        clearSet(self.result);
                        foreach (var item in originalSet)
                        {
                            addSet(self.result, item);
                        }
                    };
                }
                else
                {
                    return (self, resetDepth) =>
                    {
                        var originalSet = self.original as IEnumerable;
                        clearSet(self.result);
                        foreach (var item in originalSet)
                        {
                            addSet(self.result, self.CloneChild(item, resetDepth));
                        }
                    };
                }
            }

            if (valType.IsGenericType)
            {
                var genericTypeDefinition = valType.GetGenericTypeDefinition();

                if (genericTypeDefinition == typeof(Queue<>))
                {
                    var queueElementType = valType.GetGenericArguments()[0];
                    var clearQueue = UtilCollectionReflect.QueueClear(queueElementType);
                    var enqueue = UtilCollectionReflect.QueueEnqueue(queueElementType);

                    // if the queue members are valuelike, we can just copy the whole thing
                    if (UtilType.CanBeCloneCopied(queueElementType))
                    {
                        return (self, resetDepth) =>
                        {
                            var originalQueue = self.original as IEnumerable;
                            clearQueue(self.result);
                            // there might be a faster way to do this?
                            foreach (var item in originalQueue)
                            {
                                enqueue(self.result, item);
                            }
                        };
                    }
                    else
                    {
                        return (self, resetDepth) =>
                        {
                            var originalQueue = self.original as IEnumerable;
                            clearQueue(self.result);
                            foreach (var item in originalQueue)
                            {
                                enqueue(self.result, self.CloneChild(item, resetDepth));
                            }
                        };
                    }
                }

                if (genericTypeDefinition == typeof(Stack<>))
                {
                    var stackElementType = valType.GetGenericArguments()[0];
                    var clearStack = UtilCollectionReflect.StackClear(stackElementType);
                    var push = UtilCollectionReflect.StackPush(stackElementType);

                    return (self, resetDepth) =>
                    {
                        var originalStack = self.original as IEnumerable;
                        var tempStack = new Stack<object>();

                        // just in case; maybe we should be reusing originals as models?
                        clearStack(self.result);

                        foreach (var item in originalStack)
                        {
                            tempStack.Push(self.CloneChild(item, resetDepth));
                        }

                        clearStack(self.result);

                        while (tempStack.Count > 0)
                        {
                            push(self.result, tempStack.Pop());
                        }
                    };
                }

                if (genericTypeDefinition == typeof(Tuple<>) ||
                    genericTypeDefinition == typeof(Tuple<,>) ||
                    genericTypeDefinition == typeof(Tuple<,,>) ||
                    genericTypeDefinition == typeof(Tuple<,,,>) ||
                    genericTypeDefinition == typeof(Tuple<,,,,>) ||
                    genericTypeDefinition == typeof(Tuple<,,,,,>) ||
                    genericTypeDefinition == typeof(Tuple<,,,,,,>) ||
                    genericTypeDefinition == typeof(Tuple<,,,,,,,>))
                {
                    var properties = valType.GetProperties();

                    return (self, resetDepth) =>
                    {
                        var tupleItems = properties.Select(prop => prop.GetValue(self.original)).Select(item => self.CloneChild(item, resetDepth)).ToArray();
                        self.result = Activator.CreateInstance(valType, tupleItems);
                    };
                }

                if (genericTypeDefinition == typeof(ValueTuple<>) ||
                    genericTypeDefinition == typeof(ValueTuple<,>) ||
                    genericTypeDefinition == typeof(ValueTuple<,,>) ||
                    genericTypeDefinition == typeof(ValueTuple<,,,>) ||
                    genericTypeDefinition == typeof(ValueTuple<,,,,>) ||
                    genericTypeDefinition == typeof(ValueTuple<,,,,,>) ||
                    genericTypeDefinition == typeof(ValueTuple<,,,,,,>) ||
                    genericTypeDefinition == typeof(ValueTuple<,,,,,,,>))
                {
                    var fields = valType.GetFields();

                    return (self, resetDepth) =>
                    {
                        var valueTupleItems = fields.Select(field => field.GetValue(self.original)).Select(item => self.CloneChild(item, resetDepth)).ToArray();
                        self.result = Activator.CreateInstance(valType, valueTupleItems);
                    };
                }
            }

            // something went wrong
            return (self, resetDepth) =>
            {
                Dbg.Err($"Internal error: Failed to clone object of type {valType}");
            };
        }

        private void CreateResult_Resolve(Type valType, bool resetDepth = false)
        {
            if (originalConverter != null)
            {
                // Converter dispatch is instance-dependent (not cacheable by type)
                if (originalConverter is ConverterString converterString)
                {
                    // there's kind of not a lot we can do here to speed it up unfortunately
                    result = converterString.ReadObj(converterString.WriteObj(original), new Context(filename: "clone"));
                }
                else if (originalConverter is ConverterRecord converterRecord)
                {
                    // this calls CreateRecorderChild a bunch and fills it out
                    converterRecord.RecordObj(original, new RecorderWriter(this));

                    var readerClone = new ReaderNodeCloneRecorder(recorderChildren, UserSettings);

                    // object already exists
                    result = converterRecord.RecordObj(result, new RecorderReader(readerClone, new ReaderGlobals()));
                }
                else if (originalConverter is ConverterFactory converterFactory)
                {
                    // the rest of this was done earlier
                    var readerClone = new ReaderNodeCloneRecorder(recorderChildren, UserSettings);
                    result = converterFactory.ReadObj(result, new RecorderReader(readerClone, new ReaderGlobals()));
                }
                else
                {
                    throw new NotImplementedException();
                }

                return;
            }

            if (!ResolveStrategyCache.TryGetValue(valType, out var strategy))
            {
                strategy = BuildResolveStrategy(valType);
                ResolveStrategyCache[valType] = strategy;
            }

            strategy(this, resetDepth);
        }

        public static WriterNodeClone StartClone(WriterClone writer, Type type)
        {
            return new WriterNodeClone(writer, 0, new Recorder.Settings() { shared = Recorder.Settings.Shared.Flexible }, new PathRoot("RECORD"));
        }

        public override WriterNode CreateRecorderChild(string label, Recorder.Settings settings)
        {
            if (recorderChildren == null)
            {
                recorderChildren = new List<(string, WriterNodeClone)>();
            }

            var child = new WriterNodeClone(writer, depth + 1, settings, new PathMember(Path, label));
            recorderChildren.Add((label, child));
            return child;
        }

        public override WriterNode CreateReflectionChild(System.Reflection.FieldInfo field, Recorder.Settings settings)
        {
            // Not supported.
            Dbg.Err("Internal error: WriterNodeClone attempted to create reflection child");
            return null;
        }

        public object CloneChild(object obj, bool resetDepth)
        {
            if (obj == null)
            {
                // okay
                return obj;
            }
            // maybe I should set up more value-type-ish special cases here?

            var objType = obj.GetType();
            var child = new WriterNodeClone(writer, resetDepth ? 0 : depth + 1, RecorderSettings.CreateChild(), Path);
            Serialization.ComposeElement(child, obj, objType);
            return child.GetResult(false, objType);
        }

        public override void WritePrimitive(object value)
        {
            SetValuelikeOriginalAndResult(value);
        }

        public override void WriteEnum(object value)
        {
            SetValuelikeOriginalAndResult(value);
        }

        public override void WriteString(string value)
        {
            SetValuelikeOriginalAndResult(value);
        }

        public override void WriteType(Type value)
        {
            SetValuelikeOriginalAndResult(value);
        }

        public override void WriteDec(Dec value)
        {
            SetValuelikeOriginalAndResult(value);
        }

        public override void WriteDecPathRef(object value)
        {
            SetValuelikeOriginalAndResult(value);
        }

        public override void TagClass(Type type)
        {
            // we don't actually care about this, the clone system finds the difference between fields-as-set and fields-as-expected to be pointless
        }

        public override void WriteExplicitNull()
        {
            FlagAsNull();

            SetValuelikeOriginalAndResult(null);
        }

        public override bool WriteReference(object value, Path path)
        {
            if (writer.cloneReferences.TryGetValue(value, out var clone))
            {
                SetValuelikeOriginalAndResult(clone);
                return true;
            }

            return false;
        }

        public override void WriteArray(Array value)
        {
            SetOriginal(value);
        }

        public override void WriteByteArray(byte[] value)
        {
            SetOriginal(value);
        }

        public override void WriteList(IList value)
        {
            SetOriginal(value);
        }

        public override void WriteDictionary(IDictionary value)
        {
            SetOriginal(value);
        }

        public override void WriteHashSet(IEnumerable value)
        {
            SetOriginal(value);
        }

        public override void WriteQueue(IEnumerable value)
        {
            SetOriginal(value);
        }

        public override void WriteStack(IEnumerable value)
        {
            SetOriginal(value);
        }

        public override void WriteTuple(object value, System.Runtime.CompilerServices.TupleElementNamesAttribute names)
        {
            SetOriginal(value);
        }

        public override void WriteValueTuple(object value, System.Runtime.CompilerServices.TupleElementNamesAttribute names)
        {
            SetOriginal(value);
        }

        public override void WriteRecord(IRecordable value)
        {
            SetOriginal(value);
        }

        public override void WriteConvertible(Converter converter, object value)
        {
            SetOriginal(value, converter);
        }

        public override void WriteCloneCopy(object value)
        {
            SetOriginal(value);
        }

        public override void WriteError()
        {
            // this exists just to avoid an internal error
            SetOriginal(null);
        }
    }

    internal class ReaderNodeCloneRecorder : ReaderNode
    {
        public override bool AllowAsThis { get => false; }
        public override Recorder.Purpose Intent { get => Recorder.Purpose.Cloning; }
        public override Recorder.IUserSettings UserSettings { get; }

        private List<(string key, WriterNodeClone value)> recorderChildren;
        private int searchHint = 0;
        public ReaderNodeCloneRecorder(List<(string key, WriterNodeClone value)> recorderChildren, Recorder.IUserSettings userSettings)
        {
            this.UserSettings = userSettings;
            this.recorderChildren = recorderChildren;
        }

        public override Context GetContext()
        {
            return new Context(filename: "clone");
        }

        public override int[] GetArrayDimensions(int rank)
        {
            // not valid, this is used only for recorders
            throw new NotImplementedException();
        }

        public override ReaderNode GetChildNamed(string name)
        {
            int count = recorderChildren.Count;

            // Fast path: check the hint position (read order usually matches write order)
            if (searchHint < count)
            {
                var hintEntry = recorderChildren[searchHint];
                if (hintEntry.key == name)
                {
                    if (hintEntry.value == null)
                    {
                        Dbg.Err($"Clone child {name} accessed twice; this is probably an attempt to record the same field twice, which is not supported");
                        return null;
                    }

                    var result = new ReaderNodeCloneRecorderItem(hintEntry.value, UserSettings);
                    recorderChildren[searchHint] = (name, null);
                    searchHint++;
                    return result;
                }
            }

            // Slow path: linear scan
            for (int i = 0; i < count; i++)
            {
                var entry = recorderChildren[i];
                if (entry.key == name)
                {
                    if (entry.value == null)
                    {
                        Dbg.Err($"Clone child {name} accessed twice; this is probably an attempt to record the same field twice, which is not supported");
                        return null;
                    }

                    var result = new ReaderNodeCloneRecorderItem(entry.value, UserSettings);
                    recorderChildren[i] = (name, null);
                    searchHint = i + 1;
                    return result;
                }
            }

            return null;
        }
        public override string[] GetAllChildren()
        {
            var result = new string[recorderChildren.Count];
            for (int i = 0; i < recorderChildren.Count; i++)
            {
                result[i] = recorderChildren[i].key;
            }
            return result;
        }
        public override bool HasText()
        {
            // not valid, this is used only for recorders
            throw new NotImplementedException();
        }

        public override object ParseElement(Type type, object model, ReaderGlobals readerGlobals, Recorder.Settings recorderSettings)
        {
            // not valid, this is used only for recorders
            throw new NotImplementedException();
        }
    }

    internal class ReaderNodeCloneRecorderItem : ReaderNode
    {
        public override bool AllowAsThis { get => false; }
        public override Recorder.Purpose Intent { get => Recorder.Purpose.Cloning; }
        public override Recorder.IUserSettings UserSettings { get; }

        private WriterNodeClone item;
        public ReaderNodeCloneRecorderItem(WriterNodeClone item, Recorder.IUserSettings userSettings)
        {
            this.UserSettings = userSettings;
            this.item = item;
        }

        public override Context GetContext()
        {
            return new Context(filename: "clone");
        }

        public override int[] GetArrayDimensions(int rank)
        {
            throw new NotImplementedException();
        }

        public override ReaderNode GetChildNamed(string name)
        {
            // not valid, this is used only for recorders
            throw new NotImplementedException();
        }
        public override string[] GetAllChildren()
        {
            // not valid, this is used only for recorders
            throw new NotImplementedException();
        }
        public override bool HasText()
        {
            // not valid, this is used only for recorders
            throw new NotImplementedException();
        }

        public override object ParseElement(Type type, object model, ReaderGlobals readerGlobals, Recorder.Settings recorderSettings)
        {
            // we actually just ignore the type right now, we copy off the original

            item.SetModel(model);
            return item.GetResult(recorderSettings.shared != Recorder.Settings.Shared.Deny, type);
        }
    }

    internal class ReaderNodeCloneCreator : ReaderNode
    {
        public override Recorder.IUserSettings UserSettings { get; }
        public override Recorder.Purpose Intent { get => Recorder.Purpose.Cloning; }

        private object original;
        public ReaderNodeCloneCreator(object original, Recorder.IUserSettings userSettings)
        {
            this.UserSettings = userSettings;
            this.original = original;
        }

        public override Context GetContext()
        {
            return new Context(filename: "clone");
        }

        public override ReaderNode GetChildNamed(string name)
        {
            throw new NotImplementedException();
        }
        public override string[] GetAllChildren()
        {
            throw new NotImplementedException();
        }
        public override bool HasText()
        {
            throw new NotImplementedException();
        }

        public override int[] GetArrayDimensions(int rank)
        {
            var arr = original as Array;
            if (arr == null)
            {
                Dbg.Err("Internal error: ReaderNodeCloneRecorderItem.GetArrayDimensions() called on non-array");
                return null;
            }

            if (arr.Rank != rank)
            {
                Dbg.Err("Internal error: ReaderNodeCloneRecorderItem.GetArrayDimensions() called with mismatched rank");
                return null;
            }

            return UtilType.GetArrayDimensions(arr);
        }

        public override object ParseElement(Type type, object model, ReaderGlobals readerGlobals, Recorder.Settings recorderSettings)
        {
            throw new NotImplementedException();
        }
    }
}
