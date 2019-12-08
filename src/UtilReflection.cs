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
    }
}
