namespace Def
{
    using System;
    using System.Collections.Generic;
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

        private struct PrimitiveTypeLookup
        {
            public Type type;
            public string str;
        }
        private static readonly PrimitiveTypeLookup[] PrimitiveTypes = new PrimitiveTypeLookup[]
        {
            new PrimitiveTypeLookup { type = typeof(int), str = "int" },
            new PrimitiveTypeLookup { type = typeof(byte), str = "byte" },
            new PrimitiveTypeLookup { type = typeof(sbyte), str = "sbyte" },
            new PrimitiveTypeLookup { type = typeof(char), str = "char" },
            new PrimitiveTypeLookup { type = typeof(decimal), str = "decimal" },
            new PrimitiveTypeLookup { type = typeof(double), str = "double" },
            new PrimitiveTypeLookup { type = typeof(float), str = "float" },
            new PrimitiveTypeLookup { type = typeof(int), str = "int" },
            new PrimitiveTypeLookup { type = typeof(uint), str = "uint" },
            new PrimitiveTypeLookup { type = typeof(long), str = "long" },
            new PrimitiveTypeLookup { type = typeof(ulong), str = "ulong" },
            new PrimitiveTypeLookup { type = typeof(short), str = "short" },
            new PrimitiveTypeLookup { type = typeof(ushort), str = "ushort" },
            new PrimitiveTypeLookup { type = typeof(object), str = "object" },
            new PrimitiveTypeLookup { type = typeof(string), str = "string" },
        };

        private static bool MatchesWithGeneric(string typeName, string defType)
        {
            return typeName.StartsWith(defType) && typeName[defType.Length] == '`';
        }

        private static Type GetTypeFromAnyAssembly(string text)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var types = assemblies.Select(asm =>
                {
                    {
                        var type = asm.GetType(text);
                        if (type != null)
                        {
                            return type;
                        }
                    }

                    // We haven't found a type, but it's possible that we have a generic that it doesn't recognize.
                    // This is unnecessarily slow and I absolutely need to fix it. For now? Let's just check everything!
                    foreach (var type in asm.GetTypes())
                    {
                        if (MatchesWithGeneric(type.FullName, text))
                        {
                            return type;
                        }
                    }

                    return null;
                });

            // "Distinct" is needed because some types, especially primitive types, seem to show up in multiple assemblies for unclear reasons
            return types.Where(t => t != null).Distinct().SingleOrDefaultChecked();
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

                // Get our type
                Type chosenType = root.GetNestedType(memberName, BindingFlags.Public | BindingFlags.NonPublic);

                // Ho ho! We have gotten a type! It has definitely not failed!
                // Unless it's a generic type in which case it might have failed.
                if (chosenType == null)
                {
                    foreach (var prospectiveType in root.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (MatchesWithGeneric(prospectiveType.Name, memberName))
                        {
                            chosenType = prospectiveType;
                            break;
                        }
                    }
                }

                // Chain on to another call in case we have a further-nested class-of-class
                return ParseWithoutNamespace(chosenType, text.Substring(end));
            }
            else if (text[0] == '<')
            {
                if (!root.IsGenericType)
                {
                    Dbg.Err($"Found generic specification on non-generic type {root}");
                    return null;
                }

                // This is a template
                // Parsing this is going to be a bit tricky; in theory it's C#-regex-able but I haven't been able to find a good simple example
                // So we're doing it by hand
                // Which is slow
                // Definitely gonna want caching for this at some point.
                var parsedTypes = new List<Type>();
                void AddParsedType(string type)
                {
                    parsedTypes.Add(ParseDefFormatted(type.Trim(), "TBD", -1));
                }

                int tokenStart = 1;
                while (tokenStart < text.Length && text[tokenStart] != '>')
                {
                    int tokenEnd = tokenStart;

                    int nestedBrackets = 0;
                    while (tokenEnd < text.Length)
                    {
                        // just so we can stop calling this function
                        char kar = text[tokenEnd];

                        if (kar == ',')
                        {
                            AddParsedType(text.Substring(tokenStart, tokenEnd - tokenStart));
                            tokenStart = tokenEnd + 1;
                            tokenEnd = tokenStart;
                            continue;
                        }
                        else if (kar == '<')
                        {
                            ++nestedBrackets;
                        }
                        else if (kar == '>' && nestedBrackets > 0)
                        {
                            --nestedBrackets;
                        }
                        else if (kar == '>')
                        {
                            // we have reached the end of the templates
                            AddParsedType(text.Substring(tokenStart, tokenEnd - tokenStart));
                            tokenStart = tokenEnd;
                            break;
                        }

                        // consume another character!
                        ++tokenEnd;
                    }
                }

                if (tokenStart >= text.Length || text[tokenStart] != '>')
                {
                    Dbg.Err("Failed to find closing angle bracket in type");
                    return null;
                }

                // Alright, we have a valid set of brackets, and a parsed set of types!
                Type specifiedType = root.MakeGenericType(parsedTypes.ToArray());

                // yay!

                // We also might have more type to parse.
                return ParseWithoutNamespace(specifiedType, text.Substring(tokenStart + 1));
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
                // We're probably not going to be parsing primitive types often, so we stash this loop in the failure case to avoid its overhead.
                // Also, these are keywords, so you can't define classes with these names without doing hideous @hackery.
                // and if you're doing that, then causing a type conflict, then *complaining that it isn't giving you a warning*, then screw you anyway :V
                for (int i = 0; i < PrimitiveTypes.Length; ++i)
                {
                    if (text == PrimitiveTypes[i].str)
                    {
                        return PrimitiveTypes[i].type;
                    }
                }

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

            // We're going to have to do this entire loop at some point anyway, so we may as well do it now when we're just comparing Types
            for (int i = 0; i < PrimitiveTypes.Length; ++i)
            {
                if (type == PrimitiveTypes[i].type)
                {
                    return PrimitiveTypes[i].str;
                }
            }

            Type baseType = type;
            if (type.IsConstructedGenericType)
            {
                baseType = type.GetGenericTypeDefinition();
            }

            string baseString = baseType.FullName.Replace("+", ".");
            string bestPrefix = "";
            foreach (var prefix in Config.UsingNamespaces)
            {
                string prospective = prefix + ".";
                if (bestPrefix.Length < prospective.Length && baseString.StartsWith(prospective))
                {
                    bestPrefix = prospective;
                }
            }

            // Strip out the generic parameter count
            int genericVariableSpecifier = baseString.IndexOfUnbounded('`');

            string baseTypeString = baseString.Substring(bestPrefix.Length, genericVariableSpecifier - bestPrefix.Length);

            if (type.IsConstructedGenericType)
            {
                // Assemble the generic types on top of this
                string genericTypes = string.Join(", ", type.GenericTypeArguments.Select(t => t.ComposeDefFormatted()));
                return $"{baseTypeString}<{genericTypes}>";
            }
            else
            {
                return baseTypeString;
            }
        }
    }
}
