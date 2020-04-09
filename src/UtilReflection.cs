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
            Type curType = type;
            while (curType != null)
            {
                foreach (var field in curType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    yield return field;
                }
                
                curType = curType.BaseType;
            }
        }

        internal static Type GetDefHierarchyType(Type type)
        {
            Type origType = type;
            if (type == typeof(Def))
            {
                Dbg.Err("Def objects do not exist in a standalone hierarchy");
                return type;
            }

            while (true)
            {
                if (IsDefHierarchyType(type))
                {
                    return type;
                }

                type = type.BaseType;

                if (type == null)
                {
                    Dbg.Err($"Type {origType} does not inherit from Def");
                    return null;
                }
            }
        }

        internal static bool IsDefHierarchyType(Type type)
        {
            return type.BaseType == typeof(Def);
        }

        internal static bool HasAttribute(this Type type, Type attribute)
        {
            return type.GetCustomAttributes(attribute).Any();
        }

        internal static IEnumerable<Type> GetAllTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes());
        }

        internal static string ToStringDefFormatted(this Type type)
        {
            return type.ToString();
        }

        private static Type GetTypeCallback(Assembly requestedAssembly, string typeName, bool ignoreCase)
        {
            if (requestedAssembly != null)
            {
                return requestedAssembly.GetType(typeName, false, ignoreCase);
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var result = assembly.GetType(typeName, false, ignoreCase);
                if (result != null)
                {
                    return result;
                }
            }
            
            return null;
        }

        internal static Type ParseTypeDefFormatted(string text, string inputLine, int lineNumber)
        {
            var possibleType = Type.GetType(text, null, GetTypeCallback);
            if (possibleType != null)
            {
                return possibleType;
            }

            var possibleTypes = UtilReflection.GetAllTypes().Where(t => t.Name == text || t.FullName == text).ToArray();
            if (possibleTypes.Length == 0)
            {
                Dbg.Err($"{inputLine}:{lineNumber}: Couldn't find type named {text}");
                return null;
            }
            else if (possibleTypes.Length > 1)
            {
                Dbg.Err($"{inputLine}:{lineNumber}: Found too many types named {text} ({possibleTypes.Select(t => t.FullName).ToCommaString()})");
                return possibleTypes[0];
            }
            else
            {
                return possibleTypes[0];
            }
        }

        internal static string ComposeTypeDefFormatted(this Type type)
        {
            return type.Name;
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
