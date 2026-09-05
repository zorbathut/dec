using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

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

        // GetTypes() can throw ReflectionTypeLoadException on some platforms when a dependency fails to load; the partial results in .Types (with nulls for the failed entries) are usually what we want.
        internal static IEnumerable<Type> GetTypesSafe(this Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                // Hard to code-coverage: happens on some platforms, not on our test server. To reproduce, you'd have to build a fake .dll that references a missing dependency.
                return e.Types.Where(t => t != null);
            }
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
                // GetFields order is unspecified and has been observed to change within a process; metadata tokens are assigned in declaration order, which keeps composed output stable.
                // Right now this exists mostly to stabilize Compose output for the sake of tests.
                // This is probably not the right solution, but it's easy and this is unlikely to be a perf issue.
                foreach (var field in curType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).OrderBy(f => f.MetadataToken))
                {
                    if (field.GetCustomAttribute<CompilerGeneratedAttribute>() != null)
                    {
                        // we don't save backing fields
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
        // Eventually-consistent: a collectible AssemblyLoadContext unload-and-reload with the same final count would escape detection, but Dec does not support that scenario (see ARCHITECTURE.md threading contract).
        // Returned arrays are never mutated after publication, so callers iterating an older snapshot are safe.
        private static Assembly[] cachedUserAssemblies;
        private static int cachedUserAssembliesAssemblyCount = -1;
        private static readonly object userAssembliesLock = new object();

        internal static IEnumerable<Assembly> GetAllUserAssemblies()
        {
            // An assembly contains types this method's callers care about (Converter subclasses, [StaticReferences]-attributed classes) only if it directly or transitively references Dec's own assembly - those types can't be declared without the C# compiler emitting a manifest-level reference to Dec. That makes this narrower than UtilType.GetTypeFromAnyAssembly's scan, which has to find plain data classes in assemblies that don't themselves reference Dec.
            //
            // Matching is by AssemblyName.Name only (no version / public-key-token / culture). In practice Dec ships as a single-version DLL per process; side-by-side loads of two different dec.dlls in separate AssemblyLoadContexts are not a supported configuration.
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

            // Build a reverse-reference graph: for each assembly name, who references it? Keyed by simple name (AssemblyName.Name) since that's how references identify their target.
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
                    // Something unexpected - don't silently swallow it, but keep going so one broken assembly doesn't take out type discovery for the whole process.
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

            // BFS outward from Dec, following "who references me?" edges. Seed with decAssembly itself so the embedded-source case works (where Dec is compiled directly into the user's assembly and there is no separate dec.dll to reference).
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

        // Dec's own Converter-hierarchy types, which must not surface through user-type discovery: abstract bases would produce "found abstract converter" errors in Serialization's scan, and the non-abstract Nullable helpers would all try to register as the generic prototype for Nullable<>. We exclude by type identity rather than namespace so user code living in the "Dec" namespace (realistic only in the embedded-source configuration) is still surfaced.
        //
        // Drift safety: the Reflection.UserTypesExcludeDecConverterHierarchy test reflectively discovers every Converter subclass in Dec's own assembly and asserts each is excluded, so forgetting to add a new entry here fails that test.
        private static readonly HashSet<Type> DecInternalNonUserTypes = new HashSet<Type>
        {
            typeof(Converter),
            typeof(ConverterString),
            typeof(ConverterString<>),
            typeof(ConverterStringDynamic),
            typeof(ConverterRecord),
            typeof(ConverterRecord<>),
            typeof(ConverterRecordDynamic),
            typeof(ConverterFactory),
            typeof(ConverterFactory<>),
            typeof(ConverterFactoryDynamic),
            typeof(Serialization.ConverterNullableString<>),
            typeof(Serialization.ConverterNullableRecord<>),
            typeof(Serialization.ConverterNullableFactory<>),
        };

        internal static IEnumerable<Type> GetAllUserTypes()
        {
            return GetAllUserAssemblies().SelectMany(a => a.GetTypesSafe()).Where(t => !DecInternalNonUserTypes.Contains(t));
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

        internal class SetupMethodInfo
        {
            // Always the base definition, so node identity is uniform across derived types; invoking it dispatches virtually to the most-derived body.
            public MethodInfo method;
            // The method the [Dec.Setup] attribute was actually found on; [Dec.SetupAfter]/[Dec.SetupBefore] are read from here, which matters when the attribute lives on an override of an untagged base method.
            public MethodInfo attributeSource;
            public bool parallel;
            public Type explicitStage;
        }
        internal static System.Collections.Concurrent.ConcurrentDictionary<Type, SetupMethodInfo[]> SetupInfoCached = new System.Collections.Concurrent.ConcurrentDictionary<Type, SetupMethodInfo[]>();
        internal static System.Collections.Concurrent.ConcurrentDictionary<Type, SetupMethodInfo[]> InterfaceContractDeclaredCached = new System.Collections.Concurrent.ConcurrentDictionary<Type, SetupMethodInfo[]>();

        // Setup functions declared as [Dec.Setup]-tagged members directly on `iface`, excluding inherited interfaces; validation errors report once per load thanks to the cache, which Database.Clear resets so they re-report like every other declaration diagnostic.
        internal static SetupMethodInfo[] GetDeclaredInterfaceContract(Type iface)
        {
            if (InterfaceContractDeclaredCached.TryGetValue(iface, out var cached))
            {
                return cached;
            }

            List<SetupMethodInfo> found = null;
            foreach (var method in iface.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                var attribute = method.GetCustomAttribute<SetupAttribute>(inherit: false);
                if (attribute == null)
                {
                    if (method.GetCustomAttributes<SetupAfterAttribute>(inherit: false).Any() || method.GetCustomAttributes<SetupBeforeAttribute>(inherit: false).Any())
                    {
                        Dbg.Err($"{iface}.{method.Name} has a [Dec.SetupAfter] or [Dec.SetupBefore] attribute but no [Dec.Setup]; the ordering constraint has no effect");
                    }

                    continue;
                }

                if (method.IsStatic)
                {
                    Dbg.Err($"{iface}.{method.Name} is a static interface member with a [Dec.Setup] attribute; static setup functions are not supported on interfaces");
                    continue;
                }

                if (!ValidateSetupSignature(method))
                {
                    continue;
                }

                if (found == null)
                {
                    found = new List<SetupMethodInfo>();
                }
                found.Add(new SetupMethodInfo { method = method, attributeSource = method, parallel = attribute.Parallel, explicitStage = attribute.Stage });
            }

            // GetMethods order is unspecified, and contract entry order must be deterministic because same-named nodes tie on the setup graph's sort key
            found?.Sort((a, b) => string.CompareOrdinal(a.method.Name, b.method.Name));

            var result = found?.ToArray();
            InterfaceContractDeclaredCached[iface] = result;
            return result;
        }

        private static IEnumerable<Type> SortedInterfaces(Type type)
        {
            // GetInterfaces order is unspecified; see GetDeclaredInterfaceContract for why determinism matters
            return type.GetInterfaces().OrderBy(i => i.FullName ?? i.Name, StringComparer.Ordinal);
        }

        // Returns the instance setup functions applicable to `type`, including inherited and interface-contract ones, or null if there are none. Static setup functions are handled by the parser's scan, not here.
        internal static SetupMethodInfo[] GetSetupInfoForType(Type type)
        {
            if (SetupInfoCached.TryGetValue(type, out var result))
            {
                return result;
            }

            SetupMethodInfo[] setups = null;

            if (type.IsInterface)
            {
                // An interface's setup is its contract: tagged members declared on it or on any interface it inherits. Interfaces have no BaseType, so this chains through GetInterfaces(), which is already the transitive closure.
                List<SetupMethodInfo> contract = null;
                foreach (var iface in type.GetInterfaces().Concat(new[] { type }).OrderBy(i => i.FullName ?? i.Name, StringComparer.Ordinal))
                {
                    var declared = GetDeclaredInterfaceContract(iface);
                    if (declared != null)
                    {
                        if (contract == null)
                        {
                            contract = new List<SetupMethodInfo>();
                        }
                        contract.AddRange(declared);
                    }
                }

                setups = contract?.ToArray();
                SetupInfoCached[type] = setups;
                return setups;
            }

            if (type.BaseType != null)
            {
                setups = GetSetupInfoForType(type.BaseType);
            }

            // Tagged contract members reachable from this type, paired with each member's implementing method here; used by the class scan below and the contract-entry pass after it. GetInterfaceMap is unavailable on generic type definitions, but those never own instances or setup nodes.
            List<(Type iface, SetupMethodInfo info, MethodInfo target)> contractMembers = null;
            if (!type.IsGenericTypeDefinition)
            {
                foreach (var iface in SortedInterfaces(type))
                {
                    var declared = GetDeclaredInterfaceContract(iface);
                    if (declared == null)
                    {
                        continue;
                    }

                    var map = type.GetInterfaceMap(iface);
                    foreach (var info in declared)
                    {
                        int index = Array.IndexOf(map.InterfaceMethods, info.method);
                        if (index < 0)
                        {
                            continue;
                        }

                        if (contractMembers == null)
                        {
                            contractMembers = new List<(Type, SetupMethodInfo, MethodInfo)>();
                        }
                        contractMembers.Add((iface, info, map.TargetMethods[index]));
                    }
                }
            }

            List<SetupMethodInfo> added = null;
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                // inherit: false is important; the default would find the base declaration's attribute through an override and misreport it as a new declaration
                var attribute = method.GetCustomAttribute<SetupAttribute>(inherit: false);
                if (attribute == null)
                {
                    if (method.GetCustomAttributes<SetupAfterAttribute>(inherit: false).Any() || method.GetCustomAttributes<SetupBeforeAttribute>(inherit: false).Any())
                    {
                        if (contractMembers != null && contractMembers.Any(c => c.target == method))
                        {
                            Dbg.Err($"{type}.{method.Name} implements a setup function and has a [Dec.SetupAfter] or [Dec.SetupBefore] attribute; ordering attributes on an implementation are ignored, declare them on the interface member");
                        }
                        else
                        {
                            Dbg.Err($"{type}.{method.Name} has a [Dec.SetupAfter] or [Dec.SetupBefore] attribute but no [Dec.Setup]; the ordering constraint has no effect");
                        }
                    }

                    continue;
                }

                var baseDefinition = method.GetBaseDefinition();
                if (baseDefinition.DeclaringType == typeof(Dec))
                {
                    Dbg.Err($"{type}.{method.Name} has a [Dec.Setup] attribute, but ConfigErrors and PostLoad already run automatically as part of setup; remove the attribute");
                    continue;
                }

                if (contractMembers != null)
                {
                    // Comparing against the map's target - the most-derived implementation - catches tagged overrides of an implementing method at every level, not just the level that introduced the interface.
                    var contract = contractMembers.FirstOrDefault(c => c.target == method);
                    if (contract.info != null)
                    {
                        Dbg.Wrn($"{type}.{method.Name} has a [Dec.Setup] attribute, but it is already a setup function through {contract.iface}.{contract.info.method.Name}; the interface declaration's settings are used and this method's ordering attributes are ignored");
                        continue;
                    }
                }

                if (baseDefinition != method && setups != null && Array.Exists(setups, s => s.method == baseDefinition))
                {
                    Dbg.Wrn($"{type}.{method.Name} has a [Dec.Setup] attribute, but it overrides a method that is already a setup function; the base declaration's settings are used");
                    continue;
                }

                if (!ValidateSetupSignature(method))
                {
                    continue;
                }

                if (added == null)
                {
                    added = new List<SetupMethodInfo>();
                }
                added.Add(new SetupMethodInfo { method = baseDefinition, attributeSource = method, parallel = attribute.Parallel, explicitStage = attribute.Stage });
            }

            // Contract entries are added only at the level that first implements the interface; lower levels inherit them, which both dedups a re-listed interface and keeps the cross-level conflict below warning once.
            if (contractMembers != null)
            {
                var inheritedInterfaces = new HashSet<Type>(type.BaseType?.GetInterfaces() ?? Type.EmptyTypes);
                foreach (var c in contractMembers)
                {
                    if (inheritedInterfaces.Contains(c.iface))
                    {
                        continue;
                    }

                    // Legacy hooks never appear in the entry list, so the conflict check below can't catch a contract member binding to them; without this, ConfigErrors/PostLoad would run a second time through the contract node.
                    var targetBase = c.target.GetBaseDefinition();
                    if (targetBase.DeclaringType == typeof(Dec))
                    {
                        Dbg.Err($"{c.iface}.{c.info.method.Name} is a setup function bound to {type}'s ConfigErrors or PostLoad, but those already run automatically as part of setup; the interface member is ignored");
                        continue;
                    }

                    // One body keeps one function identity; here the class-tagged identity was born first (its owners exist without the interface), so it wins - the mirror of the interface winning when both appear on one type.
                    if (setups != null && Array.Exists(setups, s => s.method == targetBase))
                    {
                        Dbg.Wrn($"{c.iface}.{c.info.method.Name} is a setup function implemented by {targetBase.DeclaringType}.{targetBase.Name}, which is already a setup function; the class declaration's settings are used, and bare setup dependencies on {c.iface} will not order against it (IncludeDerived dependencies will)");
                        continue;
                    }

                    if (added == null)
                    {
                        added = new List<SetupMethodInfo>();
                    }
                    added.Add(c.info);
                }
            }

            if (added != null)
            {
                if (setups != null)
                {
                    added.InsertRange(0, setups);
                }
                setups = added.ToArray();
            }

            SetupInfoCached[type] = setups;

            return setups;
        }

        // Shared between the instance-setup cache above and the parser's static-setup scan. The reporter parameter is deliberately mandatory; it's the encouraged error-reporting channel, and it's the only channel that works inside Parallel setup functions.
        internal static bool ValidateSetupSignature(MethodInfo method)
        {
            var parameters = method.GetParameters();
            if (method.ReturnType == typeof(void) && !method.IsGenericMethodDefinition && parameters.Length == 1 && parameters[0].ParameterType == typeof(Action<string>))
            {
                return true;
            }

            Dbg.Err($"Setup function {method.DeclaringType}.{method.Name} has an unsupported signature; setup functions must be `void M(Action<string> reporter)`");
            return false;
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

        internal static object CreateInstanceSafe(this Type type, string errorType, ReaderNode node, string missingCtorHint = null)
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
                    Dbg.Err($"{BuiltContext()}: Attempting to create {errorType} of type {type} without a no-argument constructor{(missingCtorHint != null ? $"; {missingCtorHint}" : "")}");
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
