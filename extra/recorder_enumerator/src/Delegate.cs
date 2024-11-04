using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Dec.RecorderEnumerator
{
    public class System_Delegate_Converter : ConverterFactory<Delegate>
    {
        public static bool IsGenericDelegate(Type type)
        {
            return type == typeof(Func<>) || type == typeof(Func<,>) || type == typeof(Func<,,>) || type == typeof(Func<,,,>) || type == typeof(Func<,,,,>) || type == typeof(Func<,,,,,>) || type == typeof(Func<,,,,,,>) || type == typeof(Func<,,,,,,,>) || type == typeof(Action) || type == typeof(Action<>) || type == typeof(Action<,>) || type == typeof(Action<,,>) || type == typeof(Action<,,,>) || type == typeof(Action<,,,,>) || type == typeof(Action<,,,,,>) || type == typeof(Action<,,,,,,>) || type == typeof(Action<,,,,,,,>);
        }

        public static bool IsNonGenericDelegate(Type type)
        {
            return type == typeof(Action);
        }

        public override bool TreatAsValuelike()
        {
            return true;
        }

        private Type localType;
        private MethodInfo localReturnDefault;

        private static MethodInfo bindToMethodInfo = typeof(Delegate).GetMethod("BindToMethodInfo", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo runtimeMethodHandle_getDeclaringType;

        private static int delegateBindingFlags = 0; //CalculateDelegateBindingFlags();

        private static MethodInfo[] ReturnDefaultLookup = typeof(System_Delegate_Converter)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Where(m => m.Name == "ReturnDefault" && m.IsGenericMethod)
            .OrderBy(m => m.GetGenericArguments().Length)
            .ToArray();

        private static MethodInfo[] DoNothingLookup = typeof(System_Delegate_Converter)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Where(m => m.Name == "DoNothing")
            .OrderBy(m => m.IsGenericMethod ? m.GetGenericArguments().Length : 0)
            .ToArray();

        static System_Delegate_Converter()
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

        public System_Delegate_Converter(Type type)
        {
            localType = type;

            // Find the appropriate ReturnDefault
            if (type.Name.Contains("Func"))
            {
                // Func<>
                var genericArguments = type.GetGenericArguments();
                localReturnDefault = ReturnDefaultLookup[genericArguments.Length - 1].MakeGenericMethod(genericArguments);
            }
            else if (type.IsGenericType)
            {
                // Action<>
                var genericArguments = type.GetGenericArguments();
                localReturnDefault = DoNothingLookup[genericArguments.Length].MakeGenericMethod(genericArguments);
            }
            else
            {
                // Action (no parameters)
                localReturnDefault = DoNothingLookup[0];
            }
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

        static void DoNothing() { }
        static void DoNothing<T0>(T0 t0) { }
        static void DoNothing<T0, T1>(T0 t0, T1 t1) { }
        static void DoNothing<T0, T1, T2>(T0 t0, T1 t1, T2 t2) { }
        static void DoNothing<T0, T1, T2, T3>(T0 t0, T1 t1, T2 t2, T3 t3) { }
        static void DoNothing<T0, T1, T2, T3, T4>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4) { }
        static void DoNothing<T0, T1, T2, T3, T4, T5>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5) { }
        static void DoNothing<T0, T1, T2, T3, T4, T5, T6>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6) { }
        static void DoNothing<T0, T1, T2, T3, T4, T5, T6, T7>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7) { }

        public override Delegate Create(Recorder recorder)
        {
            // We need to create an appropriate Delegate, but this is hard to do; Delegate refuses to play nice with GetUninitializedObject and its constructor does a lot of error checking.
            // So instead, we just create a Delegate straight out of a MethodInfo with the appropriate prototype.
            // We'll replace the Delegate later.
            return localReturnDefault.CreateDelegate(localType, null);
        }

        public override void Read(ref Delegate input, Recorder recorder)
        {
            MethodInfo method = null;
            object target = null;
            recorder.Record(ref method, "method");
            recorder.Shared().Record(ref target, "target");

            // BEHOLD
            // This is an order of magnitude easier than trying to do cutesy things with reflection to replace the guts.
            // However, this is viable *only* because of TreatAsValuelike(), which ensures that it isn't shared.
            input = method.CreateDelegate(localType, target);
        }
    }
}
