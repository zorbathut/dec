using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Dec
{
    /// <summary>
    /// One field recorded by a ConverterFactoryFields.
    /// </summary>
    public readonly struct RecordField
    {
        /// <summary>
        /// The field to record, an instance field of the converted type or one of its base types.
        /// </summary>
        public readonly FieldInfo Field;

        /// <summary>
        /// The label it's recorded under.
        /// </summary>
        public readonly string Label;

        /// <summary>
        /// Whether it's recorded through `Recorder.Shared()`.
        /// </summary>
        public readonly bool Shared;

        public RecordField(FieldInfo field, string label, bool shared)
        {
            Field = field;
            Label = label;
            Shared = shared;
        }
    }

    /// <summary>
    /// Converter for a type recorded as a fixed list of its fields.
    /// </summary>
    /// <remarks>
    /// This suits types you can't give a Record() function, like compiler-generated iterators and closures, or types whose fields aren't accessible to you. Pass the fields to the constructor and implement Create(); Write and Read come from the field list, which the converter compiles when it's constructed. Like every Dynamic converter, it isn't found by Dec's converter scan; return it from `Config.ConverterFactory`.
    ///
    /// Write records each field as `recorder.Record(ref field, label)` would, through `recorder.Shared()` where the field asks for it. Read resets each field to its default before reading it, so a field missing from the data ends up default, and every recorded field of an object that already existed is read as if the object had just been created.
    ///
    /// A converter is immutable once constructed and may be used from several threads at once.
    /// </remarks>
    public abstract class ConverterFactoryFields : ConverterFactoryDynamic
    {
        private readonly Type type;
        private readonly RecordField[] fields;
        private readonly string[] ignored;

        // Null where the runtime can't generate code.
        private readonly Action<Recorder, object> writeEmitted;
        private readonly Action<Recorder, object> readEmitted;

        // FieldInterpret closed over each field's type, parallel to fields. Built on first use, so a runtime that can't close them fails inside a converter body rather than while the converter is being looked up; threads racing to build it build the same thing.
        private MethodInfo[] interpreters;

        /// <summary>
        /// Sets up recording of `type` as the given fields, in order.
        /// </summary>
        /// <remarks>
        /// `ignored` lists labels that are deliberately unused, usually ones older versions recorded; reading them doesn't warn about unused data.
        ///
        /// Fields that can't be recorded, like static fields or fields of another type, are reported as errors and left out.
        /// </remarks>
        protected ConverterFactoryFields(Type type, IEnumerable<RecordField> fields, IEnumerable<string> ignored = null)
        {
            this.type = type;

            var labels = new HashSet<string>();

            var ignoredAccepted = new List<string>();
            foreach (var label in ignored ?? Enumerable.Empty<string>())
            {
                if (label == null)
                {
                    Dbg.Err($"{GetType()}: an ignored label for `{type}` is null; skipping it");
                }
                else if (!labels.Add(label))
                {
                    Dbg.Err($"{GetType()}: label `{label}` is used more than once for `{type}`; skipping the repeat");
                }
                else
                {
                    ignoredAccepted.Add(label);
                }
            }
            this.ignored = ignoredAccepted.ToArray();

            if (type.ContainsGenericParameters)
            {
                Dbg.Err($"{GetType()}: can't record `{type}`, which has unbound generic parameters; recording nothing");
                this.fields = new RecordField[0];
                return;
            }

            var accepted = new List<RecordField>();
            foreach (var field in fields)
            {
                if (field.Field == null || field.Label == null)
                {
                    Dbg.Err($"{GetType()}: a field for `{type}` is missing its FieldInfo or its label; skipping it");
                }
                else if (field.Field.IsStatic)
                {
                    Dbg.Err($"{GetType()}: `{field.Field}` is static, and only instance fields of `{type}` can be recorded; skipping it");
                }
                else if (!DeclaredOn(field.Field, type))
                {
                    Dbg.Err($"{GetType()}: `{field.Field}` is declared on `{field.Field.DeclaringType}`, not on `{type}` or a type it inherits from; skipping it");
                }
                else if (field.Field.FieldType.IsPointer)
                {
                    Dbg.Err($"{GetType()}: `{field.Field}` is a pointer, which can't be recorded; skipping it");
                }
                else if (!labels.Add(field.Label))
                {
                    Dbg.Err($"{GetType()}: label `{field.Label}` is used more than once for `{type}`; skipping `{field.Field}`");
                }
                else
                {
                    accepted.Add(field);
                }
            }
            this.fields = accepted.ToArray();

            if (RuntimeFeature.IsDynamicCodeSupported)
            {
                writeEmitted = Emit(read: false);
                readEmitted = Emit(read: true);
            }
        }

        // The generated code addresses fields straight through the object, unverified, so the field has to belong to it. Type.IsAssignableFrom isn't strict enough: it accepts Nullable<T> for T.
        private static bool DeclaredOn(FieldInfo field, Type type)
        {
            for (var candidate = type; candidate != null; candidate = candidate.BaseType)
            {
                if (candidate == field.DeclaringType)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool Emitted
        {
            get
            {
                return writeEmitted != null;
            }
        }

        public sealed override void Write(object input, Recorder recorder)
        {
            if (writeEmitted != null && !Config.TestForceFallbackEmit)
            {
                writeEmitted(recorder, input);
                return;
            }

            Interpret(input, recorder, read: false);
        }

        public sealed override void Read(ref object input, Recorder recorder)
        {
            if (readEmitted != null && !Config.TestForceFallbackEmit)
            {
                readEmitted(recorder, input);
                return;
            }

            foreach (var label in ignored)
            {
                recorder.Ignore(label);
            }

            Interpret(input, recorder, read: true);
        }

        private void Interpret(object input, Recorder recorder, bool read)
        {
            interpreters = interpreters ?? fields.Select(field => FieldInterpretMethod.MakeGenericMethod(field.Field.FieldType)).ToArray();

            for (int i = 0; i < fields.Length; ++i)
            {
                try
                {
                    interpreters[i].Invoke(null, new object[] { recorder, input, fields[i].Field, fields[i].Label, fields[i].Shared, read });
                }
                catch (TargetInvocationException e) when (e.InnerException != null)
                {
                    // Let whatever the field's recording threw arrive as it would from the generated code.
                    ExceptionDispatchInfo.Capture(e.InnerException).Throw();
                }
            }
        }

        private static readonly MethodInfo RecorderRecordMethod = RecordMethodFind(typeof(Recorder));
        private static readonly MethodInfo ParametersRecordMethod = RecordMethodFind(typeof(Recorder.Parameters));
        private static readonly MethodInfo RecorderSharedMethod = typeof(Recorder).GetMethod(nameof(Recorder.Shared), Type.EmptyTypes);
        private static readonly MethodInfo RecorderIgnoreMethod = typeof(Recorder).GetMethod(nameof(Recorder.Ignore), new[] { typeof(string) });
        private static readonly MethodInfo FieldInterpretMethod = typeof(ConverterFactoryFields).GetMethod(nameof(FieldInterpret), BindingFlags.NonPublic | BindingFlags.Static);

        private static MethodInfo RecordMethodFind(Type owner)
        {
            return owner.GetMethods(BindingFlags.Public | BindingFlags.Instance).Single(method => method.Name == nameof(Recorder.Record) && method.IsGenericMethodDefinition && method.GetParameters().Length == 2);
        }

        // One method per type and direction that records every field in place, through its address.
        private Action<Recorder, object> Emit(bool read)
        {
            var method = new DynamicMethod($"DecRecord{(read ? "Read" : "Write")}_{type.Name}", typeof(void), new[] { typeof(Recorder), typeof(object) }, typeof(ConverterFactoryFields).Module, skipVisibility: true);
            var il = method.GetILGenerator();

            // A class is addressed through its reference; a struct through the inside of its box, so reading mutates the box Dec passed in.
            var self = il.DeclareLocal(type.IsValueType ? type.MakeByRefType() : type);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(type.IsValueType ? OpCodes.Unbox : OpCodes.Castclass, type);
            il.Emit(OpCodes.Stloc, self);

            LocalBuilder parameters = null;

            if (read)
            {
                foreach (var label in ignored)
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldstr, label);
                    il.Emit(OpCodes.Callvirt, RecorderIgnoreMethod);
                }
            }

            foreach (var field in fields)
            {
                if (read)
                {
                    il.Emit(OpCodes.Ldloc, self);
                    il.Emit(OpCodes.Ldflda, field.Field);
                    il.Emit(OpCodes.Initobj, field.Field.FieldType);
                }

                if (field.Shared)
                {
                    parameters = parameters ?? il.DeclareLocal(typeof(Recorder.Parameters));

                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Callvirt, RecorderSharedMethod);
                    il.Emit(OpCodes.Stloc, parameters);
                    il.Emit(OpCodes.Ldloca, parameters);
                    il.Emit(OpCodes.Ldloc, self);
                    il.Emit(OpCodes.Ldflda, field.Field);
                    il.Emit(OpCodes.Ldstr, field.Label);
                    il.Emit(OpCodes.Call, ParametersRecordMethod.MakeGenericMethod(field.Field.FieldType));
                }
                else
                {
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldloc, self);
                    il.Emit(OpCodes.Ldflda, field.Field);
                    il.Emit(OpCodes.Ldstr, field.Label);
                    il.Emit(OpCodes.Callvirt, RecorderRecordMethod.MakeGenericMethod(field.Field.FieldType));
                }
            }

            il.Emit(OpCodes.Ret);
            return (Action<Recorder, object>)method.CreateDelegate(typeof(Action<Recorder, object>));
        }

        // The fallback where code can't be generated, and the reference the generated code is tested against; like the generated code, it hands the field to Record as a ref and keeps whatever comes back. Recorder.Record<T> has no non-generic form, so this still needs each field's type as a generic argument, which an AOT runtime can only supply for instantiations it compiled ahead of time.
        private static void FieldInterpret<T>(Recorder recorder, object input, FieldInfo field, string label, bool shared, bool read)
        {
            T value = read ? default : (T)field.GetValue(input);

            if (shared)
            {
                recorder.Shared().Record(ref value, label);
            }
            else
            {
                recorder.Record(ref value, label);
            }

            field.SetValue(input, value);
        }
    }
}
