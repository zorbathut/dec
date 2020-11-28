namespace Dec
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Contains information on all decs that exist.
    /// </summary>
    /// <remarks>
    /// This is generally not useful for anything except debug functionality.
    /// </remarks>
    public static class Database
    {
        private static readonly HashSet<Type> Databases = new HashSet<Type>();

        // This is redundant with Database<T>, but it's a lot faster than using reflection
        private static readonly Dictionary<Type, Dictionary<string, Dec>> Lookup = new Dictionary<Type, Dictionary<string, Dec>>();
        private static Dec[] CachedList = null;

        /// <summary>
        /// The total number of decs that exist.
        /// </summary>
        public static int Count
        {
            get
            {
                BuildCaches();
                return CachedList.Length;
            }
        }

        /// <summary>
        /// All decs.
        /// </summary>
        /// <remarks>
        /// Decs are listed in no guaranteed or stable order.
        /// </remarks>
        public static IEnumerable<Dec> List
        {
            get
            {
                BuildCaches();
                return CachedList;
            }
        }

        /// <summary>
        /// Retrieves a dec by base dec type and name.
        /// </summary>
        /// <remarks>
        /// Returns null if no such dec exists.
        /// </remarks>
        public static Dec Get(Type type, string name)
        {
            var typedict = Lookup.TryGetValue(type.GetDecRootType());
            if (typedict == null)
            {
                return null;
            }

            return typedict.TryGetValue(name);
        }

        /// <summary>
        /// Creates a Dec.
        /// </summary>
        /// <remarks>
        /// This will be supported for dynamically-generated Decs in the future, but right now exists mostly for the Composer functionality. It is currently not recommended to use this during actual gameplay.
        ///
        /// At this time, this will not register Indexes. This behavior may change at some point in the future.
        /// </remarks>
        public static Dec Create(Type type, string decName)
        {
            if (!typeof(Dec).IsAssignableFrom(type))
            {
                Dbg.Err($"Attempting to dynamically create a Dec of type {type}, which is not actually a Dec");
                return null;
            }

            // This is definitely not the most efficient way to do this.
            var createMethod = typeof(Database).GetMethod("Create", new[] { typeof(string) });
            var madeMethod = createMethod.MakeGenericMethod(new[] { type });
            return madeMethod.Invoke(null, new[] { decName }) as Dec;
        }

        /// <summary>
        /// Creates a Dec.
        /// </summary>
        /// <remarks>
        /// This will be supported for dynamically-generated Decs in the future, but right now exists mostly for the Composer functionality. It is currently not recommended to use this during actual gameplay.
        ///
        /// At this time, this will not register Indexes. This behavior may change at some point in the future.
        /// </remarks>
        public static T Create<T>(string decName) where T : Dec, new()
        {
            if (Database<T>.Get(decName) != null)
            {
                Dbg.Err($"Attempting to dynamically create {typeof(T)}:{decName} when it already exists");
                return null;
            }

            if (Get(typeof(T).GetDecRootType(), decName) != null)
            {
                Dbg.Err($"Attempting to dynamically create {typeof(T)}:{decName} when a conflicting Dec already exists");
                return null;
            }

            var decInstance = new T();
            decInstance.DecName = decName;

            Register(decInstance);

            return decInstance;
        }

        /// <summary>
        /// Deletes an existing dec.
        /// </summary>
        /// <remarks>
        /// This exists mostly for the Composer functionality. It is generally not recommended to use this during actual gameplay.
        ///
        /// At this time, this will not unregister existing Indexes. This behavior may change at some point in the future.
        /// </remarks>
        public static void Delete(Dec dec)
        {
            if (Get(dec.GetType(), dec.DecName) != dec)
            {
                Dbg.Err($"Attempting to delete {dec} when it either has already been deleted or never existed");
                return;
            }

            Unregister(dec);
        }

        /// <summary>
        /// Renames an existing dec.
        /// </summary>
        /// <remarks>
        /// This exists mostly for the Composer functionality. It is generally not recommended to use this during actual gameplay.
        /// </remarks>
        public static void Rename(Dec dec, string decName)
        {
            if (Get(dec.GetType(), dec.DecName) != dec)
            {
                Dbg.Err($"Attempting to rename what used to be {dec} but is no longer a registered Dec");
                return;
            }

            if (dec.DecName != decName && Get(dec.GetType().GetDecRootType(), decName) != null)
            {
                Dbg.Err($"Attempting to rename {dec} to {decName} when it already exists");
                return;
            }

            Unregister(dec);
            dec.DecName = decName;
            Register(dec);
        }

        /// <summary>
        /// Clears all global dec state, preparing the environment for a new Parser run.
        /// </summary>
        /// <remarks>
        /// This exists mostly for the sake of unit tests. It is generally not recommended to use this during actual gameplay. Be aware that re-parsing XML files will create an entire new set of Dec objects, it will not replace data in existing objects.
        /// </remarks>
        public static void Clear()
        {
            CachedList = null;
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

            Parser.Clear();
            Serialization.Clear();
            Index.Clear();

            UtilReflection.IndexInfoCached.Clear();
        }

        private static void BuildCaches()
        {
            if (CachedList == null)
            {
                CachedList = Lookup.Where(kvp => kvp.Key.GetDecDatabaseStatus() == UtilType.DecDatabaseStatus.Root).SelectMany(kvp => kvp.Value.Values).ToArray();
            }
        }

        private static Dictionary<string, Dec> GetLookupFor(Type type)
        {
            var typedict = Lookup.TryGetValue(type);
            if (typedict == null)
            {
                typedict = new Dictionary<string, Dec>();
                Lookup[type] = typedict;
            }

            return typedict;
        }

        internal static void Register(Dec instance)
        {
            CachedList = null;

            Type registrationType = instance.GetType();

            while (true)
            {
                var dbType = typeof(Database<>).MakeGenericType(new[] { registrationType });
                Databases.Add(dbType);

                var regFunction = dbType.GetMethod("Register", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                regFunction.Invoke(null, new[] { instance });

                // We'll just rely on Database<T> to generate the relevant errors if we're overwriting something
                GetLookupFor(registrationType)[instance.DecName] = instance;

                if (registrationType.GetDecDatabaseStatus() == UtilType.DecDatabaseStatus.Root)
                {
                    break;
                }

                registrationType = registrationType.BaseType;
            }
        }

        internal static void Unregister(Dec instance)
        {
            CachedList = null;

            Type registrationType = instance.GetType();

            while (true)
            {
                var dbType = typeof(Database<>).MakeGenericType(new[] { registrationType });
                Databases.Add(dbType);

                var regFunction = dbType.GetMethod("Unregister", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                regFunction.Invoke(null, new[] { instance });

                var lookup = GetLookupFor(registrationType);
                if (lookup.TryGetValue(instance.DecName) == instance)
                {
                    // It's possible this fails if we've clobbered the database with accidental overwrites from an xml file, but, well, what can you do I guess
                    lookup.Remove(instance.DecName);
                }

                if (registrationType.GetDecDatabaseStatus() == UtilType.DecDatabaseStatus.Root)
                {
                    break;
                }

                registrationType = registrationType.BaseType;
            }
        }
    }

    /// <summary>
    /// Contains information on a single type of Dec.
    /// </summary>
    /// <remarks>
    /// This is often used for object types that should function without being explicitly referenced. As an example, a roguelike might have ArtifactWeaponDec, then - when spawning an artifact weapon - simply choose one out of the full database.
    /// </remarks>
    public static class Database<T> where T : Dec
    {
        private static readonly List<T> DecList = new List<T>();
        private static T[] DecArray = null;
        private static readonly Dictionary<string, T> DecLookup = new Dictionary<string, T>();

        /// <summary>
        /// The number of decs of this type that exist.
        /// </summary>
        public static int Count
        {
            get
            {
                return DecList.Count;
            }
        }

        /// <summary>
        /// All decs of this type.
        /// </summary>
        public static T[] List
        {
            get
            {
                if (DecArray == null)
                {
                    DecArray = DecList.ToArray();
                }

                return DecArray;
            }
        }

        /// <summary>
        /// Returns a dec of this type by name.
        /// </summary>
        /// <remarks>
        /// Returns null if no such dec exists.
        /// </remarks>
        public static T Get(string name)
        {
            return DecLookup.TryGetValue(name);
        }

        internal static void Register(T instance)
        {
            DecArray = null;

            if (DecLookup.ContainsKey(instance.DecName))
            {
                Dbg.Err($"Found repeated dec {typeof(T)}.{instance.DecName}");

                // I . . . guess?
                DecList[DecList.FindIndex(dec => dec == DecLookup[instance.DecName])] = instance;
                DecLookup[instance.DecName] = instance;

                return;
            }

            DecList.Add(instance);
            DecLookup[instance.DecName] = instance;
        }

        internal static void Unregister(T instance)
        {
            DecArray = null;

            if (DecLookup.TryGetValue(instance.DecName) == instance)
            {
                DecLookup.Remove(instance.DecName);
            }

            DecList.Remove(instance);
        }

        internal static void Clear()
        {
            DecList.Clear();
            DecLookup.Clear();
            DecArray = null;
        }
    }
}
