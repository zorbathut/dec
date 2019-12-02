namespace Def
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Linq;

    internal static class Util
    {
        internal static V TryGetValue<T, V>(this Dictionary<T, V> dict, T key)
        {
            dict.TryGetValue(key, out V holder);
            return holder;
        }

        internal static int LineNumber(this XElement element)
        {
            if (element is IXmlLineInfo lineinfo)
            {
                return lineinfo.LineNumber;
            }
            else
            {
                return 0;
            }
        }

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

        internal static string ToCommaString(this IEnumerable<string> list)
        {
            string result = "";
            bool first = true;
            foreach (var str in list)
            {
                if (!first)
                {
                    result += ", ";
                }
                first = false;

                result += str;
            }
            return result;
        }

        internal static XElement ElementNamed(this XElement root, string name)
        {
            return root.Elements().Where(child => child.Name.LocalName == name).SingleOrDefaultChecked();
        }

        internal static T SingleOrDefaultChecked<T>(this IEnumerable<T> elements)
        {
            T result = default(T);
            bool first = true;

            foreach (var element in elements)
            {
                if (first)
                {
                    result = element;
                    first = false;
                }
                else
                {
                    // maybe we need a better error message here.
                    Dbg.Err("Multiple items found when only one is expected");
                }
            }

            return result;
        }

        internal static string GetText(this XElement element)
        {
            bool hasElements = element.Elements().Any();
            bool hasText = element.Nodes().OfType<XText>().Any();
            var text = hasText ? element.Nodes().OfType<XText>().First().Value : null;

            if (hasElements && hasText)
            {
                Dbg.Err($"{element.LineNumber()}: Elements and text are never valid together");
                return null;
            }

            return text;
        }
    }
}
