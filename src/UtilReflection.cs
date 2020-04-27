namespace Def
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    internal static class UtilReflection
    {
        internal static FieldInfo GetFieldFromHierarchy(this Type type, string name)
        {
            FieldInfo result = null;
            Type resultType = null;

            Type curType = type;
            while (curType != null)
            {
                FieldInfo typeField = curType.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (typeField != null)
                {
                    if (result == null)
                    {
                        result = typeField;
                        resultType = curType;
                    }
                    else
                    {
                        Dbg.Err($"Found multiple examples of field named {name} in type hierarchy {type}; found in {resultType} and {curType}");
                    }
                }

                curType = curType.BaseType;
            }
            return result;
        }

        internal static IEnumerable<FieldInfo> GetFieldsFromHierarchy(this Type type)
        {
            // this probably needs to be cached

            var seenFields = new HashSet<string>();

            Type curType = type;
            while (curType != null)
            {
                foreach (var field in curType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (!seenFields.Contains(field.Name))
                    {
                        yield return field;
                        seenFields.Add(field.Name);
                    }
                }
                
                curType = curType.BaseType;
            }
        }

        internal static IEnumerable<Type> GetAllTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes());
        }

        internal static bool ReflectionSetForbidden(FieldInfo field)
        {
            // Alright, this isn't exactly complicated right now
            if (field.DeclaringType == typeof(Def))
            {
                return true;
            }

            return false;
        }

        internal struct IndexInfo
        {
            public Type type;
            public FieldInfo field;
        }
        internal static Dictionary<Type, IndexInfo[]> IndexInfoCached = new Dictionary<Type, IndexInfo[]>();
        internal static IndexInfo[] GetIndicesForType(Type type)
        {
            if (IndexInfoCached.TryGetValue(type, out var result))
            {
                // found it in cache, we're done
                return result;
            }

            IndexInfo[] indices = null;

            if (type.BaseType != null)
            {
                indices = GetIndicesForType(type.BaseType);
            }

            FieldInfo matchedField = null;
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (field.GetCustomAttribute<IndexAttribute>() != null)
                {
                    if (matchedField != null)
                    {
                        Dbg.Err($"Too many indices in type {type} (found {matchedField} and {field}); only one will be filled");
                    }

                    matchedField = field;
                }
            }

            if (matchedField != null)
            {
                IndexInfo[] indicesWorking;

                if (indices != null)
                {
                    indicesWorking = new IndexInfo[indices.Length + 1];
                    Array.Copy(indices, indicesWorking, indices.Length);
                }
                else
                {
                    indicesWorking = new IndexInfo[1];
                }

                indicesWorking[indicesWorking.Length - 1] = new IndexInfo { type = type, field = matchedField };

                indices = indicesWorking;
            }

            IndexInfoCached[type] = indices;

            return indices;
        }

        internal static bool IsBackingField(this FieldInfo field)
        {
            // I wish I could find something more authoritative on this.
            return field.Name.StartsWith("<");
        }
    }
}
