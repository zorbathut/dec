using System;
using System.Collections.Generic;
using System.Reflection;

namespace Dec
{
    internal static class Index
    {
        private static readonly HashSet<Type> Indices = new HashSet<Type>();

        // NOTE: This whole thing came across when I was trying to pass a reference to a struct into a function.
        // As near as I can tell, the important part is that you ref it *once*, not twice. If you use ref again you end up with a reference to the struct's box, which I suppose makes sense.
        // While trying to figure this out I ended up building this little layer which also functions as a cache layer. It's probably a nice speed boost.
        // It's also probably unnecessary, but, whatever, it's written, just gonna leave it in place.
        // If this code seems silly: you're not wrong..
        private static Dictionary<Type, Action<object, FieldInfo>> RegisterFunctions = new Dictionary<Type, Action<object, FieldInfo>>();

        /// <summary>
        /// Clears all index state, preparing the environment for a new Parser run.
        /// </summary>
        /// <remarks>
        /// This exists mostly for the sake of unit tests, but can be used in production as well.
        /// </remarks>
        public static void Clear()
        {
            foreach (var db in Indices)
            {
                db.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
            }
            Indices.Clear();
            RegisterFunctions.Clear();
        }

        internal static void Register(ref object instance)
        {
            var indices = UtilReflection.GetIndicesForType(instance.GetType());
            if (indices != null)
            {
                for (int i = 0; i < indices.Length; ++i)
                {
                    var registerFunction = RegisterFunctions.TryGetValue(indices[i].type);
                    if (registerFunction == null)
                    {
                        var dbType = typeof(Index<>).MakeGenericType(new[] { indices[i].type });
                        Indices.Add(dbType);

                        registerFunction = dbType.GetMethod("Register", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).CreateDelegate(typeof(Action<object, FieldInfo>)) as Action<object, FieldInfo>;
                        RegisterFunctions[indices[i].type] = registerFunction;
                    }

                    registerFunction(instance, indices[i].field);
                }
            }
        }
    }

    /// <summary>
    /// Contains information on a single type of index.
    /// </summary>
    /// <remarks>
    /// This is often used when doing high-performance processing over all elements of a type. See [Indexes](~/documentation/indexes.md) for more details.
    /// </remarks>
    public static class Index<T>
    {
        // Stores index lists while generating the indices.
        private static readonly List<T> IndexList = new List<T>();

        // Stores index lists after generation, for a barely-relevant performance gain.
        private static T[] IndexArray = null;

        /// <summary>
        /// The number of indices of this type that exist.
        /// </summary>
        public static int Count
        {
            get
            {
                return IndexList.Count;
            }
        }

        /// <summary>
        /// All decs of this type.
        /// </summary>
        public static T[] List
        {
            get
            {
                if (IndexArray == null)
                {
                    IndexArray = IndexList.ToArray();
                }

                return IndexArray;
            }
        }

        /// <summary>
        /// Returns a dec of this type by name.
        /// </summary>
        /// <remarks>
        /// Returns null if no such dec exists.
        /// </remarks>
        public static T Get(int index)
        {
            return (IndexArray ?? List)[index];
        }

        internal static void Register(object instance, FieldInfo field)
        {
            // Clear our cached info
            IndexArray = null;

            // Set the appropriate member
            field.SetValue(instance, IndexList.Count);

            // Add this to the list
            IndexList.Add((T)instance);
        }

        internal static void Clear()
        {
            IndexList.Clear();
            IndexArray = null;
        }
    }

    /// <summary>
    /// Applied to mark an `int` member as an index.
    /// </summary>
    /// <remarks>
    /// See [Indexes](~/documentation/indexes.md) for more information.
    /// </remarks>
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class IndexAttribute : System.Attribute
    {

    }
}
