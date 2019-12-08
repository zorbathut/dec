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
                if (type.BaseType == typeof(Def))
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
    }
}
