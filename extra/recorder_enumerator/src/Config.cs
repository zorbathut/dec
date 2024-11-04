using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Dec.RecorderEnumerator
{
    public static class Config
    {
        private static readonly Regex UserCreatedEnumerableRegex = new Regex(@"^<([^>]+)>d__[0-9]+(?:`([0-9]+))?$", RegexOptions.Compiled);
        private static readonly Regex RecordableClosureRegex = new Regex(@"^<>c__DisplayClass([0-9]+)_([0-9]+)(`[0-9]+)?$", RegexOptions.Compiled);

        private static HashSet<(string, string)> InternalRegexSupportOverride = new HashSet<(string, string)>
        {
            ("System.Linq.Enumerable", "CastIterator"),
            ("System.Linq.Enumerable", "DistinctByIterator"),
            ("System.Linq.Enumerable", "ExceptIterator"),
            ("System.Linq.Enumerable", "ExceptByIterator"),
            ("System.Linq.Enumerable", "IntersectIterator"),
            ("System.Linq.Enumerable", "IntersectByIterator"),
            ("System.Linq.Enumerable", "OfTypeIterator"),
            ("System.Linq.Enumerable", "SkipWhileIterator"),
            ("System.Linq.Enumerable", "TakeWhileIterator"),
            ("System.Linq.Lookup", "GetEnumerator"),
            ("System.Linq.OrderedEnumerable`1", "GetEnumerator"),
        };

        public static Converter ConverterFactory(Type type)
        {
            if (type == SystemLinqEnumerable_RangeIterator_Converter.RelevantType)
            {
                return new SystemLinqEnumerable_RangeIterator_Converter();
            }

            if (type == System_ArrayEnumerator_Converter.RelevantType)
            {
                return new System_ArrayEnumerator_Converter();
            }

            if (typeof(MethodInfo).IsAssignableFrom(type))
            {
                return new MethodInfo_Converter();
            }

            if (type.IsGenericType)
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();

                // Where

                if (genericTypeDefinition == SystemLinqEnumerable_WhereEnumerable_Converter.RelevantType)
                {
                    return (Converter)Activator.CreateInstance(typeof(SystemLinqEnumerable_WhereEnumerable_Converter<,>).MakeGenericType(type, type.GenericTypeArguments[0]));
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

                // Select

                if (genericTypeDefinition == SystemLinqEnumerable_SelectEnumerable_Converter.RelevantType)
                {
                    return (Converter)Activator.CreateInstance(typeof(SystemLinqEnumerable_SelectEnumerable_Converter<,>).MakeGenericType(type, type.GenericTypeArguments[1]));
                }

                if (genericTypeDefinition == SystemLinqEnumerable_SelectArray_Converter.RelevantType)
                {
                    return (Converter)Activator.CreateInstance(typeof(SystemLinqEnumerable_SelectArray_Converter<,>).MakeGenericType(type, type.GenericTypeArguments[1]));
                }

                if (genericTypeDefinition == SystemLinqEnumerable_SelectList_Converter.RelevantType)
                {
                    return (Converter)Activator.CreateInstance(typeof(SystemLinqEnumerable_SelectList_Converter<,>).MakeGenericType(type, type.GenericTypeArguments[1]));
                }

                if (genericTypeDefinition == SystemLinqEnumerable_SelectRange_Converter.RelevantType)
                {
                    return (Converter)Activator.CreateInstance(typeof(SystemLinqEnumerable_SelectRange_Converter<,>).MakeGenericType(type, type.GenericTypeArguments[0]));
                }

                if (genericTypeDefinition == SystemLinqEnumerable_SelectMany_Converter.RelevantType)
                {
                    return (Converter)Activator.CreateInstance(typeof(SystemLinqEnumerable_SelectMany_Converter<,>).MakeGenericType(type, type.GenericTypeArguments[1]));
                }

                // Set-related

                if (genericTypeDefinition == SystemLinqEnumerable_DistinctIterator_Converter.RelevantType)
                {
                    return (Converter)Activator.CreateInstance(typeof(SystemLinqEnumerable_DistinctIterator_Converter<,>).MakeGenericType(type, type.GenericTypeArguments[0]));
                }

                if (genericTypeDefinition == SystemLinqEnumerable_UnionIterator2_Converter.RelevantType)
                {
                    return (Converter)Activator.CreateInstance(typeof(SystemLinqEnumerable_UnionIterator2_Converter<,>).MakeGenericType(type, type.GenericTypeArguments[0]));
                }

                if (genericTypeDefinition == SystemLinqEnumerable_UnionIteratorN_Converter.RelevantType)
                {
                    return (Converter)Activator.CreateInstance(typeof(SystemLinqEnumerable_UnionIteratorN_Converter<,>).MakeGenericType(type, type.GenericTypeArguments[0]));
                }

                // Misc

                if (genericTypeDefinition == SystemLinqEnumerable_ReverseIterator_Converter.RelevantType)
                {
                    return (Converter)Activator.CreateInstance(typeof(SystemLinqEnumerable_ReverseIterator_Converter<,>).MakeGenericType(type, type.GenericTypeArguments[0]));
                }

                if (genericTypeDefinition == System_SZGenericArrayEnumerator_Converter.RelevantType)
                {
                    return (Converter)Activator.CreateInstance(typeof(System_SZGenericArrayEnumerator_Converter<>).MakeGenericType(type));
                }

                if (genericTypeDefinition == SystemLinqEnumerable_Concat2Iterator_Converter.RelevantType)
                {
                    return (Converter)Activator.CreateInstance(typeof(SystemLinqEnumerable_Concat2Iterator_Converter<,>).MakeGenericType(type, type.GenericTypeArguments[0]));
                }

                if (genericTypeDefinition == SystemLinqEnumerable_ConcatNIterator_Converter.RelevantType)
                {
                    return (Converter)Activator.CreateInstance(typeof(SystemLinqEnumerable_ConcatNIterator_Converter<,>).MakeGenericType(type, type.GenericTypeArguments[0]));
                }

                // SystemCollections

                if (genericTypeDefinition == SystemCollections_List_Enumerator_Converter.RelevantType)
                {
                    return (Converter)Activator.CreateInstance(typeof(SystemCollections_List_Enumerator_Converter<,>).MakeGenericType(type, type.GenericTypeArguments[0]));
                }

                if (genericTypeDefinition == SystemCollections_Generic_GenericComparer.RelevantType)
                {
                    return (Converter)Activator.CreateInstance(typeof(SystemCollections_Generic_GenericComparer<>).MakeGenericType(type.GenericTypeArguments[0]));
                }

                // SystemLinq

                if (genericTypeDefinition == SystemLinq_SingleLinkedNode_Converter.RelevantType)
                {
                    return (Converter)Activator.CreateInstance(typeof(SystemLinq_SingleLinkedNode_Converter<,>).MakeGenericType(type, type.GenericTypeArguments[0]));
                }

                if (genericTypeDefinition == SystemLinq_Buffer_Converter.RelevantType)
                {
                    return (Converter)Activator.CreateInstance(typeof(SystemLinq_Buffer_Converter<,>).MakeGenericType(type, type.GenericTypeArguments[0]));
                }

                if (genericTypeDefinition == SystemLinq_OrderedEnumerable_Converter.RelevantType)
                {
                    return (Converter)Activator.CreateInstance(typeof(SystemLinq_OrderedEnumerable_Converter<,,>).MakeGenericType(type, type.GenericTypeArguments[0], type.GenericTypeArguments[1]));
                }

                // Delegate

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

                    var owningType = type.DeclaringType;
                    var owningTypeFunctions = owningType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                    // This code works most of the time!
                    // It doesn't work all the time. Sucks to be me, right?
                    //int functionIndex = int.Parse(ucer.Groups[2].Value);
                    //var function = owningTypeFunctions[functionIndex];

                    // We're going to rely on people tagging *all* functions of a name with the attribute, and spit out errors if that doesn't happen.
                    // This solution sucks. :shrug:

                    var functions = owningTypeFunctions.Where(f => f.Name == functionName).ToArray();

                    int tags = functions.Count(f => f.GetCustomAttribute<RecordableEnumerableAttribute>() != null);

                    string parent = type.DeclaringType?.FullName ?? type.Namespace;
                    if (tags == functions.Length)
                    {

                    }
                    else if (InternalRegexSupportOverride.Contains((parent, functionName)))
                    {

                    }
                    else if (tags == 0)
                    {
                        Dbg.Err($"Attempting to serialize an enumerable {type} without a Dec.RecorderEnumerator.RecordableEnumerable applied to its function");
                        return null;
                    }
                    else // tags != functionLength
                    {
                        Dbg.Err($"Attempting to serialize an enumerable {type} without a Dec.RecorderEnumerator.RecordableEnumerable applied to all functions with that name; sorry, it's gotta be all of them right now");
                        return null;
                    }

                    return new UserCreatedEnumerableConverter(type);
                }

                if (RecordableClosureRegex.Match(type.Name) is var vcr && vcr.Success)
                {
                    // I really want to be able to compare this to the actual function the closure is defined in
                    // but I can't find any way to go from the function index to, like, an actual function :/
                    // I could in theory reverse engineer the IL to figure out what's calling it, but
                    // (1) no
                    // (2) that's probably going to be really broken on stuff like il2cpp
                    // (3) no

                    var owningType = type.DeclaringType;

                    if (owningType.GetCustomAttribute<RecordableClosuresAttribute>() == null)
                    {
                        Dbg.Err($"Attempting to serialize a closure {type} without a Dec.RecorderEnumerator.RecordableClosures applied to its class {owningType}");
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
