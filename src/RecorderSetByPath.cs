using System;
using System.Collections;
using System.Collections.Generic;

namespace Dec
{
    // The write-back seam for SetByPath: Record<T>(ref T, ...) is the only place the original refs are reachable, so the replay is a Recorder. It is not a WriterNode either, even though that would inherit the writer's dispatch outright: WriterNode has no channel to hand back the replacement instance a ConverterRecord body can produce, and ComposeElement's preamble emits serialization diagnostics (constructibility, Shared warnings) that make no sense on a set. The type classification is shared instead, through Serialization.ComposeStrategyFor.
    internal class RecorderSetByPath : Recorder
    {
        internal class State
        {
            public List<Path> chain;
            public object newValue;
            public Recorder.IUserSettings userSettings;
            public string targetSerialized;

            // A Dec root resolves the way the Composer writes: fields by reflection, and no dec-path references.
            public bool compose;
        }

        private readonly State state;

        // Index into state.chain of the segment this replay level is trying to match; candidates build on chain[depthIdx - 1].
        private readonly int depthIdx;

        internal bool Matched;
        internal bool Landed;

        public RecorderSetByPath(State state, int depthIdx)
        {
            this.state = state;
            this.depthIdx = depthIdx;
        }

        public override IUserSettings UserSettings
        {
            get
            {
                return state.userSettings;
            }
        }

        public override Context Context
        {
            get
            {
                return new Context(filename: "introspection");
            }
        }

        public override Direction Mode
        {
            get
            {
                return Direction.Write;
            }
        }

        public override Purpose Intent
        {
            get
            {
                return Purpose.Serialization;
            }
        }

        internal override void Record<T>(ref T value, string label, Parameters parameters)
        {
            // First match wins; duplicate labels are already a loud error on Enumerate and real writes.
            if (Matched)
            {
                return;
            }

            Path basePath = state.chain[depthIdx - 1];

            if (parameters.asThis)
            {
                // Enumerate flattens RecordAsThis, so reported paths skip the "" label: resolve within the value at this same chain position, writing back through the ref for struct payloads. RecordAsThis must be the only call in its body, so a failure here is final rather than a miss to skip past.
                var (asThisOk, asThisUpdated) = ApplyInto(value, depthIdx, state, isRoot: false);
                Matched = true;
                if (asThisOk)
                {
                    value = (T)asThisUpdated;
                    Landed = true;
                }

                return;
            }

            var candidate = new PathMember(basePath, label);
            if (!candidate.Equals(state.chain[depthIdx]))
            {
                return;
            }

            Matched = true;

            if (depthIdx == state.chain.Count - 1)
            {
                if (!AssignCompatible(typeof(T), state.newValue, state))
                {
                    return;
                }

                value = (T)state.newValue;
                Landed = true;
                return;
            }

            var (ok, updated) = ApplyInto(value, depthIdx + 1, state, isRoot: false);
            if (ok)
            {
                value = (T)updated;
                Landed = true;
            }
        }

        private static bool AssignCompatible(Type slotType, object value, State state)
        {
            if (value == null)
            {
                if (slotType.IsValueType && Nullable.GetUnderlyingType(slotType) == null)
                {
                    Dbg.Err($"SetByPath: cannot assign null to value type {slotType} at [{state.targetSerialized}]");
                    return false;
                }

                return true;
            }

            var underlying = Nullable.GetUnderlyingType(slotType) ?? slotType;
            if (!underlying.IsAssignableFrom(value.GetType()))
            {
                Dbg.Err($"SetByPath: cannot assign {value.GetType()} to {slotType} at [{state.targetSerialized}]");
                return false;
            }

            return true;
        }

        // Resolves chain[depthIdx] as a position within `current` (already fetched from its slot), assigning at the chain's end. Returns whether the write landed, plus the possibly-mutated box so struct containers can be written back by the caller. isRoot is true only for the object SetByPath was handed; RecordAsThis re-enters at the same chain index, so the index cannot tell.
        internal static (bool ok, object updated) ApplyInto(object current, int depthIdx, State state, bool isRoot)
        {
            Path basePath = state.chain[depthIdx - 1];
            Path target = state.chain[depthIdx];

            if (current == null)
            {
                Dbg.Err($"SetByPath: path [{state.targetSerialized}] descends into a null value at [{basePath.Serialize()}]");
                return (false, current);
            }

            // The compose pipeline settles these as leaves before any strategy runs, so they have no interior no matter what a hand-built path claims.
            if (current is Dec && !isRoot)
            {
                Dbg.Err($"SetByPath: path [{state.targetSerialized}] descends into Dec `{current}` at [{basePath.Serialize()}], which serializes as a reference and has no addressable interior");
                return (false, current);
            }

            if (Database.IsForbidden(current))
            {
                Dbg.Err($"SetByPath: path [{state.targetSerialized}] descends into {current} at [{basePath.Serialize()}], which has been explicitly forbidden from recording");
                return (false, current);
            }

            // !state.compose is WriterNodeIntrospect.AllowDecPath by another name.
            if (!state.compose && Database.GetDecPathFromObj(current) != null)
            {
                Dbg.Err($"SetByPath: path [{state.targetSerialized}] descends into an object at [{basePath.Serialize()}] that serializes as a dec-path reference and has no addressable interior");
                return (false, current);
            }

            // The kind is the writer's own classification of this type, so the classification cannot drift; every ComposeKind still needs an arm here, and an unhandled one is a loud internal error rather than a silent misclassification.
            var strategy = Serialization.ComposeStrategyFor(current.GetType());
            var currentType = strategy.type;
            switch (strategy.kind)
            {
                case Serialization.ComposeKind.Unreferenceable:
                    Dbg.Err($"SetByPath: path [{state.targetSerialized}] descends into {currentType} at [{basePath.Serialize()}], which has no addressable interior");
                    return (false, current);

                case Serialization.ComposeKind.ByteArray:
                    Dbg.Err($"SetByPath: path [{state.targetSerialized}] descends into a byte[] at [{basePath.Serialize()}], which serializes as a single value and has no addressable interior");
                    return (false, current);

                case Serialization.ComposeKind.Array:
                {
                    var arr = (Array)current;
                    if (arr.Rank != 1)
                    {
                        Dbg.Err($"SetByPath: path [{state.targetSerialized}] addresses a position inside a multidimensional array at [{basePath.Serialize()}], which is not settable");
                        return (false, current);
                    }

                    return (ApplyIntoIndexed(arr.Length, i => arr.GetValue(i), (i, v) => arr.SetValue(v, i), currentType.GetElementType(), depthIdx, state), current);
                }

                case Serialization.ComposeKind.Queue:
                case Serialization.ComposeKind.Stack:
                    Dbg.Err($"SetByPath: path [{state.targetSerialized}] addresses a position inside a {currentType.GetGenericTypeDefinition().Name} at [{basePath.Serialize()}], which is not settable");
                    return (false, current);

                case Serialization.ComposeKind.Tuple:
                case Serialization.ComposeKind.ValueTuple:
                    Dbg.Err($"SetByPath: path [{state.targetSerialized}] addresses a position inside a tuple at [{basePath.Serialize()}], which is not settable");
                    return (false, current);

                case Serialization.ComposeKind.List:
                {
                    var listArgs = currentType.GetGenericInterfaceArguments(typeof(IList<>));
                    if (listArgs == null)
                    {
                        Dbg.Err($"SetByPath: path [{state.targetSerialized}] addresses a position inside unsupported list type {currentType} at [{basePath.Serialize()}]");
                        return (false, current);
                    }

                    var list = (IList)current;
                    return (ApplyIntoIndexed(list.Count, i => list[i], (i, v) => list[i] = v, listArgs[0], depthIdx, state), current);
                }

                case Serialization.ComposeKind.Dictionary:
                case Serialization.ComposeKind.Set:
                    Dbg.Err($"SetByPath: path [{state.targetSerialized}] addresses a position inside a dictionary or set at [{basePath.Serialize()}], which is not settable");
                    return (false, current);

                case Serialization.ComposeKind.Recordable:
                case Serialization.ComposeKind.Convertible:
                case Serialization.ComposeKind.Reflection:
                    break;

                default:
                    Dbg.Err($"Internal error: SetByPath has no handling for compose kind {strategy.kind} of {currentType}");
                    return (false, current);
            }

            bool suppressed = current is IConditionalRecordable conditional && !conditional.ShouldRecord(state.userSettings);

            if (strategy.kind == Serialization.ComposeKind.Recordable && !suppressed)
            {
                // `current` is already a box for struct recordables; the replay mutates it in place and the caller writes it back into the owning slot.
                var recorder = new RecorderSetByPath(state, depthIdx);
                ((IRecordable)current).Record(recorder);

                if (!recorder.Matched)
                {
                    Dbg.Err($"SetByPath: could not resolve [{target.Serialize()}] within the Record() body of {currentType} at [{basePath.Serialize()}]");
                    return (false, current);
                }

                return (recorder.Landed, current);
            }

            // As in the compose pipeline, a suppressed conditional falls through to its converter.
            var converter = strategy.converter;

            if (converter is ConverterRecord converterRecord)
            {
                // RecordObj hands back the value after Record(ref T), which carries both struct mutations and an instance the body replaced. Converter bodies are user code at a system boundary; the serialization writers report their exceptions the same way.
                var recorder = new RecorderSetByPath(state, depthIdx);
                object updated;
                try
                {
                    updated = converterRecord.RecordObj(current, recorder);
                }
                catch (Exception e)
                {
                    Dbg.Ex(e);
                    return (false, current);
                }

                if (!recorder.Matched)
                {
                    Dbg.Err($"SetByPath: could not resolve [{target.Serialize()}] within the {converter.GetType()} body for {currentType} at [{basePath.Serialize()}]");
                    return (false, current);
                }

                return (recorder.Landed, updated);
            }

            if (converter is ConverterFactory)
            {
                Dbg.Err($"SetByPath: path [{state.targetSerialized}] descends into {currentType} at [{basePath.Serialize()}], which is serialized by ConverterFactory {converter.GetType()}; ConverterFactory.Write has no way to hand back a modified value, so positions inside it are not settable. A ConverterRecord makes the interior settable.");
                return (false, current);
            }

            if (converter is ConverterString)
            {
                if (suppressed)
                {
                    Dbg.Err($"SetByPath: path [{state.targetSerialized}] descends into an IConditionalRecordable at [{basePath.Serialize()}] that is suppressed under these settings, so the path does not exist in this configuration");
                }
                else
                {
                    Dbg.Err($"SetByPath: path [{state.targetSerialized}] descends into {currentType} at [{basePath.Serialize()}], which ConverterString {converter.GetType()} serializes as a string; it has no addressable interior");
                }

                return (false, current);
            }

            // The compose pipeline's last resort is the type's fields.
            if (state.compose)
            {
                return ApplyIntoFields(current, depthIdx, state);
            }

            if (suppressed)
            {
                Dbg.Err($"SetByPath: path [{state.targetSerialized}] descends into an IConditionalRecordable at [{basePath.Serialize()}] that is suppressed under these settings, so the path does not exist in this configuration");
                return (false, current);
            }

            Dbg.Err($"SetByPath: path [{state.targetSerialized}] descends into {currentType} at [{basePath.Serialize()}], which has no addressable interior");
            return (false, current);
        }

        // Fields read and written through reflection, as the Composer walks them. FieldInfo.SetValue on a box mutates the box, so struct fields ride the same write-back as everything else.
        private static (bool ok, object updated) ApplyIntoFields(object current, int depthIdx, State state)
        {
            Path basePath = state.chain[depthIdx - 1];
            Path target = state.chain[depthIdx];
            bool terminal = depthIdx == state.chain.Count - 1;

            foreach (var field in current.GetType().GetSerializableFieldsFromHierarchy())
            {
                if (!new PathMember(basePath, field.Name).Equals(target))
                {
                    continue;
                }

                if (terminal)
                {
                    if (!AssignCompatible(field.FieldType, state.newValue, state))
                    {
                        return (false, current);
                    }

                    field.SetValue(current, state.newValue);
                    return (true, current);
                }

                var (ok, updated) = ApplyInto(field.GetValue(current), depthIdx + 1, state, isRoot: false);
                if (ok)
                {
                    field.SetValue(current, updated);
                }

                return (ok, current);
            }

            Dbg.Err($"SetByPath: could not resolve [{target.Serialize()}] among the fields of {current.GetType()} at [{basePath.Serialize()}]");
            return (false, current);
        }

        private static bool ApplyIntoIndexed(int count, Func<int, object> get, Action<int, object> set, Type elementType, int depthIdx, State state)
        {
            Path basePath = state.chain[depthIdx - 1];
            Path target = state.chain[depthIdx];
            bool terminal = depthIdx == state.chain.Count - 1;

            int found = -1;
            for (int i = 0; i < count; ++i)
            {
                if (new PathIndex(basePath, i).Equals(target))
                {
                    found = i;
                    break;
                }
            }

            if (found == -1)
            {
                Dbg.Err($"SetByPath: could not resolve [{target.Serialize()}] within the container at [{basePath.Serialize()}]; wrong segment kind, or index out of range");
                return false;
            }

            if (terminal)
            {
                if (!AssignCompatible(elementType, state.newValue, state))
                {
                    return false;
                }

                set(found, state.newValue);
                return true;
            }

            var (ok, updated) = ApplyInto(get(found), depthIdx + 1, state, isRoot: false);
            if (!ok)
            {
                return false;
            }

            set(found, updated);
            return true;
        }
    }
}
