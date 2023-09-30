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

                if (genericTypeDefinition == SystemLinqEnumerable_WhereList_Converter.RelevantType)
                {
                    return (Converter)Activator.CreateInstance(typeof(SystemLinqEnumerable_WhereList_Converter<,>).MakeGenericType(type, type.GenericTypeArguments[0]));
                }

                if (genericTypeDefinition == SystemLinqEnumerable_WhereSelectIterator_Converter.RelevantType)
                {
                    return (Converter)Activator.CreateInstance(typeof(SystemLinqEnumerable_WhereSelectIterator_Converter<,>).MakeGenericType(type, type.GenericTypeArguments[0]));
                }

                if (genericTypeDefinition == SystemLinqEnumerable_WhereSelectArray_Converter.RelevantType)
                {
                    return (Converter)Activator.CreateInstance(typeof(SystemLinqEnumerable_WhereSelectArray_Converter<,>).MakeGenericType(type, type.GenericTypeArguments[0]));
                }

                if (genericTypeDefinition == SystemLinqEnumerable_WhereSelectList_Converter.RelevantType)
                {
                    return (Converter)Activator.CreateInstance(typeof(SystemLinqEnumerable_WhereSelectList_Converter<,>).MakeGenericType(type, type.GenericTypeArguments[0]));
                }

                if (genericTypeDefinition == SystemCollections_List_Enumerator_Converter.RelevantType)
                {
                    return (Converter)Activator.CreateInstance(typeof(SystemCollections_List_Enumerator_Converter<,>).MakeGenericType(type, type.GenericTypeArguments[0]));
                }

                if (System_Delegate_Converter.IsGenericDelegate(genericTypeDefinition))
                {
                    return new System_Delegate_Converter(type);
                }
            }
            else
            {
                if (System_Delegate_Converter.IsNonGenericDelegate(type))
                {
                    return new System_Delegate_Converter(type);
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

                if (type.Name == "<>c")
                {
                    // it is currently unclear to me what the purpose of this class is
                    // I get that this is a place for shoving inline functions without closures
                    // but why aren't those functions static? why do they need to refer to an instance of the class?

                    // I'm a little worried that I'm going to break something horribly by creating more instances of this class, but nothing seems to stop me from doing so, so, uh
                    // okay

                    // I should probably be rigging this up to hackily refer to the internal static instance but for now I'm just not going to

                    // anyway, this isn't meant for this, but it'll work for now
                    return new RecordableClosureConverter(type);
                }
            }

            return null;
        }
    }
}
