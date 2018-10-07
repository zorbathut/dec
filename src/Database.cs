namespace Def
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class Database
    {
        private static readonly HashSet<Type> Databases = new HashSet<Type>();

        // This is redundant with Database<T>, but it's a lot faster than using reflection
        private static readonly Dictionary<Type, Dictionary<string, Def>> Lookup = new Dictionary<Type, Dictionary<string, Def>>();

        public static int Count
        {
            get
            {
                return Lookup.Values.Select(x => x.Count).Sum();
            }
        }

        public static IEnumerable<Def> List
        {
            get
            {
                return Lookup.Values.SelectMany(v => v.Values);
            }
        }

        public static Def Get(Type type, string name)
        {
            var typedict = Lookup.TryGetValue(Util.GetDefHierarchyType(type));
            if (typedict == null)
            {
                return null;
            }

            return typedict.TryGetValue(name);
        }

        public static void Clear()
        {
            Lookup.Clear();

            foreach (var db in Databases)
            {
                db.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
            }
            Databases.Clear();

            foreach (var stat in StaticReferences.StaticReferencesFilled)
            {
                foreach (var field in stat.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static))
                {
                    field.SetValue(null, null);
                }
            }
            StaticReferences.StaticReferencesFilled.Clear();

            Parser.Clear();
        }
        
        internal static void Register(Def instance)
        {
            var defType = Util.GetDefHierarchyType(instance.GetType());
            var dbType = typeof(Database<>).MakeGenericType(new[] { defType });
            Databases.Add(dbType);

            var regFunction = dbType.GetMethod("Register", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            regFunction.Invoke(null, new[] { instance });

            var typedict = Lookup.TryGetValue(defType);
            if (typedict == null)
            {
                typedict = new Dictionary<string, Def>();
                Lookup[defType] = typedict;
            }

            // We'll just rely on Database<T> to generate the relevant errors if we're overwriting something
            typedict[instance.defName] = instance;
        }
    }

    public static class Database<T> where T : Def
    {
        private static readonly List<T> DefList = new List<T>();
        private static T[] DefArray = null;
        private static readonly Dictionary<string, T> DefLookup = new Dictionary<string, T>();

        public static int Count
        {
            get
            {
                return DefList.Count;
            }
        }

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
        
        public static T Get(string name)
        {
            return DefLookup.TryGetValue(name);
        }

        internal static void Register(T instance)
        {
            if (DefLookup.ContainsKey(instance.defName))
            {
                Dbg.Err($"Found repeated def {typeof(T)}.{instance.defName}");

                // I . . . guess?
                int index = DefList.FindIndex(def => def == DefLookup[instance.defName]);

                instance.index = index;
                DefList[index] = instance;
                DefLookup[instance.defName] = instance;

                return;
            }

            instance.index = DefList.Count;
            DefList.Add(instance);
            DefLookup[instance.defName] = instance;

            DefArray = null;
        }

        internal static void Clear()
        {
            DefList.Clear();
            DefLookup.Clear();
            DefArray = null;
        }
    }
}
