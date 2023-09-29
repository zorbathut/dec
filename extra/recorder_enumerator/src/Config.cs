namespace Dec.RecorderEnumerator
{
    using System;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using System.Text.RegularExpressions;

    public static class Config
    {
        private static readonly Regex UserCreatedEnumerableRegex = new Regex(@"^<([^>]+)>d__([0-9]+)(?:`([0-9]+))?$", RegexOptions.Compiled);
        private static readonly Regex RecordableClosureRegex = new Regex(@"^<>c__DisplayClass([0-9]+)_([0-9]+)$", RegexOptions.Compiled);


        public static Converter ConverterFactory(Type type)
        {
            if (type == SystemLinqEnumerable_RangeIterator_Converter.RelevantType)
            {
                return new SystemLinqEnumerable_RangeIterator_Converter();
            }

            if (typeof(MethodInfo).IsAssignableFrom(type))
            {
                return new MethodInfo_Converter();
            }

            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();

                if (genericTypeDefinition == SystemLinqEnumerable_WhereIterator_Converter.RelevantType)
                {
                    return (Converter)Activator.CreateInstance(typeof(SystemLinqEnumerable_WhereIterator_Converter<,>).MakeGenericType(type, type.GenericTypeArguments[0]));
                }

                if (genericTypeDefinition == SystemLinqEnumerable_WhereArray_Converter.RelevantType)
                {
                    return (Converter)Activator.CreateInstance(typeof(SystemLinqEnumerable_WhereArray_Converter<,>).MakeGenericType(type, type.GenericTypeArguments[0]));
                }

                if (System_Func_Converter.IsGenericTypeFunc(genericTypeDefinition))
                {
                    return new System_Func_Converter(type);
                }
            }

            if (type.Name[0] == '<')
            {
                // compiler-generated stuff
                if (type.GetCustomAttribute<CompilerGeneratedAttribute>() == null)
                {
                    Dbg.Err($"Internal error; presumed compiler-generated type {type} does not have CompilerGeneratedAttribute");
                    return null;
                }
            
                if (UserCreatedEnumerableRegex.Match(type.Name) is var ucer && ucer.Success)
                {
                    // Now we're going to see if the attribute exists
                    string functionName = ucer.Groups[1].Value;
                    int functionIndex = int.Parse(ucer.Groups[2].Value);

                    var owningType = type.DeclaringType;
                    var owningTypeFunctions = owningType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                    var function = owningTypeFunctions[functionIndex];
                    if (function.Name != functionName)
                    {
                        Dbg.Err($"Function name mismatch: {functionName} vs {function.Name}");
                        return null;
                    }

                    if (function.GetCustomAttribute<RecordableEnumerableAttribute>() == null)
                    {
                        Dbg.Err($"Attempting to serialize an enumerable {type} without a Dec.RecorderEnumerator.Recordable applied to its function");
                        return null;
                    }
                
                    return new UserCreatedEnumerableConverter(type);
                }

                if (RecordableClosureRegex.Match(type.Name) is var vcr && vcr.Success)
                {
                    int functionIndex = int.Parse(vcr.Groups[1].Value);

                    var owningType = type.DeclaringType;
                    var owningTypeFunctions = owningType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                    var function = owningTypeFunctions[functionIndex];
                    // No function name test, unfortunately

                    if (function.GetCustomAttribute<RecordableClosuresAttribute>() == null)
                    {
                        Dbg.Err($"Attempting to serialize an enumerable {type} without a Dec.RecorderEnumerator.Recordable applied to its function");
                        return null;
                    }

                    return new RecordableClosureConverter(type);
                }
            }

            return null;
        }
    }
}
