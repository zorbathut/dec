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

        private static MethodInfo bindToMethodInfo = typeof(Delegate).GetMethod("BindToMethodInfo", BindingFlags.NonPublic | BindingFlags.Instance);
        private static MethodInfo runtimeMethodHandle_getDeclaringType;

        private static int delegateBindingFlags = 0; //CalculateDelegateBindingFlags();

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
        }

        public override void Write(Delegate input, Recorder recorder)
        {
            var method = input.Method;
            var target = input.Target;
            recorder.Record(ref method, "method");
            recorder.Shared().Record(ref target, "target");
        }

        public override Delegate Create(Recorder recorder)
        {
            // beyond this point, there lie demons
            //return (Delegate)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(localType);

            return new Func<int, bool>((int x) => false);
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
