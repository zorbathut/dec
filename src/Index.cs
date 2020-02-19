namespace Def
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal static class Index
    {
        private static readonly HashSet<Type> Indices = new HashSet<Type>();

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
        }

        internal static int Preregister(Type indexType)
        {
            var dbType = typeof(Index<>).MakeGenericType(new[] { indexType });
            Indices.Add(dbType);

            var regFunction = dbType.GetMethod("Preregister", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            return (int)regFunction.Invoke(null, null);
        }

        internal static void Register(Type indexType, object instance)
        {
            var dbType = typeof(Index<>).MakeGenericType(new[] { indexType });
            Indices.Add(dbType);

            var regFunction = dbType.GetMethod("Register", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            regFunction.Invoke(null, new[] { instance });
        }
    }

    /// <summary>
    /// Contains information on a single type of index.
    /// </summary>
    /// <remarks>
    /// This is often used when doing high-performance processing over all elements of a type. See [Indexes](/articles/doc_indexes.html) for more details.
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
        /// All defs of this type.
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
        /// Returns a def of this type by name.
        /// </summary>
        /// <remarks>
        /// Returns null if no such def exists.
        /// </remarks>
        public static T Get(int index)
        {
            return (IndexArray ?? List)[index];
        }

        internal static int Preregister()
        {
            return IndexList.Count;
        }

        internal static void Register(T instance)
        {
            IndexArray = null;
            IndexList.Add(instance);
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
    /// See [Indexes](/articles/doc_indexes.html) for more information.
    /// </remarks>
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class IndexAttribute : System.Attribute
    {

    }
}
