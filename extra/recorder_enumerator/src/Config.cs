namespace Dec.RecorderEnumerator
{
    using System;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;

    public static class Config
    {
        private static readonly Regex UserCreatedEnumerableRegex = new Regex(@"^<([^>]+)>d__([0-9]+)(?:`([0-9]+))?$", RegexOptions.Compiled);

        public static Converter ConverterFactory(Type type)
        {
            if (type == SystemLinqEnumerable_RangeIterator_Converter.RelevantType)
            {
                return new SystemLinqEnumerable_RangeIterator_Converter();
            }

            if (type.Name[0] == '<' && UserCreatedEnumerableRegex.Match(type.Name) is var result && result.Success && type.GetCustomAttribute<CompilerGeneratedAttribute>() != null)
            {
                // Now we're going to see if the attribute exists
                string functionName = result.Groups[1].Value;
                int functionIndex = int.Parse(result.Groups[2].Value);

                var owningType = type.DeclaringType;
                var owningTypeFunctions = owningType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                var function = owningTypeFunctions[functionIndex];
                if (function.Name != functionName)
                {
                    Dbg.Err($"Function name mismatch: {functionName} vs {function.Name}");
                    return null;
                }

                if (function.GetCustomAttribute<RecordableAttribute>() == null)
                {
                    Dbg.Err($"Attempting to serialize an enumerable {type} without a Dec.RecorderEnumerator.Recordable applied to its function");
                    return null;
                }
                
                return new UserCreatedEnumerableConverter(type);
            }

            return null;
        }
    }
}
