using System;
using System.Collections.Generic;
using System.Linq;

namespace Dec.Modding
{
    // Turns a set of read manifests into a load order and a problem list. Touches no disk, and reports only its own internal errors.
    internal class ModResolver
    {
        private struct Edge
        {
            internal Mod before;
            internal Mod after;
            internal Mod declarer;
            internal ModDependency entry;   // null for the implicit base edge
            internal bool preference;       // from an entry that didn't write its order; dropped if it takes part in a cycle
        }

        private readonly Mod modBase;
        private readonly ModDependency dependencyBase;
        private readonly List<Mod> installed;
        private readonly Dictionary<Mod, ModStatus> status;
        private readonly List<ModProblem> problems;
        private readonly IReadOnlyDictionary<string, AssemblyReserved> assembliesReserved;
        private readonly string decBuiltInto;

        private readonly HashSet<ModAssembly> assembliesSkipped = new HashSet<ModAssembly>();

        private readonly HashSet<(Mod, Mod)> edgesDropped = new HashSet<(Mod, Mod)>();

        private Dictionary<string, Mod> lookup;
        private Dictionary<string, List<Mod>> lookupCaseless;

        // Dec's own assembly, as its package ships it.
        internal const string AssemblyDec = "dec";

        private const string ReferenceWithoutCopying = "`<Private>false</Private>` on a `Reference`, `ExcludeAssets=\"runtime\"` on a `PackageReference`, and both on a `ProjectReference`";

        private static readonly IComparer<SemanticVersioning.Version> VersionComparer = Comparer<SemanticVersioning.Version>.Create((a, b) =>
        {
            if (a == null || b == null)
            {
                return (a == null ? 0 : 1) - (b == null ? 0 : 1);
            }
            return a.CompareTo(b);
        });

        private ModResolver(Mod modBase, IEnumerable<Mod> candidates, List<ModProblem> problems, IReadOnlyDictionary<string, AssemblyReserved> assembliesReserved, string decBuiltInto)
        {
            this.modBase = modBase;
            this.problems = problems;
            this.assembliesReserved = assembliesReserved;
            this.decBuiltInto = decBuiltInto;

            dependencyBase = ModDependency.CreateImplicitBase(modBase.Id);

            // A canonical order, so results don't depend on the order mods were found in.
            installed = new List<Mod>() { modBase };
            installed.AddRange(candidates.OrderBy(mod => mod.Id ?? "", StringComparer.Ordinal).ThenBy(mod => mod.Directory ?? "", StringComparer.Ordinal));

            status = installed.ToDictionary(mod => mod, mod => mod.manifestError ? ModStatus.Disabled : ModStatus.Enabled);
        }

        // `decBuiltInto` is the game assembly that Dec's source is built into, or null when the game references Dec's own assembly.
        internal static ModResolution Resolve(Mod modBase, IEnumerable<Mod> candidates, List<ModProblem> problems, IReadOnlyDictionary<string, AssemblyReserved> assembliesReserved, string decBuiltInto)
        {
            return new ModResolver(modBase, candidates, problems, assembliesReserved, decBuiltInto).Run();
        }

        // The phases run in this order so that a mod is never disabled as Incompatible, or for an assembly clash, because of a mod that the Required cascade or cycle detection already ruled out.
        private ModResolution Run()
        {
            Deduplicate();
            CheckIdConflicts();
            CheckIntrinsic();
            Cascade();
            CheckCycles();
            CheckIncompatible();
            CheckAssemblyClashes();

            var order = Order();

            CheckSoft();

            return new ModResolution(installed, status, order, problems, assembliesSkipped);
        }

        private IEnumerable<Mod> Enabled()
        {
            return installed.Where(mod => !mod.IsBase && status[mod] == ModStatus.Enabled);
        }

        private IEnumerable<ModDependency> Entries(Mod mod)
        {
            var entries = mod.Dependencies.Where(dependency => !dependency.invalid);
            if (entries.Any(dependency => dependency.Id == modBase.Id))
            {
                return entries;
            }
            return entries.Append(dependencyBase);
        }

        private Mod Target(ModDependency dependency)
        {
            return lookup.TryGetValue(dependency.Id, out var target) ? target : null;
        }

        private static bool Applies(ModDependency dependency, Mod target)
        {
            return target != null && target.Version != null && ModValidation.IsSatisfied(dependency.Version, target.Version);
        }

        private void Error(ModProblemKind kind, List<Mod> mods, ModDependency dependency, string message)
        {
            problems.Add(new ModProblem(ModProblemSeverity.Error, kind, mods, dependency, message));
        }

        private void Disable(IEnumerable<Mod> mods)
        {
            foreach (var mod in mods.ToList())
            {
                status[mod] = ModStatus.Disabled;
            }
        }

        private static string Range(ModDependency dependency)
        {
            return dependency.Version == null ? "" : $" `{dependency.Version}`";
        }

        private static string Explanation(ModDependency dependency)
        {
            return string.IsNullOrWhiteSpace(dependency.Text) ? "" : $" ({dependency.Text})";
        }

        private void Deduplicate()
        {
            foreach (var group in installed.Where(mod => mod.Id != null).GroupBy(mod => mod.Id, StringComparer.Ordinal))
            {
                if (group.Count() < 2)
                {
                    continue;
                }

                // Prefer a working copy, then the newest, then the first by path.
                var ordered = group
                    .OrderBy(mod => mod.IsBase ? 0 : 1)
                    .ThenBy(mod => mod.manifestError)
                    .ThenByDescending(mod => mod.Version, VersionComparer)
                    .ThenBy(mod => mod.Directory ?? "", StringComparer.Ordinal)
                    .ToList();

                if (ordered[0].IsBase)
                {
                    // Reusing the base game's id is an id conflict, not a duplicate.
                    continue;
                }

                foreach (var hidden in ordered.Skip(1))
                {
                    status[hidden] = ModStatus.Hidden;
                }

                problems.Add(new ModProblem(ModProblemSeverity.Warning, ModProblemKind.Duplicate, ordered, null, $"Mod `{group.Key}` is installed {ordered.Count} times; using {Describe(ordered[0])} and ignoring {string.Join(", ", ordered.Skip(1).Select(Describe))}"));
            }

            string Describe(Mod mod)
            {
                return $"version {mod.Version?.ToString() ?? "(invalid)"} at `{mod.Directory}`";
            }
        }

        private void CheckIdConflicts()
        {
            var visible = installed.Where(mod => mod.Id != null && status[mod] != ModStatus.Hidden).ToList();

            foreach (var group in visible.GroupBy(mod => mod.Id, StringComparer.OrdinalIgnoreCase))
            {
                var members = group.ToList();
                if (members.Count < 2)
                {
                    continue;
                }

                var victims = members.Where(mod => !mod.IsBase).ToList();
                if (members.Any(mod => mod.IsBase))
                {
                    Error(ModProblemKind.IdConflict, victims, null, $"{string.Join(", ", victims.Select(mod => $"`{mod.Id}` at `{mod.Directory}`"))} uses the base game's id `{modBase.Id}`");
                }
                else
                {
                    Error(ModProblemKind.IdConflict, victims, null, $"Mods {string.Join(", ", victims.Select(mod => $"`{mod.Id}`"))} have ids differing only in capitalization; ids must be unique regardless of case");
                }
                Disable(victims);
            }

            // Disabled mods stay findable, so their dependents hear "disabled" rather than "missing".
            lookup = visible.Where(mod => mod.IsBase || !visible.Any(other => other.IsBase && string.Equals(other.Id, mod.Id, StringComparison.OrdinalIgnoreCase))).ToDictionary(mod => mod.Id, StringComparer.Ordinal);
            lookup[modBase.Id] = modBase;
            lookupCaseless = visible.GroupBy(mod => mod.Id, StringComparer.OrdinalIgnoreCase).ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
        }

        private void CheckIntrinsic()
        {
            var failed = new List<Mod>();

            foreach (var mod in Enabled())
            {
                bool failure = false;

                foreach (var group in Entries(mod).GroupBy(dependency => dependency.Id, StringComparer.Ordinal))
                {
                    var first = group.First();
                    var required = group.FirstOrDefault(dependency => dependency.Kind == ModDependencyKind.Required);
                    var target = Target(first);

                    if (target == null)
                    {
                        if (lookupCaseless.TryGetValue(first.Id, out var caseless))
                        {
                            Error(ModProblemKind.DependencyIdCase, new List<Mod>() { mod }, first, $"`{mod.Id}` refers to `{first.Id}`, but the installed mod is `{caseless[0].Id}`; did you mean `{caseless[0].Id}`?");
                            failure = true;
                        }
                        else if (required != null)
                        {
                            Error(ModProblemKind.DependencyMissing, new List<Mod>() { mod }, required, $"`{mod.Id}` requires `{required.Id}`{Range(required)}, which isn't installed{Explanation(required)}");
                            failure = true;
                        }
                    }
                    else if (required != null && target.Version != null && !ModValidation.IsSatisfied(required.Version, target.Version))
                    {
                        Error(ModProblemKind.DependencyVersion, new List<Mod>() { mod }, required, $"`{mod.Id}` requires `{required.Id}`{Range(required)}, but version {target.Version} is installed{Explanation(required)}");
                        failure = true;
                    }
                }

                // A mod's own Dec would load as a second, separate Dec whose types and entry points the game's Dec never sees, and a reference to one finds nothing to bind to; either way the mod was built against the Dec package instead of the game.
                var decUses = new List<string>();
                if (decBuiltInto != null)
                {
                    decUses.AddRange(mod.assemblies.Where(assembly => IsDec(assembly.name)).Select(assembly => $"ships `{System.IO.Path.GetFileName(assembly.path)}`"));
                    decUses.AddRange(mod.assemblies.Where(assembly => assembly.references.Any(IsDec)).Select(assembly => $"`{assembly.name.Name}` references `{AssemblyDec}`"));
                }
                if (decUses.Count > 0)
                {
                    Error(ModProblemKind.AssemblyDecSeparate, new List<Mod>() { mod }, null, $"`{mod.Id}` uses its own Dec ({string.Join(", ", decUses)}), but the game builds Dec into its own assembly `{decBuiltInto}`; the mod's author should remove the Dec package from the mod entirely, not just stop copying it, and build against `{decBuiltInto}` in its place, referencing that without copying it: {ReferenceWithoutCopying}");
                    failure = true;
                }

                var skipped = new List<string>();
                foreach (var assembly in mod.assemblies)
                {
                    // Reported above, and the game's copy of a real `dec` couldn't stand in for it either, since that isn't the Dec the game uses.
                    if (decBuiltInto != null && IsDec(assembly.name))
                    {
                        continue;
                    }

                    if (!assembliesReserved.TryGetValue(assembly.name.Name, out var reserved) || (reserved.location != null && ModPath.Equal(reserved.location, assembly.path)))
                    {
                        continue;
                    }

                    var location = reserved.location;
                    if (location != null && ModResolution.AssembliesLoaded.Contains(System.IO.Path.GetFullPath(location)))
                    {
                        Error(ModProblemKind.AssemblyConflict, new List<Mod>() { mod }, null, $"`{mod.Id}` ships assembly `{assembly.name.Name}`, but a copy was already loaded from `{location}` earlier in this session, and only one can load per session; restart the game to change which one does");
                        failure = true;
                        continue;
                    }

                    var required = AssemblyRequired(assembly);
                    var reason = AssemblyStandInFailure(assembly, reserved.name, required);
                    if (reason == null)
                    {
                        assembliesSkipped.Add(assembly);
                        skipped.Add($"`{assembly.name.Name}` (mods need {required}; the game's is {reserved.name.Version})");
                        continue;
                    }

                    var game = location == null ? $"the game already has or depends on an assembly named `{assembly.name.Name}`" : $"the game already has an assembly named `{assembly.name.Name}` at `{location}`";
                    Error(ModProblemKind.AssemblyConflict, new List<Mod>() { mod }, null, $"`{mod.Id}` ships assembly `{assembly.name.Name}`, but {game}, and {reason}");
                    failure = true;
                }

                if (skipped.Count > 0 && !failure)
                {
                    problems.Add(new ModProblem(ModProblemSeverity.Warning, ModProblemKind.AssemblyRedundant, new List<Mod>() { mod }, null, $"`{mod.Id}` ships copies of assemblies the game already has, so it uses the game's copies instead: {string.Join(", ", skipped)}; the mod's author should reference them without copying them: {ReferenceWithoutCopying}"));
                }

                if (failure)
                {
                    failed.Add(mod);
                }
            }

            Disable(failed);
        }

        // Every enabled mod's references to an assembly's name, other than from the assembly itself; whichever copy binds serves them all.
        private List<System.Reflection.AssemblyName> AssemblyReferences(ModAssembly assembly)
        {
            return Enabled()
                .SelectMany(mod => mod.assemblies)
                .Where(other => other != assembly)
                .SelectMany(other => other.references)
                .Where(reference => string.Equals(reference.Name, assembly.name.Name, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // The version a copy standing in for a mod's assembly needs: at least the copy's own, and whatever the enabled mods reference.
        private System.Version AssemblyRequired(ModAssembly assembly)
        {
            return AssemblyReferences(assembly).Select(reference => reference.Version).Append(assembly.name.Version).Where(version => version != null).Max();
        }

        // Why the game's copy of an assembly can't stand in for a mod's copy, or null if it can.
        private string AssemblyStandInFailure(ModAssembly assembly, System.Reflection.AssemblyName game, System.Version required)
        {
            var references = AssemblyReferences(assembly);

            // A copy nothing uses may be the mod's own assembly that happens to share the game's name, and swapping in the game's would break it.
            if (references.Count == 0)
            {
                return "nothing in any enabled mod references it, so it may be a different assembly that happens to share the name; the mod's author should drop it if it's a copy, or rename it if it isn't";
            }

            if (game == null || game.Version == null)
            {
                return $"the game's version of it is unknown, so it can't be checked; the mod's author should reference it without copying it: {ReferenceWithoutCopying}";
            }

            var token = TokenString(game);
            if (references.Append(assembly.name).Any(name => TokenString(name) != token))
            {
                return $"it's signed differently from the game's, so the game's copy can't stand in for it; the mod's author should reference the game's copy without copying it: {ReferenceWithoutCopying}";
            }

            if (required == null || required.CompareTo(game.Version) > 0)
            {
                return $"mods need version {required}, newer than the game's {game.Version}; the mod's author should reference a version no newer than the game's, without copying it: {ReferenceWithoutCopying}";
            }

            return null;
        }

        private static bool IsDec(System.Reflection.AssemblyName name)
        {
            return string.Equals(name.Name, AssemblyDec, StringComparison.OrdinalIgnoreCase);
        }

        private static string TokenString(System.Reflection.AssemblyName name)
        {
            return Convert.ToHexString(name.GetPublicKeyToken() ?? Array.Empty<byte>());
        }

        private void Cascade()
        {
            bool changed = true;
            while (changed)
            {
                changed = false;

                foreach (var mod in Enabled().ToList())
                {
                    foreach (var required in Entries(mod).Where(dependency => dependency.Kind == ModDependencyKind.Required))
                    {
                        var target = Target(required);
                        if (target != null && status[target] != ModStatus.Enabled)
                        {
                            Error(ModProblemKind.DependencyDisabled, new List<Mod>() { mod }, required, $"`{mod.Id}` requires `{target.Id}`, which is disabled");
                            status[mod] = ModStatus.Disabled;
                            changed = true;
                            break;
                        }
                    }
                }
            }
        }

        private void CheckIncompatible()
        {
            var hits = new List<(Mod mod, ModDependency dependency, Mod target)>();

            foreach (var mod in Enabled())
            {
                foreach (var dependency in Entries(mod).Where(dependency => dependency.Kind == ModDependencyKind.Incompatible))
                {
                    var target = Target(dependency);
                    if (Applies(dependency, target) && status[target] == ModStatus.Enabled)
                    {
                        hits.Add((mod, dependency, target));
                    }
                }
            }

            // Evaluated all at once, so a mutually incompatible pair disables both rather than whichever happened to be checked first.
            foreach (var (mod, dependency, target) in hits)
            {
                Error(ModProblemKind.Incompatible, new List<Mod>() { mod }, dependency, $"`{mod.Id}` is incompatible with `{target.Id}`{Range(dependency)}, which is enabled{Explanation(dependency)}");
            }
            Disable(hits.Select(hit => hit.mod));

            Cascade();
        }

        private void CheckAssemblyClashes()
        {
            var owners = new SortedDictionary<string, List<Mod>>(StringComparer.OrdinalIgnoreCase);
            foreach (var mod in Enabled())
            {
                foreach (var assembly in mod.assemblies.Where(assembly => !assembliesSkipped.Contains(assembly)))
                {
                    if (!owners.TryGetValue(assembly.name.Name, out var list))
                    {
                        list = new List<Mod>();
                        owners[assembly.name.Name] = list;
                    }
                    list.Add(mod);
                }
            }

            var victims = new List<Mod>();
            foreach (var (name, mods) in owners)
            {
                if (mods.Count < 2)
                {
                    continue;
                }

                var distinct = mods.Distinct().ToList();
                if (distinct.Count == 1)
                {
                    Error(ModProblemKind.AssemblyConflict, distinct, null, $"`{distinct[0].Id}` ships more than one assembly named `{name}`");
                }
                else
                {
                    Error(ModProblemKind.AssemblyConflict, distinct, null, $"Mods {string.Join(", ", distinct.Select(mod => $"`{mod.Id}`"))} each ship an assembly named `{name}`; only one copy can load, so it belongs in its own mod that the others depend on");
                }
                victims.AddRange(distinct);
            }
            Disable(victims);

            Cascade();
        }

        private List<Edge> Edges()
        {
            var edges = new List<Edge>();
            var seen = new Dictionary<(Mod, Mod), int>();

            void Add(Mod before, Mod after, Mod declarer, ModDependency entry)
            {
                if (edgesDropped.Contains((before, after)))
                {
                    return;
                }

                var edge = new Edge() { before = before, after = after, declarer = declarer, entry = entry, preference = entry != null && !entry.HasExplicitOrder };
                if (!seen.TryGetValue((before, after), out int index))
                {
                    seen[(before, after)] = edges.Count;
                    edges.Add(edge);
                }
                else if (edges[index].preference && !edge.preference)
                {
                    // An explicit order outranks a preference for the same pair.
                    edges[index] = edge;
                }
            }

            foreach (var mod in Enabled())
            {
                // The base game always loads first, whatever a mod's own base entries say.
                Add(modBase, mod, mod, null);

                foreach (var dependency in mod.Dependencies.Where(dependency => !dependency.invalid))
                {
                    var target = Target(dependency);
                    if (!Applies(dependency, target) || status[target] != ModStatus.Enabled)
                    {
                        continue;
                    }

                    if (dependency.Order == ModDependencyOrder.After)
                    {
                        Add(target, mod, mod, dependency);
                    }
                    else if (dependency.Order == ModDependencyOrder.Before)
                    {
                        Add(mod, target, mod, dependency);
                    }
                }
            }

            return edges;
        }

        private Dictionary<Mod, List<Edge>> Successors(List<Mod> nodes)
        {
            var successors = nodes.ToDictionary(mod => mod, mod => new List<Edge>());
            foreach (var edge in Edges())
            {
                successors[edge.before].Add(edge);
            }
            foreach (var list in successors.Values)
            {
                list.Sort((a, b) => string.CompareOrdinal(a.after.Id, b.after.Id));
            }
            return successors;
        }

        private static IEnumerable<List<Mod>> Cycles(List<Mod> nodes, Dictionary<Mod, List<Edge>> successors)
        {
            return StronglyConnected(nodes, successors)
                .Where(component => component.Count > 1)
                .Select(component => component.OrderBy(mod => mod.Id, StringComparer.Ordinal).ToList())
                .OrderBy(members => members[0].Id, StringComparer.Ordinal);
        }

        private void CheckCycles()
        {
            var nodes = installed.Where(mod => status[mod] == ModStatus.Enabled).ToList();

            // First drop every preference inside a cycle. A cycle can be broken in many ways, and dropping all of them, rather than some minimal set, keeps the result independent of the order anything was found in.
            var successors = Successors(nodes);
            foreach (var members in Cycles(nodes, successors))
            {
                var inside = new HashSet<Mod>(members);
                var preferences = members.SelectMany(member => successors[member]).Where(edge => edge.preference && inside.Contains(edge.after)).ToList();
                if (preferences.Count == 0)
                {
                    continue;
                }

                foreach (var edge in preferences)
                {
                    edgesDropped.Add((edge.before, edge.after));
                }
                problems.Add(new ModProblem(ModProblemSeverity.Warning, ModProblemKind.CycleBroken, members, null, $"Default load orders among {string.Join(", ", members.Select(mod => $"`{mod.Id}`"))} form a cycle, so these are ignored: {string.Join("; ", preferences.Select(DescribeEdge))}; set `order` on these entries to choose an order"));
            }

            // Whatever is still cyclic is made of explicit orders, and can't be satisfied.
            successors = Successors(nodes);
            var victims = new List<Mod>();
            foreach (var members in Cycles(nodes, successors))
            {
                Error(ModProblemKind.Cycle, members, null, $"Load order cycle among {string.Join(", ", members.Select(mod => $"`{mod.Id}`"))}: {DescribeCycle(members, successors)}");
                victims.AddRange(members);
            }
            Disable(victims);

            Cascade();
        }

        // Tarjan's algorithm.
        private static List<List<Mod>> StronglyConnected(List<Mod> nodes, Dictionary<Mod, List<Edge>> successors)
        {
            var index = new Dictionary<Mod, int>();
            var lowlink = new Dictionary<Mod, int>();
            var stack = new Stack<Mod>();
            var onStack = new HashSet<Mod>();
            var components = new List<List<Mod>>();

            void Visit(Mod node)
            {
                index[node] = index.Count;
                lowlink[node] = index[node];
                stack.Push(node);
                onStack.Add(node);

                foreach (var edge in successors[node])
                {
                    if (!index.ContainsKey(edge.after))
                    {
                        Visit(edge.after);
                        lowlink[node] = Math.Min(lowlink[node], lowlink[edge.after]);
                    }
                    else if (onStack.Contains(edge.after))
                    {
                        lowlink[node] = Math.Min(lowlink[node], index[edge.after]);
                    }
                }

                if (lowlink[node] == index[node])
                {
                    var component = new List<Mod>();
                    Mod member;
                    do
                    {
                        member = stack.Pop();
                        onStack.Remove(member);
                        component.Add(member);
                    }
                    while (member != node);
                    components.Add(component);
                }
            }

            foreach (var node in nodes)
            {
                if (!index.ContainsKey(node))
                {
                    Visit(node);
                }
            }

            return components;
        }

        // A shortest cycle through the first member, found by breadth-first search inside the component.
        private static string DescribeCycle(List<Mod> members, Dictionary<Mod, List<Edge>> successors)
        {
            var start = members[0];
            var inside = new HashSet<Mod>(members);
            var arrival = new Dictionary<Mod, Edge>();
            var queue = new Queue<Mod>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                foreach (var edge in successors[node])
                {
                    if (!inside.Contains(edge.after))
                    {
                        continue;
                    }

                    if (edge.after == start)
                    {
                        var path = new List<Edge>() { edge };
                        for (var current = node; current != start; current = arrival[current].before)
                        {
                            path.Add(arrival[current]);
                        }
                        path.Reverse();
                        return string.Join("; ", path.Select(DescribeEdge));
                    }

                    if (!arrival.ContainsKey(edge.after))
                    {
                        arrival[edge.after] = edge;
                        queue.Enqueue(edge.after);
                    }
                }
            }

            // Every member of a strongly connected component lies on a cycle through every other member.
            Dbg.Err("Internal error: no cycle found through a strongly connected component");
            return "";
        }

        private static string DescribeEdge(Edge edge)
        {
            if (edge.entry == null)
            {
                return $"`{edge.after.Id}` loads after the base game `{edge.before.Id}`";
            }

            string order = edge.entry.Order == ModDependencyOrder.Before ? ", Before" : "";
            return $"`{edge.after.Id}` loads after `{edge.before.Id}` (`{edge.declarer.Id}`: {edge.entry.Kind}{order})";
        }

        private List<Mod> Order()
        {
            var nodes = installed.Where(mod => status[mod] == ModStatus.Enabled).ToList();
            var indegree = nodes.ToDictionary(mod => mod, mod => 0);
            var successors = nodes.ToDictionary(mod => mod, mod => new List<Mod>());
            foreach (var edge in Edges())
            {
                successors[edge.before].Add(edge.after);
                indegree[edge.after]++;
            }

            var ready = new PriorityQueue<Mod, string>(StringComparer.Ordinal);
            foreach (var node in nodes.Where(node => indegree[node] == 0))
            {
                ready.Enqueue(node, node.Id);
            }

            var order = new List<Mod>();
            while (ready.TryDequeue(out var node, out _))
            {
                order.Add(node);
                foreach (var successor in successors[node])
                {
                    if (--indegree[successor] == 0)
                    {
                        ready.Enqueue(successor, successor.Id);
                    }
                }
            }

            if (order.Count != nodes.Count)
            {
                // Cycle detection ran on a superset of these nodes, and removing nodes can't create a cycle.
                Dbg.Err("Internal error: load order still has a cycle after cycle removal");
            }

            return order;
        }

        private void CheckSoft()
        {
            foreach (var mod in Enabled())
            {
                foreach (var group in mod.Dependencies.Where(dependency => !dependency.invalid).GroupBy(dependency => dependency.Id, StringComparer.Ordinal))
                {
                    var target = Target(group.First());
                    var applying = group.FirstOrDefault(dependency => Applies(dependency, target));
                    bool targetEnabled = target != null && status[target] == ModStatus.Enabled;

                    if (applying != null && applying.Kind == ModDependencyKind.Discouraged && targetEnabled)
                    {
                        problems.Add(new ModProblem(ModProblemSeverity.Warning, ModProblemKind.Discouraged, new List<Mod>() { mod, target }, applying, $"`{mod.Id}` discourages using `{target.Id}`{Range(applying)} alongside it{Explanation(applying)}"));
                    }

                    var optional = group.Where(dependency => dependency.Kind == ModDependencyKind.Recommended || dependency.Kind == ModDependencyKind.Supported).ToList();
                    if (optional.Count > 0 && !(targetEnabled && optional.Contains(applying)))
                    {
                        var first = optional[0];
                        string verb = first.Kind == ModDependencyKind.Recommended ? "recommends" : "supports";
                        string state = target == null ? "isn't installed" : !targetEnabled ? "is disabled" : $"is installed as version {target.Version}";
                        problems.Add(new ModProblem(ModProblemSeverity.Info, ModProblemKind.DependencyOptional, new List<Mod>() { mod }, first, $"`{mod.Id}` {verb} `{first.Id}`{Range(first)}, which {state}{Explanation(first)}"));
                    }
                }
            }
        }
    }
}
