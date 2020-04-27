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
        private static Def[] CachedList = null;

        /// <summary>
        /// The total number of defs that exist.
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
        /// All defs.
        /// </summary>
        /// <remarks>
        /// Defs are listed in no guaranteed or stable order.
        /// </remarks>
        public static IEnumerable<Def> List
        {
            get
            {
                BuildCaches();
                return CachedList;
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
            var typedict = Lookup.TryGetValue(type.GetDefRootType());
            if (typedict == null)
            {
                return null;
            }

            return typedict.TryGetValue(name);
        }

        /// <summary>
        /// Creates a Def.
        /// </summary>
        /// <remarks>
        /// This will be supported for dynamically-generated Defs in the future, but right now exists mostly for the Writer functionality. It is currently not recommended to use this during actual gameplay.
        ///
        /// At this time, this will not register Indexes. This behavior may change at some point in the future.
        /// </remarks>
        public static Def Create(Type type, string defName)
        {
            if (!typeof(Def).IsAssignableFrom(type))
            {
                Dbg.Err($"Attempting to dynamically create a Def of type {type}, which is not actually a Def");
                return null;
            }

            // This is definitely not the most efficient way to do this.
            var createMethod = typeof(Database).GetMethod("Create", new[] { typeof(string) });
            var madeMethod = createMethod.MakeGenericMethod(new[] { type });
            return madeMethod.Invoke(null, new[] { defName }) as Def;
        }

        /// <summary>
        /// Creates a Def.
        /// </summary>
        /// <remarks>
        /// This will be supported for dynamically-generated Defs in the future, but right now exists mostly for the Writer functionality. It is currently not recommended to use this during actual gameplay.
        ///
        /// At this time, this will not register Indexes. This behavior may change at some point in the future.
        /// </remarks>
        public static T Create<T>(string defName) where T : Def, new()
        {
            if (Database<T>.Get(defName) != null)
            {
                Dbg.Err($"Attempting to dynamically create {typeof(T)}:{defName} when it already exists");
                return null;
            }

            if (Get(typeof(T).GetDefRootType(), defName) != null)
            {
                Dbg.Err($"Attempting to dynamically create {typeof(T)}:{defName} when a conflicting Def already exists");
                return null;
            }

            var defInstance = new T();
            defInstance.DefName = defName;

            Register(defInstance);

            return defInstance;
        }

        /// <summary>
        /// Deletes an existing def.
        /// </summary>
        /// <remarks>
        /// This exists mostly for the Writer functionality. It is generally not recommended to use this during actual gameplay.
        ///
        /// At this time, this will not unregister existing Indexes. This behavior may change at some point in the future.
        /// </remarks>
        public static void Delete(Def def)
        {
            if (Get(def.GetType(), def.DefName) != def)
            {
                Dbg.Err($"Attempting to delete {def} when it either has already been deleted or never existed");
                return;
            }

            Unregister(def);
        }

        /// <summary>
        /// Renames an existing def.
        /// </summary>
        /// <remarks>
        /// This exists mostly for the Writer functionality. It is generally not recommended to use this during actual gameplay.
        /// </remarks>
        public static void Rename(Def def, string defName)
        {
            if (Get(def.GetType(), def.DefName) != def)
            {
                Dbg.Err($"Attempting to rename what used to be {def} but is no longer a registered Def");
                return;
            }

            if (def.DefName != defName && Get(def.GetType().GetDefRootType(), defName) != null)
            {
                Dbg.Err($"Attempting to rename {def} to {defName} when it already exists");
                return;
            }

            Unregister(def);
            def.DefName = defName;
            Register(def);
        }

        /// <summary>
        /// Clears all global def state, preparing the environment for a new Parser run.
        /// </summary>
        /// <remarks>
        /// This exists mostly for the sake of unit tests. It is generally not recommended to use this during actual gameplay. Be aware that re-parsing XML files will create an entire new set of Def objects, it will not replace data in existing objects.
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
            StaticReferencesAttribute.StaticReferencesFilled.Clear();

            Parser.Clear();
            Serialization.Clear();
            Index.Clear();
        }

        private static void BuildCaches()
        {
            if (CachedList == null)
            {
                CachedList = Lookup.Where(kvp => kvp.Key.GetDefDatabaseStatus() == UtilType.DefDatabaseStatus.Root).SelectMany(kvp => kvp.Value.Values).ToArray();
            }
        }

        private static Dictionary<string, Def> GetLookupFor(Type type)
        {
            var typedict = Lookup.TryGetValue(type);
            if (typedict == null)
            {
                typedict = new Dictionary<string, Def>();
                Lookup[type] = typedict;
            }

            return typedict;
        }

        internal static void Register(Def instance)
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
                GetLookupFor(registrationType)[instance.DefName] = instance;

                if (registrationType.GetDefDatabaseStatus() == UtilType.DefDatabaseStatus.Root)
                {
                    break;
                }

                registrationType = registrationType.BaseType;
            }
        }

        internal static void Unregister(Def instance)
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
                if (lookup.TryGetValue(instance.DefName) == instance)
                {
                    // It's possible this fails if we've clobbered the database with accidental overwrites from an xml file, but, well, what can you do I guess
                    lookup.Remove(instance.DefName);
                }

                if (registrationType.GetDefDatabaseStatus() == UtilType.DefDatabaseStatus.Root)
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

        internal static void Unregister(T instance)
        {
            DefArray = null;

            if (DefLookup.TryGetValue(instance.DefName) == instance)
            {
                DefLookup.Remove(instance.DefName);
            }

            DefList.Remove(instance);
        }

        internal static void Clear()
        {
            DefList.Clear();
            DefLookup.Clear();
            DefArray = null;
        }
    }
}
