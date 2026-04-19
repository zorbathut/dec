using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Dec
{
    // Per-element-type cached delegates for the generic collection operations (ISet<T>, Stack<T>, Queue<T>)
    // that Dec has to invoke dynamically during read/write/clone/checksum. The raw MethodInfo.Invoke path is
    // much slower than a cast-and-call delegate on modern .NET, and these run per element on hot collection
    // paths. Trampolines take object / (object, object) to keep the delegate signature type-erased;
    // MakeGenericMethod+CreateDelegate closes over the element type once per type.
    //
    // Set operations go through ISet<T> rather than HashSet<T> so SortedSet<T> and user-defined ISet
    // implementations work too. Stack/Queue are concrete classes with no shared interface that exposes
    // Push/Enqueue/ToArray, so those stay tied to the concrete type.
    //
    // AOT fallback: MakeGenericMethod with unpreserved value-type element types throws on IL2CPP and
    // NativeAOT. When RuntimeFeature.IsDynamicCodeSupported is false, or delegate creation fails, we
    // fall back to a MethodInfo.Invoke-backed wrapper - slower than the fast path, but it still saves
    // the per-call GetMethod lookup and keeps Dec working on AOT runtimes.
    //
    // Thread-safety: ConcurrentDictionary.GetOrAdd's factory may run concurrently under contention.
    // The factories here are idempotent and side-effect-free, so redundant runs are wasteful but safe.
    internal static class UtilCollectionReflect
    {
        private static readonly ConcurrentDictionary<Type, Action<object>> SetClearers = new ConcurrentDictionary<Type, Action<object>>();
        private static readonly ConcurrentDictionary<Type, Action<object, object>> SetAdders = new ConcurrentDictionary<Type, Action<object, object>>();
        private static readonly ConcurrentDictionary<Type, Func<object, object, bool>> SetContainsers = new ConcurrentDictionary<Type, Func<object, object, bool>>();

        private static readonly ConcurrentDictionary<Type, Action<object>> StackClearers = new ConcurrentDictionary<Type, Action<object>>();
        private static readonly ConcurrentDictionary<Type, Action<object, object>> StackPushers = new ConcurrentDictionary<Type, Action<object, object>>();
        private static readonly ConcurrentDictionary<Type, Func<object, int>> StackCounters = new ConcurrentDictionary<Type, Func<object, int>>();
        private static readonly ConcurrentDictionary<Type, Func<object, Array>> StackToArrayers = new ConcurrentDictionary<Type, Func<object, Array>>();

        private static readonly ConcurrentDictionary<Type, Action<object>> QueueClearers = new ConcurrentDictionary<Type, Action<object>>();
        private static readonly ConcurrentDictionary<Type, Action<object, object>> QueueEnqueuers = new ConcurrentDictionary<Type, Action<object, object>>();
        private static readonly ConcurrentDictionary<Type, Func<object, int>> QueueCounters = new ConcurrentDictionary<Type, Func<object, int>>();
        private static readonly ConcurrentDictionary<Type, Func<object, Array>> QueueToArrayers = new ConcurrentDictionary<Type, Func<object, Array>>();

        private static void SetClearTrampoline<T>(object set) => ((ISet<T>)set).Clear();
        private static void SetAddTrampoline<T>(object set, object item) => ((ISet<T>)set).Add((T)item);
        private static bool SetContainsTrampoline<T>(object set, object item) => ((ISet<T>)set).Contains((T)item);

        private static void StackClearTrampoline<T>(object stack) => ((Stack<T>)stack).Clear();
        private static void StackPushTrampoline<T>(object stack, object item) => ((Stack<T>)stack).Push((T)item);
        private static int StackCountTrampoline<T>(object stack) => ((Stack<T>)stack).Count;
        private static Array StackToArrayTrampoline<T>(object stack) => ((Stack<T>)stack).ToArray();

        private static void QueueClearTrampoline<T>(object queue) => ((Queue<T>)queue).Clear();
        private static void QueueEnqueueTrampoline<T>(object queue, object item) => ((Queue<T>)queue).Enqueue((T)item);
        private static int QueueCountTrampoline<T>(object queue) => ((Queue<T>)queue).Count;
        private static Array QueueToArrayTrampoline<T>(object queue) => ((Queue<T>)queue).ToArray();

        private static readonly MethodInfo SetClearMI = typeof(UtilCollectionReflect).GetMethod(nameof(SetClearTrampoline), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo SetAddMI = typeof(UtilCollectionReflect).GetMethod(nameof(SetAddTrampoline), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo SetContainsMI = typeof(UtilCollectionReflect).GetMethod(nameof(SetContainsTrampoline), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo StackClearMI = typeof(UtilCollectionReflect).GetMethod(nameof(StackClearTrampoline), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo StackPushMI = typeof(UtilCollectionReflect).GetMethod(nameof(StackPushTrampoline), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo StackCountMI = typeof(UtilCollectionReflect).GetMethod(nameof(StackCountTrampoline), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo StackToArrayMI = typeof(UtilCollectionReflect).GetMethod(nameof(StackToArrayTrampoline), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo QueueClearMI = typeof(UtilCollectionReflect).GetMethod(nameof(QueueClearTrampoline), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo QueueEnqueueMI = typeof(UtilCollectionReflect).GetMethod(nameof(QueueEnqueueTrampoline), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo QueueCountMI = typeof(UtilCollectionReflect).GetMethod(nameof(QueueCountTrampoline), BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo QueueToArrayMI = typeof(UtilCollectionReflect).GetMethod(nameof(QueueToArrayTrampoline), BindingFlags.NonPublic | BindingFlags.Static);

        private static TDelegate Build<TDelegate>(MethodInfo openMI, Type elementType, Func<TDelegate> aotFallback) where TDelegate : Delegate
        {
            if (RuntimeFeature.IsDynamicCodeSupported)
            {
                try
                {
                    return (TDelegate)openMI.MakeGenericMethod(elementType).CreateDelegate(typeof(TDelegate));
                }
                catch (PlatformNotSupportedException) { /* fall through to AOT path */ }
                catch (NotSupportedException) { /* fall through to AOT path */ }
                catch (InvalidOperationException) { /* fall through to AOT path */ }
            }
            return aotFallback();
        }

        // AOT fallbacks: look up the method on the closed generic collection type and wrap Invoke.
        // These allocate per call (the object[] argument box), so they're slower than the fast path,
        // but avoid MakeGenericMethod entirely.
        private static Action<object> AotClearFallback(Type collectionType)
        {
            var method = collectionType.GetMethod("Clear", Type.EmptyTypes);
            return o => method.Invoke(o, null);
        }

        private static Action<object, object> AotOneArgVoidFallback(Type collectionType, string methodName, Type elementType)
        {
            var method = collectionType.GetMethod(methodName, new[] { elementType });
            return (coll, item) => method.Invoke(coll, new[] { item });
        }

        private static Func<object, object, bool> AotContainsFallback(Type collectionType, Type elementType)
        {
            var method = collectionType.GetMethod("Contains", new[] { elementType });
            return (coll, item) => (bool)method.Invoke(coll, new[] { item });
        }

        private static Func<object, int> AotCountFallback(Type collectionType)
        {
            var property = collectionType.GetProperty("Count");
            return o => (int)property.GetValue(o);
        }

        private static Func<object, Array> AotToArrayFallback(Type collectionType)
        {
            var method = collectionType.GetMethod("ToArray", Type.EmptyTypes);
            return o => (Array)method.Invoke(o, null);
        }

        // Set Clear/Contains are declared on ICollection<T>, not ISet<T>. GetMethod on an interface type
        // only finds directly-declared methods, so we look them up on the declaring interface.
        // Set Clear/Contains are declared on ICollection<T>, not ISet<T>. GetMethod on an interface type
        // only finds directly-declared methods, so we look them up on the declaring interface.
        public static Action<object> SetClear(Type elementType) => SetClearers.GetOrAdd(elementType, t =>
            Build<Action<object>>(SetClearMI, t, () => AotClearFallback(typeof(ICollection<>).MakeGenericType(t))));
        public static Action<object, object> SetAdd(Type elementType) => SetAdders.GetOrAdd(elementType, t =>
            Build<Action<object, object>>(SetAddMI, t, () => AotOneArgVoidFallback(typeof(ISet<>).MakeGenericType(t), "Add", t)));
        public static Func<object, object, bool> SetContains(Type elementType) => SetContainsers.GetOrAdd(elementType, t =>
            Build<Func<object, object, bool>>(SetContainsMI, t, () => AotContainsFallback(typeof(ICollection<>).MakeGenericType(t), t)));

        public static Action<object> StackClear(Type elementType) => StackClearers.GetOrAdd(elementType, t =>
            Build<Action<object>>(StackClearMI, t, () => AotClearFallback(typeof(Stack<>).MakeGenericType(t))));
        public static Action<object, object> StackPush(Type elementType) => StackPushers.GetOrAdd(elementType, t =>
            Build<Action<object, object>>(StackPushMI, t, () => AotOneArgVoidFallback(typeof(Stack<>).MakeGenericType(t), "Push", t)));
        public static Func<object, int> StackCount(Type elementType) => StackCounters.GetOrAdd(elementType, t =>
            Build<Func<object, int>>(StackCountMI, t, () => AotCountFallback(typeof(Stack<>).MakeGenericType(t))));
        public static Func<object, Array> StackToArray(Type elementType) => StackToArrayers.GetOrAdd(elementType, t =>
            Build<Func<object, Array>>(StackToArrayMI, t, () => AotToArrayFallback(typeof(Stack<>).MakeGenericType(t))));

        public static Action<object> QueueClear(Type elementType) => QueueClearers.GetOrAdd(elementType, t =>
            Build<Action<object>>(QueueClearMI, t, () => AotClearFallback(typeof(Queue<>).MakeGenericType(t))));
        public static Action<object, object> QueueEnqueue(Type elementType) => QueueEnqueuers.GetOrAdd(elementType, t =>
            Build<Action<object, object>>(QueueEnqueueMI, t, () => AotOneArgVoidFallback(typeof(Queue<>).MakeGenericType(t), "Enqueue", t)));
        public static Func<object, int> QueueCount(Type elementType) => QueueCounters.GetOrAdd(elementType, t =>
            Build<Func<object, int>>(QueueCountMI, t, () => AotCountFallback(typeof(Queue<>).MakeGenericType(t))));
        public static Func<object, Array> QueueToArray(Type elementType) => QueueToArrayers.GetOrAdd(elementType, t =>
            Build<Func<object, Array>>(QueueToArrayMI, t, () => AotToArrayFallback(typeof(Queue<>).MakeGenericType(t))));
    }
}
