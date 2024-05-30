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
        // Generics currently use <>'s as you would expect. This isn't compatible with XML tags but I'm just kind of living with it for now.
        // And yes, this also isn't compatible with C#'s internal types.

        // When serializing types, we chop off as much of the prefix as we can. When deserializing types, we error if there's ambiguity based on existing prefixes.

        private struct PrimitiveTypeLookup
        {
            public Type type;
            public string str;
        }
        private static readonly PrimitiveTypeLookup[] PrimitiveTypes = new PrimitiveTypeLookup[]
        {
            new PrimitiveTypeLookup { type = typeof(bool), str = "bool" },
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

        private static Regex GenericParameterMatcher = new Regex("`[0-9]+", RegexOptions.Compiled);
        private static Dictionary<(string, int), Type[]> StrippedTypeCache = null;  // this is fine not being concurrent; it's set once and then never modified
        private static Type GetTypeFromAnyAssembly(string text, int gparams, InputContext context)
        {
            // This is technically unnecessary if we're not parsing a generic, but we may as well do it because the cache will still be faster for nongenerics.
            // If we really wanted a perf boost here, we'd do one pass for non-generic objects, then do it again on a cache miss to fill it with generic stuff.
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
                    .Where(t => t.DeclaringType == null)    // we have to split these up anyway, so including declaring types just makes our life a little harder
                    .Distinct()
                    .GroupBy(t => {
                        if (t.IsGenericType)
                        {
                            return (t.FullName.Substring(0, t.FullName.IndexOfUnbounded('`')), t.GetGenericArguments().Length);
                        }
                        else
                        {
                            return (t.FullName, 0);
                        }
                    })
                    .ToDictionary(
                        group => group.Key,
                        group => group.ToArray());
            }

            var result = StrippedTypeCache.TryGetValue((text, gparams));

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

        private static Type ParseSubtype(Type root, string text, ref List<Type> genericTypes, InputContext context)
        {
            if (root == null)
            {
                return null;
            }

            if (text.Length == 0)
            {
                return root;
            }

            int previousTypes = genericTypes?.Count ?? 0;
            if (!ParsePiece(text, context, out int endIndex, out string token, ref genericTypes))
            {
                return null;
            }
            int addedTypes = (genericTypes?.Count ?? 0) - previousTypes;

            Type chosenType;
            if (addedTypes == 0)
            {
                chosenType = root.GetNestedType(token, BindingFlags.Public | BindingFlags.NonPublic);
            }
            else
            {
                chosenType = root.GetNestedType($"{token}`{addedTypes}", BindingFlags.Public | BindingFlags.NonPublic);
            }

            // Chain on to another call in case we have a further-nested class-of-class
            return ParseSubtype(chosenType, text.SubstringSafe(endIndex), ref genericTypes, context);
        }

        internal static bool ParsePiece(string input, InputContext context, out int endIndex, out string name, ref List<Type> types)
        {
            // Isolate the first token; this is the distance from our current index to the first . or < that *isn't* the beginning of a class name.
            int nameEnd = Math.Min(input.IndexOfUnbounded('.'), input.IndexOfUnbounded('<', 1));
            name = input.Substring(0, nameEnd);

            // If we have a < we need to extract generic arguments.
            if (nameEnd < input.Length && input[nameEnd] == '<')
            {
                if (!ParseGenericParams(input.Substring(nameEnd + 1), context, out int endOfGenericsAdjustment, ref types))
                {
                    // just kinda give up to ensure we don't get trapped in a loop
                    Dbg.Err($"{context}: Failed to parse generic arguments for type containing {input}");
                    endIndex = input.Length;
                    return false;
                }
                endIndex = nameEnd + endOfGenericsAdjustment + 3; // adjustment for <>.
                // . . . but also, make sure we don't have a trailing dot!
                if (endIndex == input.Length && input[endIndex - 1] == '.')
                {
                    Dbg.Err($"{context}: Type containing `{input}` has trailing .");
                    return false;
                }
            }
            else
            {
                endIndex = nameEnd + 1; // adjustment for .
            }

            return true;
        }

        // returns false on error
        internal static bool ParseGenericParams(string tstring, InputContext context, out int endIndex, ref List<Type> types)
        {
            int depth = 0;
            endIndex = 0;

            int typeStart = 0;

            for (endIndex = 0; endIndex < tstring.Length; ++endIndex)
            {
                char c = tstring[endIndex];
                switch (c)
                {
                    case '<':
                        depth++;
                        break;

                    case '>':
                        depth--;
                        break;

                    case ',':
                        if (depth == 0)
                        {
                            if (types == null)
                            {
                                types = new List<Type>();
                            }
                            types.Add(UtilType.ParseDecFormatted(tstring.Substring(typeStart, endIndex - typeStart).Trim(), context));
                            typeStart = endIndex + 1;
                        }
                        break;
                }

                if (depth < 0)
                {
                    break;
                }
            }

            if (depth != -1)
            {
                Dbg.Err($"{context}: Mismatched angle brackets when parsing generic type component `{tstring}`");
                return false;
            }

            if (endIndex + 1 < tstring.Length && tstring[endIndex + 1] != '.')
            {
                Dbg.Err($"{context}: Unexpected character after end of generic type definition");
                return false;
            }

            if (endIndex != typeStart)
            {
                if (types == null)
                {
                    types = new List<Type>();
                }
                types.Add(UtilType.ParseDecFormatted(tstring.Substring(typeStart, endIndex - typeStart).Trim(), context));
            }

            return true;
        }

        private static Type ParseIndependentType(string text, InputContext context)
        {
            // This function just tries to find a class with a specific namespace; we no longer worry about `using`.
            // Our challenge is to find a function with the right ordering of generic arguments. NS.Foo`1.Bar is different from NS.Foo.Bar`1, for example.
            // We're actually going to transform our input into C#'s generic-argument layout so we can find the right instance easily.

            // This is complicated by the fact that the compiler can generate class names with <> in them - that is, the *name*, not the generic specialization.
            // As an added bonus, the namespace is signaled differently, so we're going to be chopping this up awkwardly as we do it.

            int nextTokenEnd = 0;
            string currentPrefix = "";
            while (nextTokenEnd < text.Length)
            {
                // Parse another chunk
                // This involves an unnecessary amount of copying - this should be fixed, but this is currently not the bottleneck anywhere I've seen.
                List<Type> genericParameters = null; // avoid churn if we can
                if (!ParsePiece(text.Substring(nextTokenEnd), context, out int currentTokenLength, out string tokenText, ref genericParameters))
                {
                    // parse error, abort
                    return null;
                }

                // update next token position
                nextTokenEnd += currentTokenLength;

                // update our currentPrefix
                if (currentPrefix.Length == 0)
                {
                    currentPrefix = tokenText;
                }
                else
                {
                    currentPrefix += "." + tokenText;
                }

                // This is the thing we're going to test to see if is a class.
                var parsedType = GetTypeFromAnyAssembly(currentPrefix, genericParameters?.Count ?? 0, context);

                if (parsedType != null)
                {
                    // We found the root! Keep on digging.
                    Type primitiveType = ParseSubtype(parsedType, text.SubstringSafe(nextTokenEnd), ref genericParameters, context);
                    if (primitiveType != null && genericParameters != null)
                    {
                        primitiveType = primitiveType.MakeGenericType(genericParameters.ToArray());
                    }
                    return primitiveType;
                }

                // We did not! Continue on.
                if (genericParameters != null)
                {
                    // . . . but we can't go past a set of generics, namespace generics aren't a thing. So we're done.
                    return null;
                }
            }

            // Ran out of string, no match.
            return null;
        }

        private static Regex ArrayRankParser = new Regex(@"\[([,]*)\]$", RegexOptions.Compiled);
        private static Dictionary<string, Type> ParseCache = new Dictionary<string, Type>();
        internal static Type ParseDecFormatted(string text, InputContext context)
        {
            if (text == "")
            {
                Dbg.Err($"{context}: The empty string is not a valid type");
                return null;
            }

            if (text.Contains("{"))
            {
                // There's a bunch of times you might want to put full template typenames in XML and doing so is a pain due to XML treating angled brackets specially
                // so we allow for {} in its place
                // "why not ()" because that's tuple syntax
                // "why not []" because that's array syntax
                text = text.Replace("{", "<").Replace("}", ">");
            }

            if (ParseCache.TryGetValue(text, out Type cacheVal))
            {
                if (cacheVal == null)
                {
                    Dbg.Err($"{context}: Repeating previous failure to parse type named `{text}`");
                }
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

            int arrayRanks = 0;
            if (ArrayRankParser.Match(text) is Match match && match.Success)
            {
                arrayRanks = match.Groups[1].Length + 1;
                text = text.Substring(0, text.Length - match.Length);
            }

            // We need to find a class that matches the least number of tokens. Namespaces can't be generics so at most this continues until we hit a namespace.
            var possibleTypes = Config.UsingNamespaces
                .Select(ns => ParseIndependentType($"{ns}.{text}", context))
                .Concat(ParseIndependentType(text, context))
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

            if (arrayRanks == 1)
            {
                // I'm not totally sure why MakeArrayType(1) does the wrong thing here
                result = result.MakeArrayType();
            }
            else if (arrayRanks > 1)
            {
                result = result.MakeArrayType(arrayRanks);
            }

            ParseCache[text] = result;
            return result;
        }

        private static readonly Regex GenericParameterReplacementRegex = new Regex(@"`\d+", RegexOptions.Compiled);
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

                baseString = baseString.Remove(0, bestPrefix.Length);

                string result;
                if (type.IsConstructedGenericType)
                {
                    var genericTypes = type.GenericTypeArguments.Select(t => t.ComposeDecFormatted()).ToList();
                    int paramIndex = 0;
                    result = GenericParameterReplacementRegex.Replace(baseString, match =>
                        {
                            int numParams = int.Parse(match.Value.Substring(1));
                            var replacement = string.Join(", ", genericTypes.GetRange(paramIndex, numParams));
                            paramIndex += numParams;
                            return $"<{replacement}>";
                        });
                }
                else
                {
                    result = baseString;
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
        internal static bool CanBeCloneCopied(this Type type)
        {
            return type.IsPrimitive || typeof(Dec).IsAssignableFrom(type) || type == typeof(string) || type == typeof(Type) || type.GetCustomAttribute<CloneWithAssignmentAttribute>() != null;
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
                type.IsArray ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Stack<>)) ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Queue<>))
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
