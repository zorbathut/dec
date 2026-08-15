using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dec
{
    // Engine for the post-load setup pass: collects instances of setup-bearing types during parser and recorder loads, then builds and executes the setup dependency graph before the load returns.
    internal static class Setup
    {
        internal class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

            bool IEqualityComparer<object>.Equals(object lhs, object rhs)
            {
                return ReferenceEquals(lhs, rhs);
            }

            int IEqualityComparer<object>.GetHashCode(object obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }

        internal enum Mode
        {
            Parser,
            Recorder,
        }

        // One Collection per load operation (a Parser run, or one Recorder.Read/ReadSimple call), carried through ReaderGlobals. Per-operation isolation is what makes concurrent Recorder.Read calls safe; registration order stays deterministic because each single load parses single-threaded.
        internal class Collection
        {
            // Keyed by concrete runtime type; list preserves registration order.
            private readonly Dictionary<Type, List<object>> instancesByType = new Dictionary<Type, List<object>>();
            // Dedup for genuinely shared instances - a ConverterString returning the same object for two elements, or a savegame ref resolved through multiple pointers. Per-type sets suffice; a shared instance has exactly one concrete type.
            private readonly Dictionary<Type, HashSet<object>> instancesSeen = new Dictionary<Type, HashSet<object>>();
            // Recorder loads must not re-run setup on objects owned by the dec database (dec-path refs); the parser must NOT apply this exclusion, because its hook registers the dec path and the instance in the same call and the exclusion would suppress its entire collection.
            private readonly bool excludeDatabaseOwned;

            internal Collection(bool excludeDatabaseOwned)
            {
                this.excludeDatabaseOwned = excludeDatabaseOwned;
            }

            internal bool IsEmpty => instancesByType.Count == 0;
            internal Dictionary<Type, List<object>> InstancesByType => instancesByType;

            // Called from Serialization.ParseElement's hook during parser and recorder loads, and from RecorderApi's refs sweep.
            internal void RegisterInstance(object instance)
            {
                if (instance == null)
                {
                    return;
                }

                var type = instance.GetType();

                // Value types can't carry setup functions; dec instances get their setup via the parser's database bucketing, never through instance collection.
                if (type.IsValueType || typeof(Dec).IsAssignableFrom(type))
                {
                    return;
                }

                if (UtilReflection.GetSetupInfoForType(type) == null)
                {
                    return;
                }

                // Reference-identity check; runs after the setup-info check purely because that one rejects most objects fastest.
                if (excludeDatabaseOwned && Database.DecPathKnown(instance))
                {
                    return;
                }

                if (!instancesByType.TryGetValue(type, out var list))
                {
                    list = new List<object>();
                    instancesByType[type] = list;
                    instancesSeen[type] = new HashSet<object>(ReferenceEqualityComparer.Instance);
                }

                if (instancesSeen[type].Add(instance))
                {
                    list.Add(instance);
                }
            }
        }

        internal struct StaticSetupMethod
        {
            public MethodInfo method;
            public SetupAttribute attribute;
        }

        internal enum NodeKind
        {
            StageBegin,
            StageEnd,
            LegacyConfigErrors,
            LegacyPostLoad,
            StaticMethod,
            InstanceMethod,
        }

        // Ordinal-comparing tiebreak key; deliberately not a concatenated string, since Dag's OrderBy would compare that under the current culture and linguistic collation of punctuation is not stable across cultures.
        internal struct SortKey : IComparable<SortKey>
        {
            public string ownerName;
            public int rank;
            public string memberName;

            public SortKey(Type owner, int rank, string memberName)
            {
                this.ownerName = owner.FullName ?? owner.Name;
                this.rank = rank;
                this.memberName = memberName;
            }

            public int CompareTo(SortKey other)
            {
                int c = string.CompareOrdinal(ownerName, other.ownerName);
                if (c != 0)
                {
                    return c;
                }

                c = rank.CompareTo(other.rank);
                if (c != 0)
                {
                    return c;
                }

                return string.CompareOrdinal(memberName ?? "", other.memberName ?? "");
            }
        }

        internal class Node
        {
            public NodeKind kind;
            public Type owner;
            public MethodInfo method;
            public MethodInfo attributeSource;
            public Type explicitStage;
            public bool parallel;
            public bool includeDerivedSentinel;
            public List<object> instances;
            public SortKey sortKey;

            public override string ToString()
            {
                switch (kind)
                {
                    case NodeKind.StageBegin: return $"{owner} setup stage{(includeDerivedSentinel ? " (IncludeDerived)" : "")} begin";
                    case NodeKind.StageEnd: return $"{owner} setup stage{(includeDerivedSentinel ? " (IncludeDerived)" : "")} end";
                    case NodeKind.LegacyConfigErrors: return $"{owner} ConfigErrors";
                    case NodeKind.LegacyPostLoad: return $"{owner} PostLoad";
                    default: return $"{owner}.{method.Name}";
                }
            }
        }

        // Each stage has two nested sentinel pairs, chained hierarchyBegin -> ownBegin -> ownEnd -> hierarchyEnd. The inner "own" pair brackets the stage's own setup functions (declared on the stage type or inherited into it); the outer "hierarchy" pair additionally brackets functions introduced by derived types, which is what IncludeDerived dependencies attach to. Own membership is a strict subset of hierarchy membership, so the nesting edges are always semantically true - and they keep mixed-variant third parties transitively ordered even when the stage has no members this load.
        private class StageInfo
        {
            public Node ownBegin;
            public Node ownEnd;
            public Node hierarchyBegin;
            public Node hierarchyEnd;
            public bool valid;

            // Membership and reference tracking for the deferred empty-stage warning; whether a dependency is suspicious depends on which variant it referenced.
            public bool anyOwn;
            public bool anyHierarchy;
            public bool referencedOwn;
            public bool referencedHierarchy;

            public Node Begin(bool hierarchy)
            {
                return hierarchy ? hierarchyBegin : ownBegin;
            }

            public Node End(bool hierarchy)
            {
                return hierarchy ? hierarchyEnd : ownEnd;
            }

            public void MarkReferenced(bool hierarchy)
            {
                if (hierarchy)
                {
                    referencedHierarchy = true;
                }
                else
                {
                    referencedOwn = true;
                }
            }
        }

        // The type a node's function is declared on, used for declared-on stage membership; the legacy hooks are declared on Dec itself, which makes them part of every dec type's own setup.
        private static Type FunctionDeclaringType(Node node)
        {
            switch (node.kind)
            {
                case NodeKind.LegacyConfigErrors:
                case NodeKind.LegacyPostLoad:
                    return typeof(Dec);
                default:
                    // for instance functions, method is already the base definition or the interface member
                    return node.method.DeclaringType;
            }
        }

        private static readonly List<StaticSetupMethod> EmptyStatics = new List<StaticSetupMethod>();
        private static readonly Dictionary<Type, List<Dec>> EmptyDecs = new Dictionary<Type, List<Dec>>();

        internal static void ExecuteParser(List<StaticSetupMethod> staticSetupMethods, Dictionary<Type, List<Dec>> decsByType, Collection collection)
        {
            ExecuteCore(staticSetupMethods, decsByType, collection, Mode.Parser);
        }

        internal static void ExecuteRecorder(Collection collection)
        {
            // the common case: a Read in a project with no instance setup functions pays one branch here and never touches the graph machinery
            if (collection.IsEmpty)
            {
                return;
            }

            ExecuteCore(EmptyStatics, EmptyDecs, collection, Mode.Recorder);
        }

        private static void ExecuteCore(List<StaticSetupMethod> staticSetupMethods, Dictionary<Type, List<Dec>> decsByType, Collection collection, Mode mode)
        {
            // Method and legacy nodes; stage sentinels live in a separate list so membership scans over this one stay stable.
            var methodNodes = new List<Node>();
            var sentinelNodes = new List<Node>();
            var edges = new List<Dag<Node>.Dependency>();
            var stages = new Dictionary<Type, StageInfo>();
            var classLevelProcessed = new HashSet<Type>();

            var sortedDecsByType = new Dictionary<Type, List<object>>();
            foreach (var kvp in decsByType)
            {
                sortedDecsByType[kvp.Key] = kvp.Value.OrderBy(d => d.DecName, StringComparer.Ordinal).Cast<object>().ToList();
            }

            // Legacy nodes: one ConfigErrors and one PostLoad node per concrete dec type, always, matching the old always-call behavior.
            foreach (var kvp in sortedDecsByType)
            {
                var cfg = new Node { kind = NodeKind.LegacyConfigErrors, owner = kvp.Key, instances = kvp.Value, sortKey = new SortKey(kvp.Key, 1, "ConfigErrors") };
                var pl = new Node { kind = NodeKind.LegacyPostLoad, owner = kvp.Key, instances = kvp.Value, sortKey = new SortKey(kvp.Key, 1, "PostLoad") };
                methodNodes.Add(cfg);
                methodNodes.Add(pl);
                edges.Add(new Dag<Node>.Dependency { before = cfg, after = pl });
            }

            // Static nodes.
            foreach (var ssm in staticSetupMethods)
            {
                if (ssm.attribute.Parallel)
                {
                    Dbg.Wrn($"{ssm.method.DeclaringType}.{ssm.method.Name} is a static setup function marked Parallel; it runs once, so Parallel has no effect");
                }

                methodNodes.Add(new Node { kind = NodeKind.StaticMethod, owner = ssm.method.DeclaringType, method = ssm.method, attributeSource = ssm.method, explicitStage = ssm.attribute.Stage, sortKey = new SortKey(ssm.method.DeclaringType, 1, ssm.method.Name) });
            }

            // Instance-method nodes, one per (base-definition method, concrete type). Dec instances come from the database bucketing; everything else comes from the load's collection.
            var instanceOwners = sortedDecsByType.Keys.Concat(collection.InstancesByType.Keys).Distinct().OrderBy(t => t.FullName ?? t.Name, StringComparer.Ordinal);
            foreach (var owner in instanceOwners)
            {
                var setupInfo = UtilReflection.GetSetupInfoForType(owner);
                if (setupInfo == null)
                {
                    continue;
                }

                List<object> instances = sortedDecsByType.TryGetValue(owner, out var decs) ? decs : collection.InstancesByType[owner];

                foreach (var info in setupInfo)
                {
                    methodNodes.Add(new Node { kind = NodeKind.InstanceMethod, owner = owner, method = info.method, attributeSource = info.attributeSource, explicitStage = info.explicitStage, parallel = info.parallel, instances = instances, sortKey = new SortKey(owner, 1, info.method.Name) });
                }
            }

            methodNodes.Sort((a, b) => a.sortKey.CompareTo(b.sortKey));

            // Everything after this point must iterate in deterministic order; methodNodes is sorted and all further additions happen in an order derived from it.

            List<Node> SynthesizeNodesForType(Type type)
            {
                var created = new List<Node>();

                if (methodNodes.Any(n => n.owner == type))
                {
                    return created;
                }

                var setupInfo = UtilReflection.GetSetupInfoForType(type);
                if (setupInfo == null)
                {
                    return created;
                }

                foreach (var info in setupInfo)
                {
                    var node = new Node { kind = NodeKind.InstanceMethod, owner = type, method = info.method, attributeSource = info.attributeSource, explicitStage = info.explicitStage, parallel = info.parallel, instances = new List<object>(), sortKey = new SortKey(type, 1, info.method.Name) };
                    methodNodes.Add(node);
                    created.Add(node);
                }

                return created;
            }

            // Pre-synthesize nodes for referenced types that have setup functions but no collected instances, so stage membership sees them no matter what order stages get created in. References are chased transitively - a synthesized type's own dependencies can name further types that also need synthesis. Dec-derived types are excluded; a dependency on a dec type with no instances is an error, handled at resolution time.
            {
                var pending = new Queue<Type>();
                var examined = new HashSet<Type>();

                void EnqueueMethodTargets(Node node)
                {
                    if (node.explicitStage != null)
                    {
                        pending.Enqueue(node.explicitStage);
                    }

                    if (node.attributeSource != null)
                    {
                        foreach (var attr in node.attributeSource.GetCustomAttributes<SetupAfterAttribute>(inherit: false))
                        {
                            if (attr.Type != null)
                            {
                                pending.Enqueue(attr.Type);
                            }
                        }

                        foreach (var attr in node.attributeSource.GetCustomAttributes<SetupBeforeAttribute>(inherit: false))
                        {
                            if (attr.Type != null)
                            {
                                pending.Enqueue(attr.Type);
                            }
                        }
                    }
                }

                foreach (var node in methodNodes)
                {
                    EnqueueMethodTargets(node);
                }

                foreach (var owner in methodNodes.Select(n => n.owner).Distinct().ToList())
                {
                    pending.Enqueue(owner);
                }

                while (pending.Count > 0)
                {
                    var type = pending.Dequeue();
                    if (!examined.Add(type))
                    {
                        continue;
                    }

                    // Interfaces carry ordering attributes that inherit:true can't surface through an implementor, so they're chased explicitly; sorted because synthesis order feeds edge insertion order, which must stay deterministic.
                    foreach (var iface in type.GetInterfaces().OrderBy(i => i.FullName ?? i.Name, StringComparer.Ordinal))
                    {
                        pending.Enqueue(iface);
                    }

                    foreach (var attr in type.GetCustomAttributes<SetupAfterAttribute>(inherit: true))
                    {
                        if (attr.Type != null)
                        {
                            pending.Enqueue(attr.Type);
                        }
                    }

                    foreach (var attr in type.GetCustomAttributes<SetupBeforeAttribute>(inherit: true))
                    {
                        if (attr.Type != null)
                        {
                            pending.Enqueue(attr.Type);
                        }
                    }

                    if (typeof(Dec).IsAssignableFrom(type) || type.IsValueType || type.IsGenericTypeDefinition)
                    {
                        continue;
                    }

                    foreach (var synthesized in SynthesizeNodesForType(type))
                    {
                        EnqueueMethodTargets(synthesized);
                    }
                }
            }

            StageInfo GetStage(Type stageType)
            {
                if (stages.TryGetValue(stageType, out var existing))
                {
                    return existing.valid ? existing : null;
                }

                if (stageType.IsValueType || stageType.IsGenericTypeDefinition)
                {
                    Dbg.Err($"{stageType} is not usable as a setup stage");
                    stages[stageType] = new StageInfo { valid = false };
                    return null;
                }

                var info = new StageInfo
                {
                    ownBegin = new Node { kind = NodeKind.StageBegin, owner = stageType, sortKey = new SortKey(stageType, 0, null) },
                    ownEnd = new Node { kind = NodeKind.StageEnd, owner = stageType, sortKey = new SortKey(stageType, 2, null) },
                    hierarchyBegin = new Node { kind = NodeKind.StageBegin, owner = stageType, includeDerivedSentinel = true, sortKey = new SortKey(stageType, 0, "IncludeDerived") },
                    hierarchyEnd = new Node { kind = NodeKind.StageEnd, owner = stageType, includeDerivedSentinel = true, sortKey = new SortKey(stageType, 2, "IncludeDerived") },
                    valid = true,
                };

                // registered before processing class-level attributes, so mutually-referencing stages can't recurse forever
                stages[stageType] = info;
                sentinelNodes.Add(info.ownBegin);
                sentinelNodes.Add(info.ownEnd);
                sentinelNodes.Add(info.hierarchyBegin);
                sentinelNodes.Add(info.hierarchyEnd);

                // even a stage with no members must order its dependents transitively; whether a stage happens to have instances this load must not change ordering between third parties, and the nesting chain extends that guarantee across the two membership variants
                edges.Add(new Dag<Node>.Dependency { before = info.hierarchyBegin, after = info.ownBegin });
                edges.Add(new Dag<Node>.Dependency { before = info.ownBegin, after = info.ownEnd });
                edges.Add(new Dag<Node>.Dependency { before = info.ownEnd, after = info.hierarchyEnd });

                foreach (var node in methodNodes)
                {
                    if (NodeIsMemberOfStage(node, stageType, includeDerived: false))
                    {
                        edges.Add(new Dag<Node>.Dependency { before = info.ownBegin, after = node });
                        edges.Add(new Dag<Node>.Dependency { before = node, after = info.ownEnd });
                        info.anyOwn = true;
                        info.anyHierarchy = true;
                    }
                    else if (NodeIsMemberOfStage(node, stageType, includeDerived: true))
                    {
                        edges.Add(new Dag<Node>.Dependency { before = info.hierarchyBegin, after = node });
                        edges.Add(new Dag<Node>.Dependency { before = node, after = info.hierarchyEnd });
                        info.anyHierarchy = true;
                    }
                }

                // A pure marker class owns no setup functions, so the class-level attribute pass below never visits it; its own ordering attributes get processed here instead.
                if (!methodNodes.Any(n => n.owner == stageType))
                {
                    ProcessClassLevelAttributes(stageType);
                }

                return info;
            }

            bool NodeIsMemberOfStage(Node node, Type stageType, bool includeDerived)
            {
                if (node.explicitStage == stageType)
                {
                    return true;
                }

                if (!stageType.IsAssignableFrom(node.owner))
                {
                    return false;
                }

                if (includeDerived)
                {
                    return true;
                }

                // For interface stages, FunctionDeclaringType is the interface for contract functions, so this covers "the contract's implementations" the same way it covers "the class's own functions" for class stages.
                return FunctionDeclaringType(node).IsAssignableFrom(stageType);
            }

            bool ValidateDecTargetHasInstances(object site, Type target)
            {
                // During a recorder load, dec setup already ran at parse time; a dec-targeted dependency is simply satisfied.
                if (mode == Mode.Recorder)
                {
                    return true;
                }

                if (typeof(Dec).IsAssignableFrom(target) && !decsByType.Keys.Any(k => target.IsAssignableFrom(k)))
                {
                    Dbg.Err($"{site} has a setup dependency on {target}, but no instances of it exist");
                    return false;
                }

                return true;
            }

            // Resolves a (type, memberName) reference to the matching setup nodes; stage-like in that one name can legitimately match nodes on several concrete types, but distinct *functions* sharing the name are ambiguous. Only functions belonging to the target's own setup match: a name introduced below the target is treated as nonexistent, and a derived new-hide doesn't create ambiguity.
            List<Node> ResolveNamedTarget(object site, Type target, string memberName)
            {
                bool NameMatches(Node n)
                {
                    switch (n.kind)
                    {
                        case NodeKind.LegacyConfigErrors: return memberName == "ConfigErrors";
                        case NodeKind.LegacyPostLoad: return memberName == "PostLoad";
                        case NodeKind.InstanceMethod:
                        case NodeKind.StaticMethod: return n.method.Name == memberName;
                        default: return false;
                    }
                }

                var candidates = methodNodes.Where(n => target.IsAssignableFrom(n.owner) && FunctionDeclaringType(n).IsAssignableFrom(target) && NameMatches(n)).ToList();

                if (candidates.Count == 0)
                {
                    if (typeof(Dec).IsAssignableFrom(target))
                    {
                        // during recorder loads a dec-targeted named dependency is silently satisfied, same as the bare-type case
                        if (mode == Mode.Parser)
                        {
                            if (!decsByType.Keys.Any(k => target.IsAssignableFrom(k)))
                            {
                                Dbg.Err($"{site} has a setup dependency on {target}.{memberName}, but no instances of {target} exist");
                            }
                            else
                            {
                                Dbg.Err($"{site} references setup function {target}.{memberName}, but no such setup function exists");
                            }
                        }
                    }
                    else
                    {
                        // Recorder loads have no static nodes, and synthesis only covers instance functions - so before calling this a typo, check whether the name is a real static setup function, which is simply satisfied (it ran at parse time).
                        if (mode == Mode.Recorder && target.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).Any(m => m.Name == memberName && m.GetCustomAttribute<SetupAttribute>(inherit: false) != null))
                        {
                            return null;
                        }

                        // a named function that doesn't exist anywhere is a declaration bug (typo), not an absent-this-load situation - synthesis creates empty nodes for absent-but-real instance functions
                        Dbg.Err($"{site} references setup function {target}.{memberName}, but no such setup function exists");
                    }

                    return null;
                }

                var distinctFunctions = candidates.Select(n => n.kind == NodeKind.LegacyConfigErrors || n.kind == NodeKind.LegacyPostLoad ? (object)n.kind : n.method).Distinct().Count();
                if (distinctFunctions > 1)
                {
                    Dbg.Err($"{site} references setup function {target}.{memberName}, but the name is ambiguous between multiple distinct setup functions");
                    return null;
                }

                return candidates;
            }

            // Method-level [SetupAfter]/[SetupBefore] on setup functions.
            foreach (var node in methodNodes.ToList())
            {
                if (node.attributeSource == null)
                {
                    continue;
                }

                void ProcessMethodEdge(Type target, string memberName, bool includeDerived, bool after)
                {
                    if (target == null)
                    {
                        Dbg.Err($"{node} has a setup dependency on a null type");
                        return;
                    }

                    if (memberName != null)
                    {
                        if (includeDerived)
                        {
                            Dbg.Err($"{node} has a setup dependency on {target}.{memberName} with IncludeDerived; IncludeDerived is only meaningful for bare-type dependencies");
                            return;
                        }

                        var targets = ResolveNamedTarget(node, target, memberName);
                        if (targets == null)
                        {
                            return;
                        }

                        foreach (var t in targets)
                        {
                            if (t == node)
                            {
                                Dbg.Err($"{node} has a setup dependency on itself; ignoring the dependency");
                                continue;
                            }

                            edges.Add(after ? new Dag<Node>.Dependency { before = t, after = node } : new Dag<Node>.Dependency { before = node, after = t });
                        }

                        return;
                    }

                    if (NodeIsMemberOfStage(node, target, includeDerived))
                    {
                        Dbg.Err($"{node} has a setup dependency on {target}, but it is itself part of {target}'s setup stage; ignoring the dependency");
                        return;
                    }

                    if (!ValidateDecTargetHasInstances(node, target))
                    {
                        return;
                    }

                    var stage = GetStage(target);
                    if (stage == null)
                    {
                        return;
                    }

                    stage.MarkReferenced(includeDerived);
                    edges.Add(after ? new Dag<Node>.Dependency { before = stage.End(includeDerived), after = node } : new Dag<Node>.Dependency { before = node, after = stage.Begin(includeDerived) });
                }

                foreach (var attr in node.attributeSource.GetCustomAttributes<SetupAfterAttribute>(inherit: false))
                {
                    ProcessMethodEdge(attr.Type, attr.MemberName, attr.IncludeDerived, after: true);
                }

                foreach (var attr in node.attributeSource.GetCustomAttributes<SetupBeforeAttribute>(inherit: false))
                {
                    ProcessMethodEdge(attr.Type, attr.MemberName, attr.IncludeDerived, after: false);
                }

                if (node.explicitStage != null)
                {
                    GetStage(node.explicitStage);
                }
            }

            // Class-level [SetupAfter]/[SetupBefore]: stage-to-stage constraints. Read per concrete owner with inheritance, so a constraint on a base class applies to each derived type's stage as well. Also called from GetStage for pure marker classes, which own no nodes and are therefore missed by the owner pass below.
            void ProcessClassLevelAttributes(Type classType)
            {
                // Reachable from the marker branch in GetStage, the per-owner pass, and the owners'-interfaces pass; process each type once no matter which finds it first.
                if (!classLevelProcessed.Add(classType))
                {
                    return;
                }

                void ProcessClassEdge(Type target, string memberName, bool includeDerived, bool after)
                {
                    if (target == null)
                    {
                        Dbg.Err($"{classType} has a setup dependency on a null type");
                        return;
                    }

                    if (memberName != null)
                    {
                        Dbg.Err($"{classType} has a class-level setup dependency naming member {target}.{memberName}; member-specific dependencies are only supported on setup functions, not classes");
                        return;
                    }

                    // The ancestor short-circuit fires even when the ancestor declares no functions of its own and the stages therefore wouldn't overlap; "run my own stage after my ancestor's" is at best a no-op there, and erroring uniformly keeps the rule predictable. Method-level ancestor dependencies are the supported form. The own-side membership variant must match the pair the edge below binds - hierarchy for interfaces - or an overlap surfaces as a raw cycle instead of this diagnostic.
                    if (target.IsAssignableFrom(classType) || methodNodes.Any(n => NodeIsMemberOfStage(n, classType, includeDerived: classType.IsInterface) && NodeIsMemberOfStage(n, target, includeDerived)))
                    {
                        Dbg.Err($"{classType} has a setup dependency on {target}, but it is itself part of {target}'s setup stage; ignoring the dependency");
                        return;
                    }

                    if (!ValidateDecTargetHasInstances(classType, target))
                    {
                        return;
                    }

                    var ownStage = GetStage(classType);
                    var targetStage = GetStage(target);
                    if (ownStage == null || targetStage == null)
                    {
                        return;
                    }

                    // The own side binds the class's own setup; functions introduced by derived classes get constrained when the per-owner pass processes the inherited attribute against the derived owner's own stage. That inheritance leg doesn't exist for interfaces - GetCustomAttributes never traverses them - so an interface's class-level attributes bind its hierarchy stage to keep constraining what implementors declare beyond the contract.
                    bool targetHierarchy = includeDerived;
                    bool ownHierarchy = classType.IsInterface;
                    targetStage.MarkReferenced(targetHierarchy);
                    // For warning purposes the own side counts as a hierarchy reference: attribute inheritance extends the constraint to derived owners, so a class whose own setup is empty but whose derived classes have functions is fully enforced, not suspicious.
                    ownStage.MarkReferenced(hierarchy: true);
                    edges.Add(after ? new Dag<Node>.Dependency { before = targetStage.End(targetHierarchy), after = ownStage.Begin(ownHierarchy) } : new Dag<Node>.Dependency { before = ownStage.End(ownHierarchy), after = targetStage.Begin(targetHierarchy) });
                }

                foreach (var attr in classType.GetCustomAttributes<SetupAfterAttribute>(inherit: true))
                {
                    ProcessClassEdge(attr.Type, attr.MemberName, attr.IncludeDerived, after: true);
                }

                foreach (var attr in classType.GetCustomAttributes<SetupBeforeAttribute>(inherit: true))
                {
                    ProcessClassEdge(attr.Type, attr.MemberName, attr.IncludeDerived, after: false);
                }
            }

            // Owners' interfaces are included because attribute inheritance never traverses interfaces; without this leg, ordering attributes declared on an interface would only be discovered when something happens to reference it.
            foreach (var owner in methodNodes.Select(n => n.owner).Concat(methodNodes.SelectMany(n => n.owner.GetInterfaces())).Distinct().OrderBy(t => t.FullName ?? t.Name, StringComparer.Ordinal).ToList())
            {
                ProcessClassLevelAttributes(owner);
            }

            // Empty-stage warnings, deferred until every dependency is attached because they depend on which membership variants got referenced. Suppressed during recorder loads (a stage with nothing present in this savegame is satisfied, not suspicious) and for dec stage types (a dec type's own setup always contains ConfigErrors/PostLoad, so an empty dec stage just means no instances - already diagnosed by ValidateDecTargetHasInstances).
            if (mode == Mode.Parser)
            {
                foreach (var kvp in stages.OrderBy(kvp => kvp.Key.FullName ?? kvp.Key.Name, StringComparer.Ordinal))
                {
                    var stageType = kvp.Key;
                    var info = kvp.Value;
                    if (!info.valid || typeof(Dec).IsAssignableFrom(stageType))
                    {
                        continue;
                    }

                    if (info.referencedOwn && !info.anyOwn)
                    {
                        if (info.anyHierarchy)
                        {
                            Dbg.Wrn($"{stageType} is referenced in setup ordering, but contains no setup functions of its own; {(stageType.IsInterface ? "its implementors" : "its derived classes")} do, so use IncludeDerived = true if you meant those");
                        }
                        else
                        {
                            Dbg.Wrn($"{stageType} is referenced in setup ordering, but contains no setup functions; the constraint has no effect");
                        }
                    }
                    else if (info.referencedHierarchy && !info.anyHierarchy)
                    {
                        Dbg.Wrn($"{stageType} is referenced in setup ordering, but contains no setup functions; the constraint has no effect");
                    }
                }
            }

            var order = Dag<Node>.CalculateOrder(methodNodes.Concat(sentinelNodes), edges, n => n.sortKey);

            foreach (var node in order)
            {
                ExecuteNode(node);
            }
        }

        private static void ExecuteNode(Node node)
        {
            switch (node.kind)
            {
                case NodeKind.StageBegin:
                case NodeKind.StageEnd:
                    return;

                case NodeKind.LegacyConfigErrors:
                    foreach (Dec dec in node.instances)
                    {
                        try
                        {
                            // the engine is the one legitimate caller of the deprecated hook
                            #pragma warning disable CS0618
                            dec.ConfigErrors(err => Dbg.Err($"{dec}: {err}"));
                            #pragma warning restore CS0618
                        }
                        catch (Exception e)
                        {
                            Dbg.Ex(new Exception($"Exception thrown during ConfigErrors on {dec}", e));
                        }
                    }
                    return;

                case NodeKind.LegacyPostLoad:
                    foreach (Dec dec in node.instances)
                    {
                        try
                        {
                            // the engine is the one legitimate caller of the deprecated hook
                            #pragma warning disable CS0618
                            dec.PostLoad(err => Dbg.Err($"{dec}: {err}"));
                            #pragma warning restore CS0618
                        }
                        catch (Exception e)
                        {
                            Dbg.Ex(new Exception($"Exception thrown during PostLoad on {dec}", e));
                        }
                    }
                    return;

                case NodeKind.StaticMethod:
                    InvokeSetupMethod(node, null, err => Dbg.Err($"{node.owner}.{node.method.Name}: {err}"));
                    return;

                case NodeKind.InstanceMethod:
                    if (!node.parallel)
                    {
                        foreach (var instance in node.instances)
                        {
                            InvokeSetupMethod(node, instance, err => Dbg.Err($"{InstancePrefix(node, instance)}: {err}"));
                        }
                    }
                    else
                    {
                        ExecuteParallel(node);
                    }
                    return;
            }
        }

        private static string InstancePrefix(Node node, object instance)
        {
            return instance is Dec dec ? dec.ToString() : node.owner.ToString();
        }

        private static void InvokeSetupMethod(Node node, object instance, Action<string> reporter)
        {
            try
            {
                node.method.Invoke(instance, new object[] { reporter });
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException && e.InnerException != null)
                {
                    e = e.InnerException;
                }

                string suffix = instance is Dec dec ? $" on {dec}" : "";
                Dbg.Ex(new Exception($"Exception thrown during setup function {node.owner}.{node.method.Name}{suffix}", e));
            }
        }

        private static void ExecuteParallel(Node node)
        {
            var instances = node.instances;

            System.Threading.Tasks.Parallel.For(0, instances.Count, i =>
            {
                try
                {
                    // CurrentCulture flows into Parallel work items via ExecutionContext on every supported .NET runtime, so this assignment is belt-and-suspenders for less-certain runtimes like Mono. It's deliberately a bare assignment rather than a restoring scope; a scope's Dispose can itself error on the worker if user code mutates the culture.
                    System.Threading.Thread.CurrentThread.CurrentCulture = Config.CultureInfo;

                    var instance = instances[i];

                    // Reports go straight through the Config handlers from whatever thread this lands on; handlers are required to be threadsafe when threading is in use, as documented on Config.
                    InvokeSetupMethod(node, instance, err => Dbg.Err($"{InstancePrefix(node, instance)}: {err}"));
                }
                catch
                {
                    // A throwing error handler is a legitimate configuration, but in practice the only thing that reaches here is a handler's own throw after it received the message, and letting it escape would tear down the whole Parallel loop and take sibling instances with it.
                }
            });
        }
    }
}
