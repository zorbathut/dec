using System.Reflection;
using System.Runtime.CompilerServices;

namespace Dec
{
    internal delegate void RecordRef<T>(ref T value, Recorder recorder);

    // Whether T is a struct whose Record() a recorder can call in place, without the box that going through IRecordable makes, and the call that does it.
    internal static class RecordableStruct<T>
    {
        private sealed class Info
        {
            internal bool applies;
            internal RecordRef<T> record;
        }

        // Published by one assignment, so a racing reader sees either nothing, and computes it again, or all of it. Computed on first use rather than in a static initializer, so a lookup that throws is retried next time instead of breaking the type for good.
        private static Info Cached;

        private static Info Get()
        {
            var info = Cached;
            if (info == null)
            {
                info = Compute();
                Cached = info;
            }

            return info;
        }

        internal static bool Applies()
        {
            return Get().applies;
        }

        // Valid only when Applies().
        internal static RecordRef<T> Record
        {
            get
            {
                return Get().record;
            }
        }

        // Registered converters can't change this answer, since IRecordable takes precedence over them in ComposeElement, so it never goes stale. Clone's copy-as-is check, which converters can change, comes before it; see CloneRecordedStruct<T>.
        private static Info Compute()
        {
#if NETSTANDARD2_1
            // Unity's AOT builds may not have the delegate's instantiation, or Invoke itself after stripping, while still claiming dynamic code.
            return new Info();
#else
            var type = typeof(T);
            if (!type.IsValueType || !RuntimeFeature.IsDynamicCodeSupported)
            {
                return new Info();
            }

            if (!typeof(IRecordable).IsAssignableFrom(type) || typeof(IConditionalRecordable).IsAssignableFrom(type))
            {
                return new Info();
            }

            if (Serialization.ComposeStrategyFor(type).kind != Serialization.ComposeKind.Recordable)
            {
                return new Info();
            }

            var record = (RecordRef<T>)RecordableStructInvoke.Method.MakeGenericMethod(type).CreateDelegate(typeof(RecordRef<T>));
            return new Info { applies = true, record = record };
#endif
        }
    }

    internal static class RecordableStructInvoke
    {
        internal static readonly MethodInfo Method = typeof(RecordableStructInvoke).GetMethod(nameof(Invoke), BindingFlags.NonPublic | BindingFlags.Static);

        private static void Invoke<U>(ref U value, Recorder recorder) where U : struct, IRecordable
        {
            value.Record(recorder);
        }
    }
}
