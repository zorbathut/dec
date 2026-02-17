using System;
using System.Collections.Generic;
using System.Linq;

namespace Dec
{
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

        // this is for actual valid lookups
        private static Dictionary<object, Path> DecPathLookup = new Dictionary<object, Path>();

        // this is for all lookups, valid or not
        // note: this gets filled from the outside right now
        private static Dictionary<object, Path> DecPathLookupComplete = new Dictionary<object, Path>();

        // these are for all possible reverses
        private static Dictionary<string, object> DecPathLookupReverse = new Dictionary<string, object>();
        private static HashSet<string> DecPathLookupUnusable = new HashSet<string>();
        private static HashSet<string> DecPathLookupConflicts = new HashSet<string>();

        private static HashSet<object> DecForbidden = new HashSet<object>();

        // A lot of this really needs work for validation and error reporting.
        internal static string GetDecPathFromObj(object obj)
        {
            var path = Database.DecPathLookup.TryGetValue(obj);

            if (path == null)
            {
                return null;
            }
            else
            {
                return path.Serialize();
            }
        }

        internal static bool IsForbidden(object obj)
        {
            return DecForbidden.Contains(obj);
        }

        internal static void DecPathRegister(object obj, Path path)
        {
            DecPathLookupComplete[obj] = path;
        }

        internal static object GetFromDecPath(string path)
        {
            return DecPathLookupReverse.TryGetValue(path);
        }

        internal static void DecPathResolveDatabase()
        {
            foreach (var (decObject, decPath) in Database.DecPathLookup)
            {
                var pathSerialized = decPath.Serialize();

                if (Database.DecPathLookupReverse.ContainsKey(pathSerialized))
                {
                    Database.DecPathLookupConflicts.Add(pathSerialized);    // this is currently considered OK because our path system is highly incomplete
                }

                if (!decPath.IsValidForWriting())
                {
                    DecPathLookupUnusable.Add(pathSerialized);
                }

                DecPathLookupReverse[pathSerialized] = decObject;
            }
        }

        /// <summary>
        /// Enables a path lookup with the database.
        /// </summary>
        /// <remarks>
        /// This is a hack job put in because I need the functionality on my own project. Don't be surprised if this behavior changes dramatically at some point.
        /// </remarks>
        public static void DecLookupEnable(object obj)
        {
            if (DecForbidden.Contains(obj))
            {
                Dbg.Err($"Attempting to enable lookup for {obj} which has been explicitly forbidden from recording");
                return;
            }

            DecPathLookup[obj] = DecPathLookupComplete[obj];
            DecPathLookupReverse[DecPathLookupComplete[obj].Serialize()] = obj;

            // needs more validation
        }

        /// <summary>
        /// Registers a custom path lookup with the database.
        /// </summary>
        /// <remarks>
        /// This is a hack job put in because I need the functionality on my own project. Don't be surprised if this behavior changes dramatically at some point.
        /// </remarks>
        public static void DecLookupRegisterCustom(object obj, Path path)
        {
            var serialized = path.Serialize();

            if (DecForbidden.Contains(obj))
            {
                Dbg.Err($"Attempting to register {obj} with path [{serialized}], but it has been explicitly forbidden from recording");
                return;
            }

            if (DecPathLookup.ContainsKey(obj))
            {
                Dbg.Err($"Attempting to register {obj} with path [{serialized}], but it's already registered with path {DecPathLookup[obj].Serialize()}");
                return;
            }

            if (DecPathLookupReverse.ContainsKey(serialized))
            {
                Dbg.Err($"Attempting to register {obj} with path [{serialized}], but that path is already registered to {DecPathLookupReverse[serialized]}");
                return;
            }

            // needs more validation

            DecPathLookup[obj] = path;
            DecPathLookupComplete[obj] = path;
            DecPathLookupReverse[serialized] = obj;
        }

        /// <summary>
        /// Registers an object as forbidden from being Recorded.
        /// </summary>
        /// <remarks>
        /// This is a hack job put in because I need the functionality on my own project. Don't be surprised if this behavior changes dramatically at some point.
        /// </remarks>
        public static void DecRegisterForbid(object obj)
        {
            if (obj.GetType().IsValueType)
            {
                Dbg.Err($"Attempting to forbid {obj} which is a value type; forbidding value types is not supported");
                return;
            }

            if (obj is Dec)
            {
                Dbg.Err($"Attempting to forbid {obj} which is a Dec; forbidding Decs is not currently supported");
                return;
            }

            if (DecForbidden.Contains(obj))
            {
                Dbg.Err($"Attempting to forbid {obj} which has already been forbidden");
                return;
            }

            if (DecPathLookup.ContainsKey(obj))
            {
                Dbg.Err($"Attempting to forbid {obj} which is already registered with path {DecPathLookup[obj].Serialize()}");
                return;
            }

            DecForbidden.Add(obj);
        }

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
                WarnOnEmpty();
                return null;
            }

            var result = typedict.TryGetValue(name);
            if (result == null)
            {
                var remappedName = LookupCompatDecName(type, name);

                if (remappedName != null)
                {
                    result = typedict.TryGetValue(remappedName);
                }
            }

            return result;
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
            // Anyone using these functions hopefully knows what they're doing
            SuppressEmptyWarning();

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
            // Anyone using these functions hopefully knows what they're doing
            SuppressEmptyWarning();

            if (Database<T>.Get(decName) != null)
            {
                Dbg.Err($"Attempting to dynamically create [{typeof(T)}:{decName}] when it already exists");
                return null;
            }

            if (Get(typeof(T).GetDecRootType(), decName) != null)
            {
                Dbg.Err($"Attempting to dynamically create [{typeof(T)}:{decName}] when a conflicting Dec already exists");
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
            // Anyone using these functions hopefully knows what they're doing
            SuppressEmptyWarning();

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
            // Anyone using these functions hopefully knows what they're doing
            SuppressEmptyWarning();

            if (Get(dec.GetType(), dec.DecName) != dec)
            {
                Dbg.Err($"Attempting to rename what used to be {dec} but is no longer a registered Dec");
                return;
            }

            if (dec.DecName != decName && Get(dec.GetType().GetDecRootType(), decName) != null)
            {
                Dbg.Err($"Attempting to rename {dec} to `{decName}` when it already exists");
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
            DecPathLookup.Clear();
            DecPathLookupComplete.Clear();
            DecPathLookupReverse.Clear();
            DecPathLookupUnusable.Clear();
            DecPathLookupConflicts.Clear();
            DecForbidden.Clear();

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

            ParserModular.Clear();
            Serialization.Clear();
            Index.Clear();

            UtilReflection.IndexInfoCached.Clear();

            SuppressEmptyWarningFlag = false;
        }

        private static void BuildCaches()
        {
            if (CachedList == null)
            {
                CachedList = Lookup.Where(kvp => kvp.Key.GetDecDatabaseStatus() == UtilType.DecDatabaseStatus.Root).SelectMany(kvp => kvp.Value.Values).ToArray();

                WarnOnEmpty();
            }
        }

        internal static string LookupCompatDecName(Type type, string originalName)
        {
            var lookup = Config.CompatDecLookup;
            if (lookup.Count == 0)
            {
                return null;
            }

            // Walk up type hierarchy checking each type for a remapping
            Type currentType = type;
            while (currentType != typeof(object))
            {
                if (lookup.TryGetValue(currentType, out var typeRemappings) &&
                    typeRemappings.TryGetValue(originalName, out string remappedName))
                {
                    return remappedName;
                }
                currentType = currentType.BaseType;
            }

            return null;
        }

        /// <summary>
        /// Validates CompatDecLookup configuration and warns about issues.
        /// </summary>
        /// <remarks>
        /// Called after parsing is complete to validate that compat mappings are sensible.
        /// </remarks>
        internal static void ValidateCompatDecLookup()
        {
            // Group mappings by root type to detect conflicts within hierarchies
            var mappingsByRoot = new Dictionary<Type, Dictionary<string, (string newName, Type sourceType)>>();

            foreach (var typeKvp in Config.CompatDecLookup)
            {
                var type = typeKvp.Key;

                // Check if type is a Dec type
                if (!typeof(Dec).IsAssignableFrom(type))
                {
                    Dbg.Wrn($"CompatDecLookup contains type {type}, which is not a Dec type; these entries will be ignored");
                    continue;
                }

                var rootType = type.GetDecRootType();
                if (rootType == null)
                {
                    // Maybe start validating these at some point also?
                    continue;
                }

                // Track mappings for hierarchy conflict detection
                if (!mappingsByRoot.TryGetValue(rootType, out var rootMappings))
                {
                    rootMappings = new Dictionary<string, (string newName, Type sourceType)>();
                    mappingsByRoot[rootType] = rootMappings;
                }

                var typedict = Lookup.TryGetValue(rootType);
                foreach (var nameKvp in typeKvp.Value)
                {
                    var oldName = nameKvp.Key;
                    var newName = nameKvp.Value;

                    // Check if old name already exists (mapping will be ignored)
                    if (typedict.ContainsKey(oldName))
                    {
                        Dbg.Wrn($"CompatDecLookup maps `{type}.{oldName}` to `{type}.{newName}`, but `{type}.{oldName}` already exists; mapping will be ignored");
                    }
                    // Check if new name exists (mapping target is valid)
                    else if (!typedict.ContainsKey(newName))
                    {
                        Dbg.Wrn($"CompatDecLookup maps `{type}.{oldName}` to `{type}.{newName}`, but `{newName}` does not exist (if you want transitive renaming, please come ask on Discord, thanks)");
                    }

                    // Check for conflicting mappings within the same hierarchy
                    if (rootMappings.TryGetValue(oldName, out var existing))
                    {
                        if (existing.newName != newName)
                        {
                            Dbg.Wrn($"CompatDecLookup has conflicting mappings for `{oldName}` within the {rootType} hierarchy: {existing.sourceType} maps to `{existing.newName}`, but {type} maps to `{newName}`");
                        }
                    }
                    else
                    {
                        rootMappings[oldName] = (newName, type);
                    }
                }
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
            SuppressEmptyWarning();

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

        private static bool SuppressEmptyWarningFlag = false;
        internal static void WarnOnEmpty()
        {
            if (SuppressEmptyWarningFlag)
            {
                return;
            }

            SuppressEmptyWarning();
            Dbg.Wrn("You are trying to query the Dec database with no Decs loaded. Perhaps your Parser load step isn't working properly? Recommend reading https://zorbathut.github.io/dec/release/quickstart/setup.html.");
        }

        internal static void SuppressEmptyWarning()
        {
            SuppressEmptyWarningFlag = true;
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
                Database.WarnOnEmpty();
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
                    Database.WarnOnEmpty();
                }

                return DecArray;
            }
        }

        static Database()
        {
            var databaseStatus = UtilType.GetDecDatabaseStatus(typeof(T));

            if (databaseStatus == UtilType.DecDatabaseStatus.Invalid)
            {
                Dbg.Err($"Attempting to create a Database<T> for {typeof(T)}, which is not a valid Dec");
            }
            else if (databaseStatus == UtilType.DecDatabaseStatus.Abstract)
            {
                Dbg.Err($"Attempting to create a Database<T> for {typeof(T)}, which is an abstract Dec and cannot be used as a database root");
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
            var result = DecLookup.TryGetValue(name);
            if (result == null)
            {
                string remappedName = Database.LookupCompatDecName(typeof(T), name);
                if (remappedName != null)
                {
                    result = DecLookup.TryGetValue(remappedName);
                }
            }

            if (result == null)
            {
                Database.WarnOnEmpty();
            }

            return result;
        }

        internal static void Register(T instance)
        {
            DecArray = null;

            if (DecLookup.ContainsKey(instance.DecName))
            {
                Dbg.Err($"Found repeated dec {instance}");

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
