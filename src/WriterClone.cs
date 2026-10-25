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

        internal Dictionary<object, object> cloneReferences = new Dictionary<object, object>(ComparerIdentity.Instance);

        // One creator serves the whole operation; object construction only consults it for the duration of the call.
        private ReaderNodeCloneCreator creator;

        // A node goes back here once its parent's resolution is done with it (or, if its own resolution was deferred, once that finishes), so a clone allocates about as many nodes as it holds at once rather than one per position. Free nodes are chained through their own nodeFreeNext, so the pool itself never allocates.
        private WriterNodeClone nodeFreeHead;

        public WriterClone(Recorder.IUserSettings userSettings)
        {
            this.UserSettings = userSettings;
        }

        internal ReaderNodeCloneCreator CreatorFor(object original)
        {
            if (creator == null)
            {
                creator = new ReaderNodeCloneCreator(UserSettings);
            }

            creator.original = original;
            return creator;
        }

        public WriterNodeClone StartClone(Type type)
        {
            return NodeAcquire(0, new Recorder.Settings() { shared = Recorder.Settings.Shared.Flexible }, PathPending.Built(new PathRoot("RECORD")));
        }

        internal WriterNodeClone NodeAcquire(int depth, Recorder.Settings settings, PathPending path)
        {
            var node = nodeFreeHead;
            if (node != null)
            {
                nodeFreeHead = node.nodeFreeNext;
                node.nodeFreeNext = null;
            }
            else
            {
                node = new WriterNodeClone(this);
            }

            node.Prepare(depth, settings, path);
            return node;
        }

        internal void NodeRelease(WriterNodeClone node)
        {
            node.nodeFreeNext = nodeFreeHead;
            nodeFreeHead = node;
        }
    }

    // Carries a value-type field from Clone's write pass to its read pass without boxing it: a value copied as-is, or a recorded struct, first as its original and then as its clone.
    internal abstract class CloneSlot
    {
        internal abstract object Box();
    }

    internal sealed class CloneSlot<T> : CloneSlot
    {
        internal T value;

        internal override object Box()
        {
            return value;
        }
    }

    // Whether a field of type T clones as a plain copy of its value with nothing else happening, which is when Clone can carry it in a CloneSlot instead of through the general path.
    internal static class CloneCopiedValue<T>
    {
        // Computed on first use rather than in a static initializer, so a converter lookup that throws is retried next time instead of breaking the type for good. A racing read can only see it unset or false, which recomputes or takes the general path.
        private static bool? Cached;

        internal static bool Applies()
        {
            if (!Cached.HasValue)
            {
                Cached = Compute();
            }

            return Cached.Value;
        }

        // Everything admitted here composes the same whenever it runs, with no references, lookups or user Record() involved, which is what lets a read of another type replay the skipped write later; widen this only to types that keep that true. Nullable is out because a null one boxes to null, which the replay can't take the type of.
        private static bool Compute()
        {
            var type = typeof(T);
            if (Nullable.GetUnderlyingType(type) != null)
            {
                return false;
            }

            // The general path runs these on the copy it hands back.
            if (typeof(IPostCloneOriginal).IsAssignableFrom(type) || typeof(IPostCloneNew).IsAssignableFrom(type))
            {
                return false;
            }

            return type.IsEnum || UtilType.CanBeCloneCopied(type);
        }
    }

    // Whether Clone can carry a recorded struct of type T in a CloneSlot and record it in place through RecordableStruct<T>, instead of boxing it into the general path: as a field, which keeps its destination's value as the model, or as an element Clone creates itself.
    internal static class CloneRecordedStruct<T>
    {
        // As in CloneCopiedValue<T>, a racing read can only see these unset or false, which recomputes or takes the general path.
        private static bool? CachedField;
        private static bool? CachedElement;

        internal static bool AppliesField()
        {
            if (!CachedField.HasValue)
            {
                var type = typeof(T);

                // A copy-as-is struct composes through WriteCloneCopy rather than WriteRecord, and the general path runs the post-clone hooks on the copy it hands back.
                CachedField = RecordableStruct<T>.Applies() && !UtilType.CanBeCloneCopied(type) && !typeof(IPostCloneOriginal).IsAssignableFrom(type) && !typeof(IPostCloneNew).IsAssignableFrom(type);
            }

            return CachedField.Value;
        }

        internal static bool AppliesElement()
        {
            if (!CachedElement.HasValue)
            {
                // The general path creates an element with CreateInstanceSafe, which runs a declared parameterless constructor where default(T) wouldn't.
                CachedElement = AppliesField() && typeof(T).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null) == null;
            }

            return CachedElement.Value;
        }
    }

    // Clone's recorders: a field CloneCopiedValue<T> or CloneRecordedStruct<T> admits goes from the write pass to the read pass in a CloneSlot, without boxing it, instead of through ComposeElement and ParseElement.
    internal sealed class RecorderWriterClone : RecorderWriter
    {
        private readonly WriterNodeClone cloneNode;

        internal RecorderWriterClone(WriterNodeClone node) : base(node)
        {
            cloneNode = node;
        }

        internal override void Record<T>(ref T value, string label, Parameters parameters)
        {
            if (!FieldAdmit(ref value, label, in parameters))
            {
                return;
            }

            // Shared() keeps the general path, which warns about it.
            if (typeof(T).IsValueType && !parameters.shared)
            {
                if (CloneCopiedValue<T>.Applies())
                {
                    cloneNode.CreateRecorderChildValue(label, parameters.CreateSettings(), value, WriterNodeClone.SlotState.Copied);
                    return;
                }

                if (TryRecordStruct(value, label, in parameters))
                {
                    return;
                }
            }

            // Directly here rather than through a helper; see RecorderWriter.Record<T>.
            Serialization.ComposeElement(cloneNode.CreateRecorderChild(label, parameters.CreateSettings()), value, typeof(T));
        }

        // Out of line so the value-type fields that never take it don't carry it in their Record<T>, which costs a field-heavy Clone a few percent.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool TryRecordStruct<T>(T value, string label, in Parameters parameters)
        {
            // Factories create the object themselves in the general path.
            if (parameters.factories != null || !CloneRecordedStruct<T>.AppliesField() || Config.TestForceFallbackEmit)
            {
                return false;
            }

            cloneNode.CreateRecorderChildValue(label, parameters.CreateSettings(), value, WriterNodeClone.SlotState.Recorded);
            return true;
        }
    }

    internal sealed class RecorderReaderClone : RecorderReader
    {
        internal RecorderReaderClone(ReaderNodeCloneRecorder node, ReaderGlobals globals) : base(node, globals)
        {
        }

        internal override void Record<T>(ref T value, string label, Parameters parameters)
        {
            var recorded = FieldAdmit(ref value, label, in parameters);
            if (recorded == null)
            {
                return;
            }

            // A clone view hands out nothing but its item, RecordAsThis's "" field included, since clone views don't allow AsThis.
            if (typeof(T).IsValueType && ((ReaderNodeCloneRecorderItem)recorded).TryReadInto(ref value))
            {
                return;
            }

            FieldRead(ref value, recorded, in parameters);
        }
    }

    // this process is pretty complicated
    // WriterNodeClone basically acts as a storage for all the data we need to clone something, which is, in some cases, accumulated gradually and not immediately
    // once it's all ready, *then* we can call GetResult() and get the actual clone, decorated with all its childrens' clones
    // thing is, this is a lazy process, so calling GetResult() also does all the work for its children
    // then we need some somewhat awkward ReaderNode subclasses for "this node, with children" and "the specific child that was requested"
    // I get the feeling that this implies a bunch of interfaces should be cleaned up, but I'm not doing that right now, so
    internal sealed class WriterNodeClone : WriterNodePooled
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
        // Past these, CreateResult warns and then errors about unshareable nesting; a recorded struct deeper than WarnRecursionDepth takes the general path so it gets the same diagnostics.
        private const int WarnRecursionDepth = 50;
        private const int MaxRecursionDepth = 100;

        // Set when this node's resolution was deferred to a pending write, which then releases the node itself instead of its parent doing so.
        private bool resolvePending;

        // A value-type field written without going through ComposeElement; see CreateRecorderChildValue. The slot stays with the node across reuse, and slotState says what it holds for this use.
        private CloneSlot slot;
        private SlotState slotState;

        internal enum SlotState
        {
            Empty,

            // A value copied as-is, which is also its result.
            Copied,

            // A recorded struct's original, not yet cloned.
            Recorded,

            // The clone of a recorded struct, which every later read gets.
            Resolved,
        }

        // Created on first use and kept across reuse, as is the read view; most nodes are leaves and never need them.
        private List<(string key, WriterNodeClone value)> recorderChildren;
        private ReaderNodeCloneRecorder readerView;

        internal WriterNodeClone nodeFreeNext;

        public override bool AllowReflection { get => writer.AllowReflection; }
        public override bool AllowDecPath { get => true; }
        public override bool AllowCloning { get => true;  }
        public override Recorder.Purpose Intent { get => Recorder.Purpose.Cloning; }
        public override Recorder.IUserSettings UserSettings { get => writer.UserSettings; }

        internal WriterNodeClone(WriterClone writer)
        {
            this.writer = writer;
        }

        internal void Prepare(int depth, Recorder.Settings settings, PathPending path)
        {
            Reuse(settings, path);
            this.depth = depth;

            model = null;
            modelSet = false;
            original = null;
            originalConverter = null;
            originalSet = false;
            result = null;
            resultReady = false;
            resultIsValuelike = false;
            resolvePending = false;
            slotState = SlotState.Empty;

            // Normally already empty; an exception partway through a resolution can leave children behind, and they're simply dropped, slots and all.
            recorderChildren?.Clear();
        }

        // Each read body gets this node's reader as if it were new.
        private RecorderReader ReaderAcquire()
        {
            if (readerView == null)
            {
                if (recorderChildren == null)
                {
                    recorderChildren = new List<(string key, WriterNodeClone value)>();
                }

                readerView = new ReaderNodeCloneRecorder(recorderChildren, UserSettings);
            }

            readerView.Reset();
            return readerView.reader;
        }

        // The children have served both of this node's passes; one whose own resolution was deferred releases itself when that runs.
        private void ChildrenRelease()
        {
            if (recorderChildren == null)
            {
                return;
            }

            // In reverse, so the pool hands them back in recording order: the next object of the same type then gets each field's node back, slot type and all.
            for (int i = recorderChildren.Count - 1; i >= 0; --i)
            {
                var child = recorderChildren[i].value;
                if (!child.resolvePending)
                {
                    writer.NodeRelease(child);
                }
            }

            recorderChildren.Clear();
        }

        internal void SetModel(object model)
        {
            if (modelSet)
            {
                // Duplicate read; RecorderReader has already warned. The first model won.
                return;
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
            // Already cloned through TryReadInto; the original must not be composed again, or everything it references would be cloned twice.
            if (slotState == SlotState.Resolved)
            {
                return slot.Box();
            }

            if ((slotState == SlotState.Copied || slotState == SlotState.Recorded) && !originalSet)
            {
                // A read that doesn't take the value straight from the slot gets the write the slot skipped, exactly as it would have run.
                var boxed = slot.Box();
                Serialization.ComposeElement(this, boxed, boxed.GetType());
            }

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

            var originalType = original.GetType();

            // Value types are copied at every position, as Write does; only reference types have an identity to preserve.
            bool tracked = !originalType.IsValueType;

            // see if we already have a reference
            if (tracked && writer.cloneReferences.TryGetValue(original, out var clone))
            {
                result = clone;
                return;
            }

            // make our object
            // tuples are sort of busted and we're going to punt on them for now
            // this needs to deal with anything that has complicated constructor behavior

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
                RecorderRun(converterFactory, original);

                // now we create the object itself
                result = ReaderAcquire().BodyCreate(converterFactory);
            }
            else if (originalConverter is ConverterString converterString)
            {
                var str = converterString.WriteObj(original);
                result = converterString.ReadObj(str, new Context(filename: "clone"));
                done = true;
            }
            else if (RecorderSettings.factories != null && original is IRecordable)
            {
                result = RecorderSettings.CreateRecordableFromFactory(originalType, "clone", writer.CreatorFor(original));
            }
            else if ((model == null || model.GetType() != original.GetType()) && !typeof(ITuple).IsAssignableFrom(originalType))
            {
                // derive an appropriate type; we're just yanking this out of the original type right now (is this always right?)
                // this is kind of awful in terms of perf ;.;
                result = original.GetType().CreateInstanceSafe("recordable", writer.CreatorFor(original));
            }
            else
            {
                // re-use!
                result = model;
            }

            // put this in first so we can use the reference if we need it through the recursion
            // this probably does not work at all for tuple.
            if (tracked)
            {
                writer.cloneReferences[original] = result;
            }

            // at this point we have the right object even if we don't have enough room on the stack
            // although that's not as useful if this is a struct
            bool deferred = false;
            if (!done)
            {
                bool doPending = false;
                if (depth > 20 && result != null && !result.GetType().IsValueType && sharable)
                {
                    doPending = true;
                }
                else if (depth > MaxRecursionDepth)
                {
                    Dbg.Err("Depth limiter ran into an unshareable node stack that's too deep. Recommend using more `.Shared()` calls to allow for stack splitting. This may cause data corruption.");
                    doPending = true;
                }
                else if (depth > WarnRecursionDepth)
                {
                    // this is currently going to be way too spammy, we'll fix it later
                    Dbg.Wrn("Depth limiter is running into an unshareable node stack that's too deep. Recommend using more `.Shared()` calls to allow for stack splitting.");
                }

                if (doPending)
                {
                    // Ancestors are recycled before the pending write runs, so the path is built while they're still intact.
                    resolvePending = true;
                    _ = Path;

                    // Captures only `this`; capturing the local originalType would allocate a closure on every call, deferred or not.
                    writer.RegisterPendingWrite(() =>
                    {
                        CreateResult_Resolve(original.GetType(), resetDepth: true);
                        (original as IPostCloneOriginal)?.PostCloneOriginal();
                        (result as IPostCloneNew)?.PostCloneNew();
                        writer.NodeRelease(this);
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
            if (tracked)
            {
                writer.cloneReferences[original] = result;
            }
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
                    self.RecorderRun(self.original as IRecordable);

                    // do the dupe
                    var resultAsIRecordable = self.result as IRecordable;
                    var recorderReader = self.ReaderAcquire();
                    try
                    {
                        recorderReader.BodyRecord(resultAsIRecordable);
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

                            versionField.SetValue(resultList, Util.CollectionDeserializationVersionBoxed);
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
                    Action<WriterNodeClone, bool> fallback = (self, resetDepth) =>
                    {
                        var originalList = self.original as IList;
                        var resultList = self.result as IList;

                        // just in case; maybe we should be reusing originals as models?
                        resultList.Clear();

                        for (int i = 0; i < originalList.Count; i++)
                        {
                            resultList.Add(self.CloneChild(originalList[i], resetDepth));
                        }

                        versionField?.SetValue(resultList, Util.CollectionDeserializationVersionBoxed);
                    };

                    if (!isExactList || !listElementType.IsValueType || !RuntimeFeature.IsDynamicCodeSupported)
                    {
                        return fallback;
                    }

                    var typed = (Action<WriterNodeClone, bool>)BuildResolveStrategyListTypedMethod.MakeGenericMethod(listElementType).Invoke(null, new object[] { versionField });
                    if (typed == null)
                    {
                        return fallback;
                    }

                    return (self, resetDepth) =>
                    {
                        // I don't know what the perf hit of this is; it may be that the decision should happen when generating the callback, and changes to TestForceFallbackEmit should clear cached callbacks.
                        if (Config.TestForceFallbackEmit)
                        {
                            fallback(self, resetDepth);
                        }
                        else
                        {
                            typed(self, resetDepth);
                        }
                    };
                }
            }

            if (typeof(IDictionary).IsAssignableFrom(valType))
            {
                var dictArgs = valType.GetGenericInterfaceArguments(typeof(IDictionary<,>));
                bool canCloneKey = UtilType.CanBeCloneCopied(dictArgs[0]);
                bool canCloneValue = UtilType.CanBeCloneCopied(dictArgs[1]);

                Action<WriterNodeClone, bool> fallback;

                // if the dictionary members are valuelike, we can just copy the whole thing
                // skipping the tests is important enough that we'll just specialcase the various options
                // IDictionaryEnumerator's Key and Value rather than foreach, which boxes a DictionaryEntry per entry
                if (canCloneKey && canCloneValue)
                {
                    fallback = (self, resetDepth) =>
                    {
                        var originalDict = self.original as IDictionary;
                        var resultDict = self.result as IDictionary;
                        resultDict.Clear();
                        IDictionaryEnumerator iterator = originalDict.GetEnumerator();
                        while (iterator.MoveNext())
                        {
                            resultDict[iterator.Key] = iterator.Value;
                        }
                    };
                }
                else if (canCloneKey)
                {
                    fallback = (self, resetDepth) =>
                    {
                        var originalDict = self.original as IDictionary;
                        var resultDict = self.result as IDictionary;
                        resultDict.Clear();
                        IDictionaryEnumerator iterator = originalDict.GetEnumerator();
                        while (iterator.MoveNext())
                        {
                            resultDict[iterator.Key] = self.CloneChild(iterator.Value, resetDepth);
                        }
                    };
                }
                else if (canCloneValue)
                {
                    fallback = (self, resetDepth) =>
                    {
                        var originalDict = self.original as IDictionary;
                        var resultDict = self.result as IDictionary;
                        resultDict.Clear();
                        IDictionaryEnumerator iterator = originalDict.GetEnumerator();
                        while (iterator.MoveNext())
                        {
                            resultDict[self.CloneChild(iterator.Key, resetDepth)] = iterator.Value;
                        }
                    };
                }
                else
                {
                    fallback = (self, resetDepth) =>
                    {
                        var originalDict = self.original as IDictionary;
                        var resultDict = self.result as IDictionary;
                        resultDict.Clear();
                        IDictionaryEnumerator iterator = originalDict.GetEnumerator();
                        while (iterator.MoveNext())
                        {
                            resultDict[self.CloneChild(iterator.Key, resetDepth)] = self.CloneChild(iterator.Value, resetDepth);
                        }
                    };
                }

                // An exact Dictionary gets a typed copy that presizes its result and boxes nothing it copies as-is; everything else, and every runtime without dynamic code, takes the non-generic path above.
                if (!RuntimeFeature.IsDynamicCodeSupported || !valType.IsGenericType || valType.GetGenericTypeDefinition() != typeof(Dictionary<,>))
                {
                    return fallback;
                }

                var typed = (Action<WriterNodeClone, bool>)BuildResolveStrategyDictionaryTypedMethod.MakeGenericMethod(dictArgs).Invoke(null, new object[] { canCloneKey, canCloneValue });
                return (self, resetDepth) =>
                {
                    if (Config.TestForceFallbackEmit)
                    {
                        fallback(self, resetDepth);
                    }
                    else
                    {
                        typed(self, resetDepth);
                    }
                };
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

        private static readonly MethodInfo BuildResolveStrategyDictionaryTypedMethod = typeof(WriterNodeClone).GetMethod(nameof(BuildResolveStrategyDictionaryTyped), BindingFlags.NonPublic | BindingFlags.Static);

        private static Action<WriterNodeClone, bool> BuildResolveStrategyDictionaryTyped<K, V>(bool canCloneKey, bool canCloneValue)
        {
            if (canCloneKey && canCloneValue)
            {
                return (self, resetDepth) =>
                {
                    var originalDict = (Dictionary<K, V>)self.original;
                    var resultDict = (Dictionary<K, V>)self.result;
                    resultDict.Clear();
                    resultDict.EnsureCapacity(originalDict.Count);
                    foreach (var entry in originalDict)
                    {
                        resultDict[entry.Key] = entry.Value;
                    }
                };
            }

            return (self, resetDepth) =>
            {
                var originalDict = (Dictionary<K, V>)self.original;
                var resultDict = (Dictionary<K, V>)self.result;
                resultDict.Clear();
                resultDict.EnsureCapacity(originalDict.Count);
                foreach (var entry in originalDict)
                {
                    // Key before value, as the non-generic path does, so both paths reach shared references in the same order.
                    var key = canCloneKey ? entry.Key : self.CloneChildTyped(entry.Key, resetDepth);
                    var value = canCloneValue ? entry.Value : self.CloneChildTyped(entry.Value, resetDepth);
                    resultDict[key] = value;
                }
            };
        }

        private static readonly MethodInfo BuildResolveStrategyListTypedMethod = typeof(WriterNodeClone).GetMethod(nameof(BuildResolveStrategyListTyped), BindingFlags.NonPublic | BindingFlags.Static);

        // A List<T> of recorded structs gets a typed copy that presizes its result and records each element in place instead of boxing it twice; null for any other T, which keeps the non-generic path.
        private static Action<WriterNodeClone, bool> BuildResolveStrategyListTyped<T>(FieldInfo versionField)
        {
            if (!CloneRecordedStruct<T>.AppliesElement())
            {
                return null;
            }

            return (self, resetDepth) =>
            {
                var originalList = (List<T>)self.original;
                var resultList = (List<T>)self.result;
                resultList.Clear();
                if (resultList.Capacity < originalList.Count)
                {
                    resultList.Capacity = originalList.Count;
                }

                for (int i = 0; i < originalList.Count; i++)
                {
                    resultList.Add(self.CloneChildTyped(originalList[i], resetDepth));
                }

                versionField.SetValue(resultList, Util.CollectionDeserializationVersionBoxed);
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
                    RecorderRun(converterRecord, original);

                    // object already exists
                    result = ReaderAcquire().BodyRecord(converterRecord, result);
                }
                else if (originalConverter is ConverterFactory converterFactory)
                {
                    // the rest of this was done earlier
                    result = ReaderAcquire().BodyRead(converterFactory, result);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            else
            {
                if (!ResolveStrategyCache.TryGetValue(valType, out var strategy))
                {
                    strategy = BuildResolveStrategy(valType);
                    ResolveStrategyCache[valType] = strategy;
                }

                strategy(this, resetDepth);
            }

            ChildrenRelease();
        }

        protected override RecorderWriter RecorderCreate()
        {
            return new RecorderWriterClone(this);
        }

        public override WriterNode CreateRecorderChild(string label, Recorder.Settings settings)
        {
            if (recorderChildren == null)
            {
                recorderChildren = new List<(string key, WriterNodeClone value)>();
            }

            var child = writer.NodeAcquire(depth + 1, settings, PathPending.Member(this, label));
            recorderChildren.Add((label, child));
            return child;
        }

        // For a field CloneCopiedValue<T> or CloneRecordedStruct<T> admits: the child holds the value itself, and the read pass takes it, or its clone, back out through TryReadInto.
        internal void CreateRecorderChildValue<T>(string label, Recorder.Settings settings, T value, SlotState state)
        {
            var child = (WriterNodeClone)CreateRecorderChild(label, settings);
            if (!(child.slot is CloneSlot<T> typed))
            {
                typed = new CloneSlot<T>();
                child.slot = typed;
            }

            typed.value = value;
            child.slotState = state;
        }

        // Leaves the result in place; a ConverterFactory's Create and Read can both read the same field, and both get the same result, as GetResult gives them in the general path.
        internal bool TryReadInto<T>(ref T value)
        {
            if ((slotState == SlotState.Copied || slotState == SlotState.Resolved) && slot is CloneSlot<T> typed)
            {
                value = typed.value;
                return true;
            }

            if (slotState == SlotState.Recorded && slot is CloneSlot<T> recorded)
            {
                return TryResolveRecorded(recorded, ref value);
            }

            return false;
        }

        // Out of line from TryReadInto, which every value-type field read in a clone goes through, so that stays small.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private bool TryResolveRecorded<T>(CloneSlot<T> typed, ref T value)
        {
            // Left to GetResult when a read through it has already cloned this, or when it would warn about the depth.
            if (originalSet || depth > WarnRecursionDepth)
            {
                return false;
            }

            CloneRecordedInto(typed.value, ref value);
            typed.value = value;
            slotState = SlotState.Resolved;
            return true;
        }

        // What the general path's CreateResult and IRecordable resolution do for a recorded struct, without boxing it: the original's write pass fills this node's children, and the read pass records them into value, which is the result's model.
        private void CloneRecordedInto<T>(T original, ref T value)
        {
            var record = RecordableStruct<T>.Record;

            // A copy, so a write pass that changes its own fields changes only this.
            RecorderRun(ref original, record);

            var recorderReader = ReaderAcquire();
            try
            {
                recorderReader.BodyRecord(ref value, record);
            }
            catch (Exception e)
            {
                Dbg.Ex(e);
            }

            ChildrenRelease();
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
            var child = writer.NodeAcquire(resetDepth ? 0 : depth + 1, RecorderSettings.CreateChild(), PathPending.Same(this));
            Serialization.ComposeElement(child, obj, objType);
            var cloned = child.GetResult(false, objType);

            if (!child.resolvePending)
            {
                writer.NodeRelease(child);
            }

            return cloned;
        }

        // CloneChild for a typed collection's element, which records a recorded struct in place instead of boxing it into the general path and back.
        internal T CloneChildTyped<T>(T original, bool resetDepth)
        {
            int childDepth = resetDepth ? 0 : depth + 1;

            // Factories create the object themselves in the general path.
            if (typeof(T).IsValueType && RecorderSettings.factories == null && childDepth <= WarnRecursionDepth && CloneRecordedStruct<T>.AppliesElement() && !Config.TestForceFallbackEmit)
            {
                var child = writer.NodeAcquire(childDepth, RecorderSettings.CreateChild(), PathPending.Same(this));

                // What CreateInstanceSafe makes of a struct with no constructor of its own.
                T result = default;
                child.CloneRecordedInto(original, ref result);
                writer.NodeRelease(child);
                return result;
            }

            return (T)CloneChild(original, resetDepth);
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

        // Handed out by GetChildNamed; RecorderReader parses each child it asks for before asking for another.
        private readonly ReaderNodeCloneRecorderItem item;

        internal readonly RecorderReader reader;

        public ReaderNodeCloneRecorder(List<(string key, WriterNodeClone value)> recorderChildren, Recorder.IUserSettings userSettings)
        {
            this.UserSettings = userSettings;
            this.recorderChildren = recorderChildren;
            item = new ReaderNodeCloneRecorderItem(userSettings);
            reader = new RecorderReaderClone(this, new ReaderGlobals());
        }

        // Readies the view and its reader for another read body, as if both were new.
        internal void Reset()
        {
            searchHint = 0;
            reader.Reset();
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
                    searchHint++;
                    return item.Point(hintEntry.value);
                }
            }

            // Slow path: linear scan
            for (int i = 0; i < count; i++)
            {
                var entry = recorderChildren[i];
                if (entry.key == name)
                {
                    searchHint = i + 1;
                    return item.Point(entry.value);
                }
            }

            return null;
        }

        public override object ParseElement(Type type, object model, ReaderGlobals readerGlobals, Recorder.Settings recorderSettings)
        {
            // not valid, this is used only for recorders
            throw new NotImplementedException();
        }
    }

    internal sealed class ReaderNodeCloneRecorderItem : ReaderNode
    {
        public override bool AllowAsThis { get => false; }
        public override Recorder.Purpose Intent { get => Recorder.Purpose.Cloning; }
        public override Recorder.IUserSettings UserSettings { get; }

        private WriterNodeClone item;

        public ReaderNodeCloneRecorderItem(Recorder.IUserSettings userSettings)
        {
            this.UserSettings = userSettings;
        }

        internal ReaderNodeCloneRecorderItem Point(WriterNodeClone item)
        {
            this.item = item;
            return this;
        }

        internal bool TryReadInto<T>(ref T value)
        {
            return item.TryReadInto(ref value);
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

        internal object original;

        public ReaderNodeCloneCreator(Recorder.IUserSettings userSettings)
        {
            this.UserSettings = userSettings;
        }

        public override Context GetContext()
        {
            return new Context(filename: "clone");
        }

        public override ReaderNode GetChildNamed(string name)
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
