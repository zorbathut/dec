namespace Dec.RecorderEnumerator
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    public class System_Func_Converter : ConverterFactory<Delegate>
    {
        public static bool IsGenericTypeFunc(Type type)
        {
            return type == typeof(Func<>) || type == typeof(Func<,>) || type == typeof(Func<,,>) || type == typeof(Func<,,,>) || type == typeof(Func<,,,,>) || type == typeof(Func<,,,,,>) || type == typeof(Func<,,,,,,>) || type == typeof(Func<,,,,,,,>);
        }

        private Type localType;
        private MethodInfo localReturnDefault;

        private static MethodInfo bindToMethodInfo = typeof(Delegate).GetMethod("BindToMethodInfo", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo runtimeMethodHandle_getDeclaringType;

        private static int delegateBindingFlags = 0; //CalculateDelegateBindingFlags();

        private static MethodInfo[] ReturnDefaultLookup = typeof(System_Func_Converter)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Where(m => m.Name == "ReturnDefault" && m.IsGenericMethod)
            .OrderBy(m => m.GetGenericArguments().Length)
            .ToArray();

        static System_Func_Converter()
        {
            runtimeMethodHandle_getDeclaringType = typeof(RuntimeMethodHandle).GetMethod("GetDeclaringType", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(System.Runtime.CompilerServices.RuntimeHelpers).Assembly.GetType("System.IRuntimeMethodInfo") }, null);

            // Get the DelegateBindingFlags enum type directly.
            Type bindingFlagsType = typeof(Delegate).Assembly.GetType("System.DelegateBindingFlags", false, true);

            // Fetch individual enum values
            // These might be needed for more modern runtimes.
            object relaxedSignature = Enum.Parse(bindingFlagsType, "RelaxedSignature");
            //object skipSecurityChecks = Enum.Parse(bindingFlagsType, "SkipSecurityChecks");

            // Combine the enum values using bitwise OR
            delegateBindingFlags = (int)relaxedSignature; // | (int)skipSecurityChecks;
        }

        public System_Func_Converter(Type type)
        {
            localType = type;

            // Find the appropriate ReturnDefault
            var meds = typeof(System_Func_Converter).GetMethods(BindingFlags.NonPublic | BindingFlags.Static);
            var genericArguments = type.GetGenericArguments();
            localReturnDefault = ReturnDefaultLookup[genericArguments.Length - 1].MakeGenericMethod(genericArguments);
        }

        public override void Write(Delegate input, Recorder recorder)
        {
            var method = input.Method;
            var target = input.Target;
            recorder.Record(ref method, "method");
            recorder.Shared().Record(ref target, "target");
        }

        static Result ReturnDefault<Result>() { return default; }
        static Result ReturnDefault<T0, Result>(T0 t0) { return default; }
        static Result ReturnDefault<T0, T1, Result>(T0 t0, T1 t1) { return default; }
        static Result ReturnDefault<T0, T1, T2, Result>(T0 t0, T1 t1, T2 t2) { return default; }
        static Result ReturnDefault<T0, T1, T2, T3, Result>(T0 t0, T1 t1, T2 t2, T3 t3) { return default; }
        static Result ReturnDefault<T0, T1, T2, T3, T4, Result>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4) { return default; }
        static Result ReturnDefault<T0, T1, T2, T3, T4, T5, Result>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5) { return default; }
        static Result ReturnDefault<T0, T1, T2, T3, T4, T5, T6, Result>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6) { return default; }
        static Result ReturnDefault<T0, T1, T2, T3, T4, T5, T6, T7, Result>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7) { return default; }


        public override Delegate Create(Recorder recorder)
        {
            // We need to create an appropriate Delegate, but this is hard to do; Delegate refuses to play nice with GetUninitializedObject and its constructor does a lot of error checking.
            // So instead, we just create a Delegate straight out of a MethodInfo with the appropriate prototype.
            // We'll swap out the guts of the Delegate later.
            return localReturnDefault.CreateDelegate(localType, null);
        }

        public override void Read(ref Delegate input, Recorder recorder)
        {
            MethodInfo method = null;
            object target = null;
            recorder.Record(ref method, "method");
            recorder.Shared().Record(ref target, "target");

            // now we have to actually jam this into our fake uninitialized Delegate
            // this is shamelessly cannibalized from https://github.com/microsoft/referencesource/blob/master/mscorlib/system/delegate.cs
            bindToMethodInfo.Invoke(input, new object[] { target, method, runtimeMethodHandle_getDeclaringType.Invoke(null, new object[] { method }), delegateBindingFlags });
        }
    }
}
