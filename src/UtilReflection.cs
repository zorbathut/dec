using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dec
{
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
                        Dbg.Err($"Found multiple examples of field named `{name}` in type hierarchy {type}; found in {resultType} and {curType}");
                    }
                }

                curType = curType.BaseType;
            }
            return result;
        }

        internal static System.Collections.Concurrent.ConcurrentDictionary<Type, FieldInfo[]> SerializableFieldsCached = new System.Collections.Concurrent.ConcurrentDictionary<Type, FieldInfo[]>();

        internal static FieldInfo[] GetSerializableFieldsFromHierarchy(this Type type)
        {
            if (SerializableFieldsCached.TryGetValue(type, out var cached))
            {
                return cached;
            }

            var result = new List<FieldInfo>();
            var seenFields = new HashSet<string>();

            Type curType = type;
            while (curType != null)
            {
                foreach (var field in curType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (field.IsBackingField())
                    {
                        continue;
                    }

                    if (field.GetCustomAttribute<IndexAttribute>() != null)
                    {
                        // we don't save indices
                        continue;
                    }

                    if (field.GetCustomAttribute<NonSerializedAttribute>() != null)
                    {
                        // we also don't save nonserialized
                        continue;
                    }

                    if (seenFields.Contains(field.Name))
                    {
                        Dbg.Err($"Found duplicates of field `{field}`; base fields will be ignored");
                        continue;
                    }

                    result.Add(field);
                    seenFields.Add(field.Name);
                }

                curType = curType.BaseType;
            }

            var array = result.ToArray();
            SerializableFieldsCached.TryAdd(type, array);
            return array;
        }

        internal static bool IsUserAssembly(this Assembly asm)
        {
            var name = asm.FullName;

            // Filter out system libraries
            if (name.StartsWith("mscorlib,") || name.StartsWith("System,") || name.StartsWith("System.") || name.StartsWith("netstandard"))
            {
                return false;
            }

            // Filter out Mono
            if (name.StartsWith("Mono."))
            {
                return false;
            }

            // Filter out nunit, almost entirely so our test results look better
            if (name.StartsWith("nunit.framework,"))
            {
                return false;
            }

            // Filter out Microsoft test platform to avoid weird .NET 9 compatibility issues
            if (name.StartsWith("Microsoft.TestPlatform") || name.StartsWith("Microsoft.VisualStudio.TestPlatform"))
            {
                return false;
            }

            // Filter out Unity
            if (name.StartsWith("Unity.") || name.StartsWith("UnityEngine,") || name.StartsWith("UnityEngine.") || name.StartsWith("UnityEditor,") || name.StartsWith("UnityEditor.") || name.StartsWith("ExCSS.Unity,"))
            {
                return false;
            }

            // Filter out dec
            if (name.StartsWith("dec,"))
            {
                return false;
            }

            return true;
        }

        internal static IEnumerable<Assembly> GetAllUserAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Where(asm => asm.IsUserAssembly());
        }

        internal static IEnumerable<Type> GetAllUserTypes()
        {
            return GetAllUserAssemblies().SelectMany(a => a.GetTypes()).Where(t => {
                var ns = t.Namespace;
                if (ns == null)
                {
                    return true;
                }

                return !t.Namespace.StartsWith("Dec.") && t.Namespace != "Dec";
            });
        }

        internal struct IndexInfo
        {
            public Type type;
            public FieldInfo field;
        }
        internal static System.Collections.Concurrent.ConcurrentDictionary<Type, IndexInfo[]> IndexInfoCached = new System.Collections.Concurrent.ConcurrentDictionary<Type, IndexInfo[]>();
        internal static IndexInfo[] GetIndicesForType(Type type)
        {
            if (IndexInfoCached.TryGetValue(type, out var result))
            {
                // found it in cache, we're done
                return result;
            }

            IndexInfo[] indices = null;

            if (type.BaseType != null)
            {
                indices = GetIndicesForType(type.BaseType);
            }

            FieldInfo matchedField = null;
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (field.GetCustomAttribute<IndexAttribute>() != null)
                {
                    if (matchedField != null)
                    {
                        Dbg.Err($"Too many indices in type {type} (found `{matchedField}` and `{field}`); only one will be filled");
                    }

                    matchedField = field;
                }
            }

            if (matchedField != null)
            {
                IndexInfo[] indicesWorking;

                if (indices != null)
                {
                    indicesWorking = new IndexInfo[indices.Length + 1];
                    Array.Copy(indices, indicesWorking, indices.Length);
                }
                else
                {
                    indicesWorking = new IndexInfo[1];
                }

                indicesWorking[indicesWorking.Length - 1] = new IndexInfo { type = type, field = matchedField };

                indices = indicesWorking;
            }

            IndexInfoCached[type] = indices;

            return indices;
        }

        internal static bool IsBackingField(this FieldInfo field)
        {
            // I wish I could find something more authoritative on this.
            return field.Name.StartsWith("<");
        }

        private enum CreateInstanceAction : byte
        {
            Construct,
            ConstructValueType,
            Abstract,
            Array,
            NoConstructor,
        }

        private static ConcurrentDictionary<Type, (CreateInstanceAction action, ConstructorInfo ctor, Type constructType)> CreateInstanceCache = new ConcurrentDictionary<Type, (CreateInstanceAction, ConstructorInfo, Type)>();

        internal static object CreateInstanceSafe(this Type type, string errorType, ReaderNode node)
        {
            if (!CreateInstanceCache.TryGetValue(type, out var cached))
            {
                // Unwrap Nullable<T> to T; T is always a non-nullable value type
                var resolvedType = type;
                if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    resolvedType = type.GenericTypeArguments[0];
                }

                if (resolvedType.IsAbstract)
                {
                    cached = (CreateInstanceAction.Abstract, null, null);
                }
                else if (resolvedType.IsArray)
                {
                    // Special handling, we need a fancy constructor with an int array parameter
                    // Conveniently, arrays are really easy to deal with in this pathway :D
                    cached = (CreateInstanceAction.Array, null, null);
                }
                else if (resolvedType.IsValueType)
                {
                    // Note: Structs don't have constructors. I actually can't tell if ints do, I'm kind of bypassing that system.
                    cached = (CreateInstanceAction.ConstructValueType, null, resolvedType);
                }
                else
                {
                    var ctor = resolvedType.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { }, null);
                    if (ctor == null)
                    {
                        cached = (CreateInstanceAction.NoConstructor, null, null);
                    }
                    else
                    {
                        cached = (CreateInstanceAction.Construct, ctor, null);
                    }
                }

                CreateInstanceCache[type] = cached;
            }

            string BuiltContext()
            {
                return node?.GetContext().ToString() ?? "setup";
            }

            switch (cached.action)
            {
                case CreateInstanceAction.Abstract:
                    Dbg.Err($"{BuiltContext()}: Attempting to create {errorType} of abstract type {type}");
                    // thankfully all abstract types can accept being null
                    return null;

                case CreateInstanceAction.Array:
                    return UtilType.CreateDynamicArray(type.GetElementType(), node.GetArrayDimensions(type.GetArrayRank()));

                case CreateInstanceAction.NoConstructor:
                    Dbg.Err($"{BuiltContext()}: Attempting to create {errorType} of type {type} without a no-argument constructor");
                    // anything that is capable of not having a no-argument constructor can accept being null
                    return null;

                case CreateInstanceAction.Construct:
                    try
                    {
                        var result = cached.ctor.Invoke(null);
                        if (result == null)
                        {
                            // this is supposedly impossible, but I'm putting this here just because I'm paranoid as hell
                            Dbg.Err($"{BuiltContext()}: {errorType} of type {type} was not properly created; this will cause issues");
                        }
                        return result;
                    }
                    catch (TargetInvocationException e)
                    {
                        Dbg.Ex(e);
                        return null;
                    }

                case CreateInstanceAction.ConstructValueType:
                    try
                    {
                        var result = Activator.CreateInstance(cached.constructType, true);
                        if (result == null)
                        {
                            // This is difficult to test; there are very few things that can get CreateInstance to return null, and we supposedly handle all of them in the above tests.
                            // In theory a malformed COM object might do it.
                            Dbg.Err($"{BuiltContext()}: {errorType} of type {type} was not properly created; this will cause issues");
                        }
                        return result;
                    }
                    catch (TargetInvocationException e)
                    {
                        Dbg.Ex(e);
                        return null;
                    }

                default:
                    Dbg.Err($"{BuiltContext()}: Internal error in CreateInstanceSafe for type {type}");
                    return null;
            }
        }
    }
}
