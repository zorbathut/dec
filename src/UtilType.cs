namespace Def
{
    using System;
    using System.Linq;
    using System.Reflection;

    static class UtilType
    {
        // Our Official Type Format:
        // Namespaces are separated by .'s, for example, LowerNamespace.UpperNamespace.ClassName
        // Member classes are also separted by .'s, which means you can have LowerNamespace.ClassName.MemberClass
        // C# wants to do those with +'s, but we're using .'s for readability and XML compatibility reasons.
        // Templates currently use <>'s as you would expect. This isn't compatible with XML tags but I'm just kind of living with it for now.
        // And yes, this also isn't compatible with C#.

        // When serializing types, we chop off as much of the prefix as we can. When deserializing types, we error if there's ambiguity based on existing prefixes.

        private static Type GetTypeFromAnyAssembly(string text)
        {
            // "Distinct" is needed because some types, especially fundamental types, seem to show up in multiple assemblies for unclear reasons
            return AppDomain.CurrentDomain.GetAssemblies().Select(asm => asm.GetType(text)).Where(t => t != null).Distinct().SingleOrDefaultChecked();
        }

        private static Type ParseWithoutNamespace(Type root, string text)
        {
            if (root == null)
            {
                return null;
            }

            if (text.Length == 0)
            {
                return root;
            }

            if (text[0] == '.')
            {
                // This is a member class of a class
                int end = Math.Min(text.IndexOfUnbounded('<', 1), text.IndexOfUnbounded('.', 1));
                string memberName = text.Substring(1, end - 1);
                return ParseWithoutNamespace(root.GetNestedType(memberName, BindingFlags.Public | BindingFlags.NonPublic), text.Substring(end));
            }
            else if (text[0] == '<')
            {
                // This is a template
                return null;
            }
            else
            {
                // nope.
                return null;
            }
        }

        private static Type ParseWithNamespace(string text)
        {
            // At this point we've dealt with the whole Using thing, we just need to deal with this class on its own.

            // We definitely stop at the first < - that has to be a class or we're done for - so let's figure out our stopping point.
            int stringEnd = text.IndexOfUnbounded('<');

            int tokenNext = 0;
            while (true)
            {
                int tokenEnd = text.IndexOf('.', tokenNext);
                if (tokenEnd == -1 || tokenEnd > stringEnd)
                {
                    tokenEnd = stringEnd;
                }

                string token = text.Substring(0, tokenEnd);
                Type parsedType = GetTypeFromAnyAssembly(token);
                if (parsedType != null)
                {
                    return ParseWithoutNamespace(parsedType, text.Substring(tokenEnd));
                }

                tokenNext = tokenEnd + 1;
                if (tokenNext >= stringEnd)
                {
                    // We failed to find anything.
                    return null;
                }
            }
        }

        internal static Type ParseDefFormatted(string text, string inputLine, int lineNumber)
        {
            if (Config.TestParameters?.explicitTypes != null)
            {
                // Test override, we check the test types first
                foreach (var explicitType in Config.TestParameters.explicitTypes)
                {
                    if (text == explicitType.Name)
                    {
                        return explicitType;
                    }
                }
            }

            // We need to find a class that matches the least number of tokens. Namespaces can't be templates so at most this continues until we hit a namespace.
            var possibleTypes = Config.UsingNamespaces
                .Select(ns => ParseWithNamespace($"{ns}.{text}"))
                .Concat(ParseWithNamespace(text))
                .Where(t => t != null)
                .ToArray();

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

        internal static string ComposeDefFormatted(this Type type)
        {
            if (Config.TestParameters?.explicitTypes != null)
            {
                // Test override, we check the test types first
                foreach (var explicitType in Config.TestParameters.explicitTypes)
                {
                    if (type == explicitType)
                    {
                        return explicitType.Name;
                    }
                }
            }

            string baseString = type.FullName.Replace("+", ".");
            string bestPrefix = "";
            foreach (var prefix in Config.UsingNamespaces)
            {
                string prospective = prefix + ".";
                if (bestPrefix.Length < prospective.Length && baseString.StartsWith(prospective))
                {
                    bestPrefix = prospective;
                }
            }

            // TODO: templates.

            return baseString.Substring(bestPrefix.Length);
        }
    }
}
