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

        // Cached transitive-reference closure of Dec. Invalidated when AppDomain.GetAssemblies().Length changes.
        // Eventually-consistent: a collectible AssemblyLoadContext unload-and-reload with the same final count
        // would escape detection, but Dec does not support that scenario (see ARCHITECTURE.md threading contract).
        // Returned arrays are never mutated after publication, so callers iterating an older snapshot are safe.
        private static Assembly[] cachedUserAssemblies;
        private static int cachedUserAssembliesAssemblyCount = -1;
        private static readonly object userAssembliesLock = new object();

        internal static IEnumerable<Assembly> GetAllUserAssemblies()
        {
            // An assembly contains types this method's callers care about (Converter subclasses,
            // [StaticReferences]-attributed classes) only if it directly or transitively references Dec's
            // own assembly - those types can't be declared without the C# compiler emitting a manifest-level
            // reference to Dec. That makes this narrower than UtilType.GetTypeFromAnyAssembly's scan, which
            // has to find plain data classes in assemblies that don't themselves reference Dec.
            //
            // Matching is by AssemblyName.Name only (no version / public-key-token / culture). In practice
            // Dec ships as a single-version DLL per process; side-by-side loads of two different dec.dlls
            // in separate AssemblyLoadContexts are not a supported configuration.
            var loaded = AppDomain.CurrentDomain.GetAssemblies();
            lock (userAssembliesLock)
            {
                if (cachedUserAssemblies == null || loaded.Length != cachedUserAssembliesAssemblyCount)
                {
                    cachedUserAssemblies = ComputeDecReferrerClosure(loaded);
                    cachedUserAssembliesAssemblyCount = loaded.Length;
                }
                return cachedUserAssemblies;
            }
        }

        private static Assembly[] ComputeDecReferrerClosure(Assembly[] loaded)
        {
            var decAssembly = typeof(Dec).Assembly;

            // Build a reverse-reference graph: for each assembly name, who references it?
            // Keyed by simple name (AssemblyName.Name) since that's how references identify their target.
            var referrers = new Dictionary<string, List<Assembly>>();
            foreach (var asm in loaded)
            {
                AssemblyName[] references;
                try
                {
                    references = asm.GetReferencedAssemblies();
                }
                catch (NotSupportedException)
                {
                    // Dynamic (AssemblyBuilder) and reflection-only assemblies throw this; skip them.
                    continue;
                }
                catch (Exception e)
                {
                    // Something unexpected - don't silently swallow it, but keep going so one broken
                    // assembly doesn't take out type discovery for the whole process.
                    Dbg.Err($"Failed to read references from {asm.FullName}: {e}");
                    continue;
                }

                foreach (var reference in references)
                {
                    if (!referrers.TryGetValue(reference.Name, out var list))
                    {
                        list = new List<Assembly>();
                        referrers[reference.Name] = list;
                    }
                    list.Add(asm);
                }
            }

            // BFS outward from Dec, following "who references me?" edges. Seed with decAssembly itself so
            // the embedded-source case works (where Dec is compiled directly into the user's assembly and
            // there is no separate dec.dll to reference).
            var closure = new HashSet<Assembly> { decAssembly };
            var queue = new Queue<Assembly>();
            queue.Enqueue(decAssembly);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (referrers.TryGetValue(current.GetName().Name, out var directReferrers))
                {
                    foreach (var referrer in directReferrers)
                    {
                        if (closure.Add(referrer))
                        {
                            queue.Enqueue(referrer);
                        }
                    }
                }
            }

            return closure.ToArray();
        }

        internal static IEnumerable<Type> GetAllUserTypes()
        {
            // Filter out Dec's own internal types. We key off (assembly, namespace) rather than namespace
            // alone, so user code that happens to live in a "Dec" namespace (in a user assembly) is still
            // surfaced. This matters for the embedded-source configuration where decAssembly is the user's
            // assembly: we still want to exclude Dec's own types from discovery, but we can only identify
            // them by namespace since the assembly check won't help.
            var decAssembly = typeof(Dec).Assembly;
            return GetAllUserAssemblies().SelectMany(a => a.GetTypes()).Where(t =>
            {
                if (t.Assembly != decAssembly)
                {
                    return true;
                }
                var ns = t.Namespace;
                return ns != null && ns != "Dec" && !ns.StartsWith("Dec.");
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
