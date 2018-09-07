namespace Def
{
    using System;
    using System.Collections.Generic;

    public static class Database
    {
        private static readonly HashSet<Type> Databases = new HashSet<Type>();

        internal static void Register(Def instance)
        {
            var defType = Util.GetDefHierarchyType(instance.GetType());
            var dbType = typeof(Database<>).MakeGenericType(new[] { defType });
            Databases.Add(dbType);

            var regFunction = dbType.GetMethod("Register", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            regFunction.Invoke(null, new[] { instance });

            // TODO: register in a central DB?
        }

        public static void Clear()
        {
            foreach (var db in Databases)
            {
                db.GetMethod("Clear", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke(null, null);
            }
            Databases.Clear();

            Parser.Clear();
        }
    }

    public static class Database<T> where T : Def
    {
        private static readonly List<T> DefList = new List<T>();
        private static readonly Dictionary<string, T> DefLookup = new Dictionary<string, T>();
        
        public static T Get(string name)
        {
            return DefLookup.TryGetValue(name);
        }

        internal static void Register(T instance)
        {
            if (DefLookup.ContainsKey(instance.defName))
            {
                Dbg.Err($"Found repeated def ${typeof(T)}.${instance.defName}");

                // I . . . guess?
                DefList.Remove(DefLookup[instance.defName]);
            }

            DefList.Add(instance);
            DefLookup[instance.defName] = instance;
        }

        internal static void Clear()
        {
            DefList.Clear();
            DefLookup.Clear();
        }
    }
}
