using System.Reflection;
using System.Runtime.CompilerServices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


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

        public WriterNodeClone StartData(Type type)
        {
            return WriterNodeClone.StartData(this, type);
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

        private Dictionary<string, WriterNodeClone> recorderChildren;

        public override bool AllowReflection { get => writer.AllowReflection; }
        public override bool AllowAsThis { get => false; }
        public override bool AllowCloning { get => true;  }
        public override Recorder.IUserSettings UserSettings { get => writer.UserSettings; }

        private WriterNodeClone(WriterClone writer, int depth, Recorder.Context context) : base(context)
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

        public object GetResult(bool sharable)
        {
            if (!resultReady)
            {
                if (!originalSet)
                {
                    Dbg.Err("Internal error: WriterNodeClone.GetResult() called before original was set");
                    // we'll just "clone" null, I guess
                }

                CreateResult(sharable);

                resultReady = true;
            }

            return result;
        }

        private void CreateResult(bool sharable)
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
                result = converterFactory.CreateObj(new RecorderReader(readerClone, new ReaderContext()));
            }
            else if (RecorderContext.factories != null && original is IRecordable)
            {
                result = RecorderContext.CreateRecordableFromFactory(originalType, "clone", new ReaderNodeCloneCreator(original, UserSettings));
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

        private void CreateResult_Resolve(Type valType, bool resetDepth = false)
        {
            if (originalConverter != null)
            {
                // time to converter
                if (originalConverter is ConverterString converterString)
                {
                    // there's kind of not a lot we can do here to speed it up unfortunately
                    result = converterString.ReadObj(converterString.WriteObj(original), new InputContext("clone"));
                }
                else if (originalConverter is ConverterRecord converterRecord)
                {
                    // this calls CreateRecorderChild a bunch and fills it out
                    converterRecord.RecordObj(original, new RecorderWriter(this));

                    var readerClone = new ReaderNodeCloneRecorder(recorderChildren, UserSettings);

                    // object already exists
                    result = converterRecord.RecordObj(result, new RecorderReader(readerClone, new ReaderContext()));
                }
                else if (originalConverter is ConverterFactory converterFactory)
                {
                    // the rest of this was done earlier
                    var readerClone = new ReaderNodeCloneRecorder(recorderChildren, UserSettings);
                    result = converterFactory.ReadObj(result, new RecorderReader(readerClone, new ReaderContext()));
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else if (valType.IsArray)
            {
                var originalArray = original as Array;
                var resultArray = result as Array;

                // if the array members are valuelike, we can just copy the whole thing
                if (UtilType.CanBeCloneCopied(originalArray.GetType().GetElementType()))
                {
                    Array.Copy(originalArray, resultArray, originalArray.Length);
                }
                else
                {
                    int[] dimensions = Enumerable.Range(0, originalArray.Rank).Select(i => originalArray.GetLength(i)).ToArray();
                    int[] index = new int[originalArray.Rank];

                    DoArrayRecursive(originalArray, resultArray, dimensions, index, 0, resetDepth);
                }
            }
            else if (valType.IsGenericType && valType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var originalList = original as IList;
                var resultList = result as IList;

                // just in case; maybe we should be reusing originals as models?
                resultList.Clear();

                // if the list members are valuelike, we can just copy the whole thing
                if (UtilType.CanBeCloneCopied(originalList.GetType().GetGenericArguments()[0]))
                {
                    // use AddRange to copy
                    var addRangeFunction = resultList.GetType().GetMethod("AddRange");
                    addRangeFunction.Invoke(resultList, new object[] { originalList });
                }
                else
                {
                    for (int i = 0; i < originalList.Count; i++)
                    {
                        resultList.Add(CloneChild(originalList[i], resetDepth));
                    }
                }

                resultList.GetType().GetField("_version", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(resultList, Util.CollectionDeserializationVersion);
            }
            else if (valType.IsGenericType && valType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var originalDict = original as IDictionary;
                var resultDict = result as IDictionary;

                // just in case; maybe we should be reusing originals as models?
                resultDict.Clear();

                // if the dictionary members are valuelike, we can just copy the whole thing
                // skipping the tests is important enough that we'll just specialcase the various options
                bool canCloneKey = UtilType.CanBeCloneCopied(originalDict.GetType().GetGenericArguments()[0]);
                bool canCloneValue = UtilType.CanBeCloneCopied(originalDict.GetType().GetGenericArguments()[1]);
                if (canCloneKey && canCloneValue)
                {
                    foreach (DictionaryEntry kvp in originalDict)
                    {
                        resultDict[kvp.Key] = kvp.Value;
                    }
                }
                else if (canCloneKey)
                {
                    foreach (DictionaryEntry kvp in originalDict)
                    {
                        resultDict[kvp.Key] = CloneChild(kvp.Value, resetDepth);
                    }
                }
                else if (canCloneValue)
                {
                    foreach (DictionaryEntry kvp in originalDict)
                    {
                        resultDict[CloneChild(kvp.Key, resetDepth)] = kvp.Value;
                    }
                }
                else
                {
                    foreach (DictionaryEntry kvp in originalDict)
                    {
                        resultDict[CloneChild(kvp.Key, resetDepth)] = CloneChild(kvp.Value, resetDepth);
                    }
                }
            }
            else if (valType.IsGenericType && valType.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                var originalSet = original as IEnumerable;

                // just in case; maybe we should be reusing originals as models?
                var clearFunction = result.GetType().GetMethod("Clear");
                clearFunction.Invoke(result, null);

                // if the hashset members are valuelike, we can just copy the whole thing
                if (UtilType.CanBeCloneCopied(originalSet.GetType().GetGenericArguments()[0]))
                {
                    var addFunction = result.GetType().GetMethod("Add");
                    foreach (var item in originalSet)
                    {
                        addFunction.Invoke(result, new object[] { item });
                    }
                }
                else
                {
                    var addFunction = result.GetType().GetMethod("Add");
                    foreach (var item in originalSet)
                    {
                        addFunction.Invoke(result, new object[] { CloneChild(item, resetDepth) });
                    }
                }
            }
            else if (valType.IsGenericType && valType.GetGenericTypeDefinition() == typeof(Queue<>))
            {
                var originalQueue = original as IEnumerable;
                var resultQueueClearFunction = result.GetType().GetMethod("Clear");
                resultQueueClearFunction.Invoke(result, null);

                // just in case; maybe we should be reusing originals as models?
                var clearFunction = result.GetType().GetMethod("Clear");
                clearFunction.Invoke(result, null);

                // if the queue members are valuelike, we can just copy the whole thing
                if (UtilType.CanBeCloneCopied(originalQueue.GetType().GetGenericArguments()[0]))
                {
                    // there might be a faster way to do this?
                    var resultQueueEnqueueFunction = result.GetType().GetMethod("Enqueue");
                    foreach (var item in originalQueue)
                    {
                        resultQueueEnqueueFunction.Invoke(result, new object[] { item });
                    }
                }
                else
                {
                    var resultQueueEnqueueFunction = result.GetType().GetMethod("Enqueue");
                    foreach (var item in originalQueue)
                    {
                        resultQueueEnqueueFunction.Invoke(result, new object[] { CloneChild(item, resetDepth) });
                    }
                }
            }
            else if (valType.IsGenericType && valType.GetGenericTypeDefinition() == typeof(Stack<>))
            {
                var originalStack = original as IEnumerable;
                var tempStack = new Stack<object>();

                // just in case; maybe we should be reusing originals as models?
                var clearFunction = result.GetType().GetMethod("Clear");
                clearFunction.Invoke(result, null);

                // if the stack members are valuelike, we can just copy the whole thing
                if (UtilType.CanBeCloneCopied(originalStack.GetType().GetGenericArguments()[0]))
                {
                    foreach (var item in originalStack)
                    {
                        tempStack.Push(CloneChild(item, resetDepth));
                    }

                    var resultStackClearFunction = result.GetType().GetMethod("Clear");
                    resultStackClearFunction.Invoke(result, null);

                    var resultStackPushFunction = result.GetType().GetMethod("Push");
                    while (tempStack.Count > 0)
                    {
                        resultStackPushFunction.Invoke(result, new object[] { tempStack.Pop() });
                    }
                }
                else
                {
                    foreach (var item in originalStack)
                    {
                        tempStack.Push(CloneChild(item, resetDepth));
                    }

                    var resultStackClearFunction = result.GetType().GetMethod("Clear");
                    resultStackClearFunction.Invoke(result, null);

                    var resultStackPushFunction = result.GetType().GetMethod("Push");
                    while (tempStack.Count > 0)
                    {
                        resultStackPushFunction.Invoke(result, new object[] { tempStack.Pop() });
                    }
                }
            }
            else if (valType.IsGenericType && (
                         valType.GetGenericTypeDefinition() == typeof(Tuple<>) ||
                         valType.GetGenericTypeDefinition() == typeof(Tuple<,>) ||
                         valType.GetGenericTypeDefinition() == typeof(Tuple<,,>) ||
                         valType.GetGenericTypeDefinition() == typeof(Tuple<,,,>) ||
                         valType.GetGenericTypeDefinition() == typeof(Tuple<,,,,>) ||
                         valType.GetGenericTypeDefinition() == typeof(Tuple<,,,,,>) ||
                         valType.GetGenericTypeDefinition() == typeof(Tuple<,,,,,,>) ||
                         valType.GetGenericTypeDefinition() == typeof(Tuple<,,,,,,,>)
                     ))
            {
                var tupleItems = original.GetType().GetProperties().Select(prop => prop.GetValue(original)).Select(item => CloneChild(item, resetDepth)).ToArray();
                result = Activator.CreateInstance(original.GetType(), tupleItems);
            }
            else if (valType.IsGenericType && (
                         valType.GetGenericTypeDefinition() == typeof(ValueTuple<>) ||
                         valType.GetGenericTypeDefinition() == typeof(ValueTuple<,>) ||
                         valType.GetGenericTypeDefinition() == typeof(ValueTuple<,,>) ||
                         valType.GetGenericTypeDefinition() == typeof(ValueTuple<,,,>) ||
                         valType.GetGenericTypeDefinition() == typeof(ValueTuple<,,,,>) ||
                         valType.GetGenericTypeDefinition() == typeof(ValueTuple<,,,,,>) ||
                         valType.GetGenericTypeDefinition() == typeof(ValueTuple<,,,,,,>) ||
                         valType.GetGenericTypeDefinition() == typeof(ValueTuple<,,,,,,,>)
                     ))
            {
                var valueTupleItems = original.GetType().GetFields().Select(field => field.GetValue(original)).Select(item => CloneChild(item, resetDepth)).ToArray();
                result = Activator.CreateInstance(original.GetType(), valueTupleItems);
            }
            else if (original is IRecordable originalRecordable)
            {
                if (result == null)
                {
                    // we have presumably already printed an error explaining why we can't create this class, so just give up
                    return;
                }

                // this calls CreateRecorderChild a bunch and fills it out
                originalRecordable.Record(new RecorderWriter(this));

                var readerClone = new ReaderNodeCloneRecorder(recorderChildren, UserSettings);

                // do the dupe
                (result as IRecordable).Record(new RecorderReader(readerClone, new ReaderContext()));
            }
            else
            {
                // something went wrong
                Dbg.Err($"Internal error: Failed to clone object of type {valType}");
            }
        }

        public static WriterNodeClone StartData(WriterClone writer, Type type)
        {
            return new WriterNodeClone(writer, 0, new Recorder.Context() { shared = Recorder.Context.Shared.Flexible });
        }

        public override WriterNode CreateRecorderChild(string label, Recorder.Context context)
        {
            if (recorderChildren == null)
            {
                recorderChildren = new Dictionary<string, WriterNodeClone>();
            }

            var child = new WriterNodeClone(writer, depth + 1, context);
            recorderChildren[label] = child;
            return child;
        }

        public override WriterNode CreateReflectionChild(System.Reflection.FieldInfo field, Recorder.Context context)
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

            var child = new WriterNodeClone(writer, resetDepth ? 0 : depth + 1, RecorderContext.CreateChild());
            Serialization.ComposeElement(child, obj, obj.GetType());
            return child.GetResult(false);
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

        public override void TagClass(Type type)
        {
            // we don't actually care about this, the clone system finds the difference between fields-as-set and fields-as-expected to be pointless
        }

        public override void WriteExplicitNull()
        {
            SetValuelikeOriginalAndResult(null);
        }

        public override bool WriteReference(object value)
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
        public override Recorder.IUserSettings UserSettings { get; }

        private Dictionary<string, WriterNodeClone> recorderChildren;
        private HashSet<string> recorderChildrenConsumed = new HashSet<string>();
        public ReaderNodeCloneRecorder(Dictionary<string, WriterNodeClone> recorderChildren, Recorder.IUserSettings userSettings)
        {
            this.UserSettings = userSettings;
            this.recorderChildren = recorderChildren;
        }

        public override InputContext GetInputContext()
        {
            return new InputContext("clone");
        }

        public override int[] GetArrayDimensions(int rank)
        {
            // not valid, this is used only for recorders
            throw new NotImplementedException();
        }

        public override ReaderNode GetChildNamed(string name)
        {
            if (recorderChildren.TryGetValue(name, out var child))
            {
                if (recorderChildrenConsumed.Contains(name))
                {
                    Dbg.Err($"Clone child {name} accessed twice; this is probably an attempt to record the same field twice, which is not supported");
                    return null;
                }

                recorderChildrenConsumed.Add(name);
                return new ReaderNodeCloneRecorderItem(child, UserSettings);
            }
            else
            {
                return null;
            }
        }
        public override string[] GetAllChildren()
        {
            return recorderChildren.Keys.ToArray();
        }

        public override object ParseElement(Type type, object model, ReaderContext readerContext, Recorder.Context recorderContext)
        {
            // not valid, this is used only for recorders
            throw new NotImplementedException();
        }
    }

    internal class ReaderNodeCloneRecorderItem : ReaderNode
    {
        public override bool AllowAsThis { get => false; }
        public override Recorder.IUserSettings UserSettings { get; }

        private WriterNodeClone item;
        public ReaderNodeCloneRecorderItem(WriterNodeClone item, Recorder.IUserSettings userSettings)
        {
            this.UserSettings = userSettings;
            this.item = item;
        }

        public override InputContext GetInputContext()
        {
            return new InputContext("clone");
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

        public override object ParseElement(Type type, object model, ReaderContext readerContext, Recorder.Context recorderContext)
        {
            // we actually just ignore the type right now, we copy off the original

            item.SetModel(model);
            return item.GetResult(recorderContext.shared != Recorder.Context.Shared.Deny);
        }
    }

    internal class ReaderNodeCloneCreator : ReaderNode
    {
        public override Recorder.IUserSettings UserSettings { get; }

        private object original;
        public ReaderNodeCloneCreator(object original, Recorder.IUserSettings userSettings)
        {
            this.UserSettings = userSettings;
            this.original = original;
        }

        public override InputContext GetInputContext()
        {
            return new InputContext("clone");
        }

        public override ReaderNode GetChildNamed(string name)
        {
            throw new NotImplementedException();
        }
        public override string[] GetAllChildren()
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

            return Enumerable.Range(0, rank).Select(i => arr.GetLength(i)).ToArray();
        }

        public override object ParseElement(Type type, object model, ReaderContext readerContext, Recorder.Context recorderContext)
        {
            throw new NotImplementedException();
        }
    }
}
