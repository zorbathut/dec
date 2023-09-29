namespace Dec
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;

    internal static class UtilType
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

        private static bool MatchesWithGeneric(string typeName, string decType)
        {
            return typeName.StartsWith(decType) && typeName[decType.Length] == '`';
        }

        private static Regex GenericParameterMatcher = new Regex("`[0-9]+", RegexOptions.Compiled);
        private static Dictionary<string, Type[]> StrippedTypeCache = null;
        private static Type GetTypeFromAnyAssembly(string text, InputContext context)
        {
            // This is technically unnecessary if we're not parsing a generic, but we may as well do it because the cache will still be faster for nongenerics.
            // If we really wanted a perf boost here, we'd do one pass for non-template objects, then do it again on a cache miss to fill it with template stuff.
            // But there's probably much better performance boosts to be seen throughout this.
            if (StrippedTypeCache == null)
            {
                StrippedTypeCache = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(asm => {
                        try
                        {
                            return asm.GetTypes();
                        }
                        catch (ReflectionTypeLoadException reflectionException)
                        {
                            // This is very difficult to code-coverage - it happens on some platforms sometimes, but not on our automatic test server.
                            // To test this, we'd have to create a fake .dll that existed just to trigger this issue.
                            return reflectionException.Types.Where(t => t != null);
                        }
                    })
                    .Distinct()
                    .GroupBy(t => GenericParameterMatcher.Replace(t.FullName, ""))
                    .ToDictionary(
                        group => group.Key,
                        group => group.ToArray());
            }

            var result = StrippedTypeCache.TryGetValue(text);

            if (result == null)
            {
                return null;
            }
            else if (result.Length == 1)
            {
                return result[0];
            }
            else
            {
                Dbg.Err($"{context}: Too many types found with name {text}");
                return result[0];
            }
        }

        private static Type ParseWithoutNamespace(Type root, string text, InputContext context)
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
                // Might actually start with < if it's compiler-generated!
                int end = Math.Min(text.IndexOfUnbounded('<', 2), text.IndexOfUnbounded('.', 1));
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
                return ParseWithoutNamespace(chosenType, text.Substring(end), context);
            }
            else if (text[0] == '<')
            {
                if (!root.IsGenericTypeDefinition)
                {
                    Dbg.Err($"{context}: Found generic specification on non-generic type {root}");
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
                    parsedTypes.Add(ParseDecFormatted(type.Trim(), context));
                }

                int tokenStart = 1;
                while (tokenStart < text.Length && text[tokenStart] != '>')
                {
                    int tokenEnd = tokenStart;

                    int nestedBrackets = 0;
                    while (true)
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

                        if (tokenEnd >= text.Length)
                        {
                            // We've hit the end; this is a failure, but it'll be picked up by the below error
                            tokenStart = tokenEnd;
                            break;
                        }
                    }
                }

                if (tokenStart >= text.Length || text[tokenStart] != '>')
                {
                    Dbg.Err($"{context}: Failed to find closing angle bracket in type");
                    return null;
                }

                if (parsedTypes.Count != root.GetGenericArguments().Length)
                {
                    Dbg.Err($"{context}: Wrong number of generic arguments for type {root}");
                    return null;
                }

                // Alright, we have a valid set of brackets, and a parsed set of types!
                Type specifiedType = root.MakeGenericType(parsedTypes.ToArray());

                // yay!

                // We also might have more type to parse.
                return ParseWithoutNamespace(specifiedType, text.Substring(tokenStart + 1), context);
            }
            else
            {
                // nope.
                return null;
            }
        }

        private static Type ParseWithNamespace(string text, InputContext context)
        {
            // At this point we've dealt with the whole Using thing, we just need to deal with this class on its own.

            // This is now extra complicated because compiler tools can generate class names with <'s in them.
            // So far, at least, the < is always the first character in the class name

            // We definitely stop at the first < - that has to be a class or we're done for - so let's figure out our stopping point.
            int stringEnd = text.IndexOfUnbounded('<');
            while (stringEnd > 0 && stringEnd < text.Length && text[stringEnd - 1] == '.')
            {
                // get the next <
                stringEnd = text.IndexOfUnbounded('<', stringEnd + 1);
            }

            int tokenNext = 0;
            while (true)
            {
                int tokenEnd = text.IndexOf('.', tokenNext);
                if (tokenEnd == -1 || tokenEnd > stringEnd)
                {
                    tokenEnd = stringEnd;
                }

                string token = text.Substring(0, tokenEnd);
                Type parsedType = GetTypeFromAnyAssembly(token, context);
                if (parsedType != null)
                {
                    return ParseWithoutNamespace(parsedType, text.Substring(tokenEnd), context);
                }

                tokenNext = tokenEnd + 1;
                if (tokenNext >= stringEnd)
                {
                    // We failed to find anything.
                    return null;
                }
            }
        }

        private static Dictionary<string, Type> ParseCache = new Dictionary<string, Type>();
        internal static Type ParseDecFormatted(string text, InputContext context)
        {
            if (ParseCache.TryGetValue(text, out Type cacheVal))
            {
                return cacheVal;
            }

            if (Config.TestParameters?.explicitTypes != null)
            {
                // Test override, we check the test types first
                foreach (var explicitType in Config.TestParameters.explicitTypes)
                {
                    if (text == explicitType.Name)
                    {
                        ParseCache[text] = explicitType;
                        return explicitType;
                    }
                }
            }

            bool isArray = text.EndsWith("[]");
            if (isArray)
            {
                text = text.Substring(0, text.Length - 2);
            }

            // We need to find a class that matches the least number of tokens. Namespaces can't be templates so at most this continues until we hit a namespace.
            var possibleTypes = Config.UsingNamespaces
                .Select(ns => ParseWithNamespace($"{ns}.{text}", context))
                .Concat(ParseWithNamespace(text, context))
                .Where(t => t != null)
                .ToArray();

            Type result;
            if (possibleTypes.Length == 0)
            {
                Dbg.Err($"{context}: Couldn't find type named `{text}`");
                result = null;
            }
            else if (possibleTypes.Length > 1)
            {
                Dbg.Err($"{context}: Found too many types named `{text}` ({possibleTypes.Select(t => t.FullName).ToCommaString()})");
                result = possibleTypes[0];
            }
            else
            {
                result = possibleTypes[0];
            }

            if (isArray)
            {
                // TODO: multiple-dimension arrays?
                result = result.MakeArrayType();
            }

            ParseCache[text] = result;
            return result;
        }

        private static Dictionary<Type, string> ComposeDecCache = new Dictionary<Type, string>();
        internal static string ComposeDecFormatted(this Type type)
        {
            if (ComposeDecCache.TryGetValue(type, out string cacheVal))
            {
                return cacheVal;
            }

            if (Config.TestParameters?.explicitTypes != null)
            {
                // Test override, we check the test types first
                foreach (var explicitType in Config.TestParameters.explicitTypes)
                {
                    if (type == explicitType)
                    {
                        string result = explicitType.Name;
                        ComposeDecCache[type] = result;
                        return result;
                    }
                }
            }

            {
                // Main parsing
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

                string result;
                if (type.IsConstructedGenericType)
                {
                    // Assemble the generic types on top of this
                    string genericTypes = string.Join(", ", type.GenericTypeArguments.Select(t => t.ComposeDecFormatted()));
                    result = $"{baseTypeString}<{genericTypes}>";
                }
                else
                {
                    result = baseTypeString;
                }

                ComposeDecCache[type] = result;
                return result;
            }
        }

        private static Dictionary<Type, string> ComposeCSCache = new Dictionary<Type, string>();
        internal static string ComposeCSFormatted(this Type type)
        {
            if (ComposeCSCache.TryGetValue(type, out string cacheVal))
            {
                return cacheVal;
            }

            string result;
            if (type.IsConstructedGenericType)
            {
                result = type.GetGenericTypeDefinition().ToString();
                result = result.Substring(0, result.IndexOf('`'));
            }
            else
            {
                result = type.ToString();
            }

            // strip out the namespace/class distinction
            result = result.Replace('+', '.');

            if (type.IsConstructedGenericType)
            {
                result += "<" + string.Join(", ", type.GetGenericArguments().Select(arg => ComposeCSFormatted(arg))) + ">";
            }

            ComposeCSCache[type] = result;

            return result;
        }

        internal static void ClearCache()
        {
            ComposeDecCache.Clear();
            ComposeCSCache.Clear();
            ParseCache.Clear();
            StrippedTypeCache = null;

            // Seed with our primitive types
            for (int i = 0; i < PrimitiveTypes.Length; ++i)
            {
                ComposeDecCache[PrimitiveTypes[i].type] = PrimitiveTypes[i].str;
                ParseCache[PrimitiveTypes[i].str] = PrimitiveTypes[i].type;
            }
        }

        static UtilType()
        {
            // seed the cache
            ClearCache();
        }

        internal enum DecDatabaseStatus
        {
            Invalid,
            Abstract,
            Root,
            Branch,
        }
        private static Dictionary<Type, DecDatabaseStatus> GetDecDatabaseStatusCache = new Dictionary<Type, DecDatabaseStatus>();
        internal static DecDatabaseStatus GetDecDatabaseStatus(this Type type)
        {
            if (!GetDecDatabaseStatusCache.TryGetValue(type, out var result))
            {
                if (!typeof(Dec).IsAssignableFrom(type))
                {
                    Dbg.Err($"Queried the dec hierarchy status of a type {type} that doesn't even inherit from Dec.");

                    result = DecDatabaseStatus.Invalid;
                }
                else if (type.GetCustomAttribute<AbstractAttribute>(false) != null)
                {
                    if (!type.IsAbstract)
                    {
                        Dbg.Err($"Type {type} is tagged Dec.Abstract, but is not abstract.");
                    }

                    if (type.BaseType != typeof(object) && GetDecDatabaseStatus(type.BaseType) > DecDatabaseStatus.Abstract)
                    {
                        Dbg.Err($"Type {type} is tagged Dec.Abstract, but inherits from {type.BaseType} which is within the database.");
                    }

                    result = DecDatabaseStatus.Abstract;
                }
                else if (type.BaseType.GetCustomAttribute<AbstractAttribute>(false) != null)
                {
                    // We do this just to validate everything beneath this. It'd better return Abstract! More importantly, it goes through all parents and makes sure they're consistent.
                    GetDecDatabaseStatus(type.BaseType);

                    result = DecDatabaseStatus.Root;
                }
                else
                {
                    // Further validation. This time we really hope it returns Abstract or Root. More importantly, it goes through all parents and makes sure they're consistent.
                    GetDecDatabaseStatus(type.BaseType);

                    // Our parent isn't NotDatabaseRootAttribute. We are not a database root, but we also can't say anything meaningful about our parents.
                    result = DecDatabaseStatus.Branch;
                }

                GetDecDatabaseStatusCache.Add(type, result);
            }

            return result;
        }

        private static Dictionary<Type, Type> GetDecRootTypeCache = new Dictionary<Type, Type>();
        internal static Type GetDecRootType(this Type type)
        {
            if (!GetDecRootTypeCache.TryGetValue(type, out var result))
            {
                if (GetDecDatabaseStatus(type) <= DecDatabaseStatus.Abstract)
                {
                    Dbg.Err($"{type} does not exist within a database hierarchy.");
                    result = null;
                }
                else
                {
                    Type currentType = type;
                    while (GetDecDatabaseStatus(currentType) == DecDatabaseStatus.Branch)
                    {
                        currentType = currentType.BaseType;
                    }

                    result = currentType;
                }

                GetDecRootTypeCache.Add(type, result);
            }

            return result;
        }

        internal static bool CanBeShared(this Type type)
        {
            return Util.CanBeShared(type);
        }

        internal enum ParseModeCategory
        {
            Dec,
            Object,
            OrderedContainer,
            UnorderedContainer,
            Value,
        }
        internal static ParseModeCategory CalculateSerializationModeCategory(this Type type, Converter converter, bool isRootDec)
        {
            if (isRootDec && typeof(Dec).IsAssignableFrom(type))
            {
                return ParseModeCategory.Dec;
            }
            else if (false
                || type.IsPrimitive
                || type == typeof(string)
                || type == typeof(Type)
                || (!isRootDec && typeof(Dec).IsAssignableFrom(type)) // unnecessary isRootDec test here is to shortcut the expensive IsAssignableFrom call
                || typeof(Enum).IsAssignableFrom(type)
                || converter is ConverterString
                || (System.ComponentModel.TypeDescriptor.GetConverter(type)?.CanConvertFrom(typeof(string)) ?? false)   // this is last because it's slow
            )
            {
                return ParseModeCategory.Value;
            }
            else if (
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) ||
                type.IsArray
            )
            {
                return ParseModeCategory.OrderedContainer;
            }
            else if (
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)) ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
            )
            {
                return ParseModeCategory.UnorderedContainer;
            }
            else if (type.IsGenericType && (
                    type.GetGenericTypeDefinition() == typeof(Tuple<>) ||
                    type.GetGenericTypeDefinition() == typeof(Tuple<,>) ||
                    type.GetGenericTypeDefinition() == typeof(Tuple<,,>) ||
                    type.GetGenericTypeDefinition() == typeof(Tuple<,,,>) ||
                    type.GetGenericTypeDefinition() == typeof(Tuple<,,,,>) ||
                    type.GetGenericTypeDefinition() == typeof(Tuple<,,,,,>) ||
                    type.GetGenericTypeDefinition() == typeof(Tuple<,,,,,,>) ||
                    type.GetGenericTypeDefinition() == typeof(Tuple<,,,,,,,>) ||
                    type.GetGenericTypeDefinition() == typeof(ValueTuple<>) ||
                    type.GetGenericTypeDefinition() == typeof(ValueTuple<,>) ||
                    type.GetGenericTypeDefinition() == typeof(ValueTuple<,,>) ||
                    type.GetGenericTypeDefinition() == typeof(ValueTuple<,,,>) ||
                    type.GetGenericTypeDefinition() == typeof(ValueTuple<,,,,>) ||
                    type.GetGenericTypeDefinition() == typeof(ValueTuple<,,,,,>) ||
                    type.GetGenericTypeDefinition() == typeof(ValueTuple<,,,,,,>) ||
                    type.GetGenericTypeDefinition() == typeof(ValueTuple<,,,,,,,>)
                ))
            {
                return ParseModeCategory.Value;
            }
            else
            {
                return ParseModeCategory.Object;
            }
        }
    }
}
