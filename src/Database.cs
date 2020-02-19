namespace Def
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Contains information on all defs that exist.
    /// </summary>
    /// <remarks>
    /// This is generally not useful for anything except debug functionality.
    /// </remarks>
    public static class Database
    {
        private static readonly HashSet<Type> Databases = new HashSet<Type>();

        // This is redundant with Database<T>, but it's a lot faster than using reflection
        private static readonly Dictionary<Type, Dictionary<string, Def>> Lookup = new Dictionary<Type, Dictionary<string, Def>>();
        private static int CachedCount = -1;

        /// <summary>
        /// The total number of defs that exist.
        /// </summary>
        public static int Count
        {
            get
            {
                if (CachedCount == -1)
                {
                    CachedCount = Lookup.Select(kvp => UtilReflection.IsDefHierarchyType(kvp.Key) ? kvp.Value.Count : 0).Sum();
                }

                return CachedCount;
            }
        }

        /// <summary>
        /// All defs.
        /// </summary>
        /// <remarks>
        /// Defs are listed in no guaranteed or stable order.
        /// </remarks>
        public static IEnumerable<Def> List
        {
            get
            {
                return Lookup.Values.SelectMany(v => v.Values);
            }
        }

        /// <summary>
        /// Retrieves a def by base def type and name.
        /// </summary>
        /// <remarks>
        /// Returns null if no such def exists.
        /// </remarks>
        public static Def Get(Type type, string name)
        {
            var typedict = Lookup.TryGetValue(UtilReflection.GetDefHierarchyType(type));
            if (typedict == null)
            {
                return null;
            }

            return typedict.TryGetValue(name);
        }

        /// <summary>
        /// Clears all global def state, preparing the environment for a new Parser run.
        /// </summary>
        /// <remarks>
        /// This exists mostly for the sake of unit tests, but can be used in production as well. Be aware that re-parsing XML files will create an entire new set of Def objects, it will not replace data in existing objects.
        /// </remarks>
        public static void Clear()
        {
            CachedCount = -1;
            Lookup.Clear();

            foreach (var db in Databases)
            {
                db.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
            }
            Databases.Clear();

            foreach (var stat in StaticReferencesAttribute.StaticReferencesFilled)
            {
                foreach (var field in stat.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static))
                {
                    field.SetValue(null, null);
                }
            }
            StaticReferencesAttribute.StaticReferencesFilled.Clear();

            Parser.Clear();
            Serialization.Clear();
            Index.Clear();
        }
        
        internal static void Register(Def instance)
        {
            CachedCount = -1;

            Type registrationType = instance.GetType();

            while (true)
            {
                var dbType = typeof(Database<>).MakeGenericType(new[] { registrationType });
                Databases.Add(dbType);

                var regFunction = dbType.GetMethod("Register", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                regFunction.Invoke(null, new[] { instance });

                var typedict = Lookup.TryGetValue(registrationType);
                if (typedict == null)
                {
                    typedict = new Dictionary<string, Def>();
                    Lookup[registrationType] = typedict;
                }

                // We'll just rely on Database<T> to generate the relevant errors if we're overwriting something
                typedict[instance.DefName] = instance;

                if (UtilReflection.IsDefHierarchyType(registrationType))
                {
                    break;
                }

                registrationType = registrationType.BaseType;
            }
        }
    }

    /// <summary>
    /// Contains information on a single type of Def.
    /// </summary>
    /// <remarks>
    /// This is often used for object types that should function without being explicitly referenced. As an example, a roguelike might have ArtifactWeaponDef, then - when spawning an artifact weapon - simply choose one out of the full database.
    /// </remarks>
    public static class Database<T> where T : Def
    {
        private static readonly List<T> DefList = new List<T>();
        private static T[] DefArray = null;
        private static readonly Dictionary<string, T> DefLookup = new Dictionary<string, T>();

        /// <summary>
        /// The number of defs of this type that exist.
        /// </summary>
        public static int Count
        {
            get
            {
                return DefList.Count;
            }
        }

        /// <summary>
        /// All defs of this type.
        /// </summary>
        public static T[] List
        {
            get
            {
                if (DefArray == null)
                {
                    DefArray = DefList.ToArray();
                }

                return DefArray;
            }
        }

        /// <summary>
        /// Returns a def of this type by name.
        /// </summary>
        /// <remarks>
        /// Returns null if no such def exists.
        /// </remarks>
        public static T Get(string name)
        {
            return DefLookup.TryGetValue(name);
        }

        internal static void Register(T instance)
        {
            DefArray = null;

            if (DefLookup.ContainsKey(instance.DefName))
            {
                Dbg.Err($"Found repeated def {typeof(T)}.{instance.DefName}");

                // I . . . guess?
                DefList[DefList.FindIndex(def => def == DefLookup[instance.DefName])] = instance;
                DefLookup[instance.DefName] = instance;

                return;
            }

            DefList.Add(instance);
            DefLookup[instance.DefName] = instance;
        }

        internal static void Clear()
        {
            DefList.Clear();
            DefLookup.Clear();
            DefArray = null;
        }
    }
}
