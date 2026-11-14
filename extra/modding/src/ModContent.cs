using System;
using System.Collections.Generic;
using System.Linq;

namespace Dec.Modding
{
    // The names every mod folder shares, from ModLoader's properties, as `/`-separated paths within a mod's listing.
    internal class ModLayout
    {
        internal readonly string manifest;
        internal readonly string icon;
        internal readonly string decs;
        internal readonly string assemblies;

        internal ModLayout(string manifest, string icon, string decs, string assemblies)
        {
            this.manifest = manifest;
            this.icon = icon;
            this.decs = decs;
            this.assemblies = assemblies;
        }
    }

    // Checks of a mod's content, made over its listing rather than the disk.
    internal static class ModContent
    {
        internal static void CheckContent(Mod mod, ModListing listing, ModLayout layout, List<ModProblem> problems)
        {
            // A name that matches only by case works where the filesystem ignores case, so its author would never find out that it fails everywhere else; the same goes for the extensions Dec looks for.
            var miscased = new List<string>();
            foreach (var (expected, paths) in new[] { (layout.manifest, listing.files), (layout.decs, listing.directories), (layout.assemblies, listing.directories) })
            {
                var actual = NameCaseMismatch(paths, expected);
                if (actual != null)
                {
                    miscased.Add($"`{actual}` should be `{expected}`");
                }
            }
            miscased.AddRange(listing.FilesIn(layout.decs, nested: true, StringComparison.Ordinal).Where(path => ExtensionMiscased(path, ".xml")).Select(path => $"`{path}` should end in `.xml`"));
            miscased.AddRange(listing.FilesIn(layout.assemblies, nested: false, StringComparison.Ordinal).Where(path => ExtensionMiscased(path, ".dll")).Select(path => $"`{path}` should end in `.dll`"));
            if (miscased.Count > 0)
            {
                mod.manifestError = true;
                problems.Add(new ModProblem(ModProblemSeverity.Error, ModProblemKind.ContentCase, new List<Mod>() { mod }, null, $"Mod `{mod}` has names that match only when ignoring case, so it would work only where the filesystem ignores case: {string.Join(", ", miscased)}"));
            }

            // A miscased icon costs only the icon, which the exact lookup already leaves out everywhere, so it doesn't disable the mod.
            var icon = NameCaseMismatch(listing.files, layout.icon);
            if (icon != null)
            {
                problems.Add(new ModProblem(ModProblemSeverity.Warning, ModProblemKind.ContentCase, new List<Mod>() { mod }, null, $"Mod `{mod}` has `{icon}`, which matches `{layout.icon}` only when ignoring case, so it has no icon"));
            }

            // Content in the wrong folder otherwise loads nothing without a word. A mod with some content in the right place presumably meant the rest, so this only speaks up when nothing landed; it ignores case, which the check above already covers.
            bool decsHasXml = listing.FilesIn(layout.decs, nested: true, StringComparison.OrdinalIgnoreCase).Any(path => ModListing.HasExtension(path, ".xml", StringComparison.OrdinalIgnoreCase));
            if (!decsHasXml)
            {
                var xml = listing.files.Where(path => ModListing.HasExtension(path, ".xml", StringComparison.OrdinalIgnoreCase) && !string.Equals(path, layout.manifest, StringComparison.OrdinalIgnoreCase)).Select(listing.Full).ToList();
                if (xml.Count > 0)
                {
                    problems.Add(new ModProblem(ModProblemSeverity.Warning, ModProblemKind.ContentMisplaced, new List<Mod>() { mod }, null, $"Mod `{mod}` has XML, but none in its {layout.decs} folder, so none of it will be loaded: {string.Join(", ", xml.Select(path => $"`{path}`"))}"));
                }
            }

            bool assembliesHasDll = listing.FilesIn(layout.assemblies, nested: false, StringComparison.OrdinalIgnoreCase).Any(path => ModListing.HasExtension(path, ".dll", StringComparison.OrdinalIgnoreCase));
            if (!assembliesHasDll)
            {
                var dll = listing.files.Where(path => ModListing.HasExtension(path, ".dll", StringComparison.OrdinalIgnoreCase)).Select(listing.Full).ToList();
                if (dll.Count > 0)
                {
                    problems.Add(new ModProblem(ModProblemSeverity.Warning, ModProblemKind.ContentMisplaced, new List<Mod>() { mod }, null, $"Mod `{mod}` has assemblies, but none directly inside its {layout.assemblies} folder, so none of them will be loaded: {string.Join(", ", dll.Select(path => $"`{path}`"))}"));
                }
            }
        }

        // Names are compared within each folder, so two clashing folders are reported once rather than again for every pair of files inside them.
        internal static void CheckCollisions(Mod mod, ModListing listing, List<ModProblem> problems)
        {
            var collisions = listing.files.Concat(listing.directories)
                .GroupBy(path => path.Substring(0, Math.Max(path.LastIndexOf('/'), 0)), StringComparer.Ordinal)
                .SelectMany(folder => folder.GroupBy(path => path.Substring(path.LastIndexOf('/') + 1), StringComparer.OrdinalIgnoreCase))
                .Where(group => group.Count() > 1)
                .Select(group => string.Join(" and ", group.OrderBy(path => path, StringComparer.Ordinal).Select(path => $"`{path}`")))
                .OrderBy(collision => collision, StringComparer.Ordinal)
                .ToList();
            if (collisions.Count > 0)
            {
                mod.manifestError = true;
                problems.Add(new ModProblem(ModProblemSeverity.Error, ModProblemKind.ContentCaseCollision, new List<Mod>() { mod }, null, $"Mod `{mod}` has names that differ only by case, which can't both exist where the filesystem ignores case, such as on Windows and macOS: {string.Join("; ", collisions)}"));
            }
        }

        // The game's overridable files keyed ignoring case, each with every spelling the game uses for it.
        internal static Dictionary<string, HashSet<string>> FilesOverridable(IEnumerable<string> files)
        {
            var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in files)
            {
                if (string.IsNullOrEmpty(file) || file.Contains(':') || System.IO.Path.IsPathRooted(file))
                {
                    throw new ArgumentException($"Overridable game file `{file}` must be a path relative to the game's folder, like `dec/art/thing.png`; strip any `res://` prefix", "filesOverridable");
                }

                var path = file.Replace('\\', '/');
                if (path.Split('/').Any(segment => segment.Length == 0 || segment == "." || segment == ".."))
                {
                    throw new ArgumentException($"Overridable game file `{file}` must be written the way FileResolve matches it, like `dec/art/thing.png`, with no `.` or `..` segments, doubled separators, or trailing separator", "filesOverridable");
                }

                if (!result.TryGetValue(path, out var spellings))
                {
                    spellings = new HashSet<string>(StringComparer.Ordinal);
                    result[path] = spellings;
                }
                spellings.Add(path);
            }
            return result;
        }

        // Each entry is the shallowest part of a mod file's path that's spelled differently from the game file it matches, so a miscased folder is reported once rather than for every file in it.
        internal static void CheckOverrides(Mod mod, ModListing listing, Dictionary<string, HashSet<string>> game, List<ModProblem> problems)
        {
            var misses = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var path in listing.files)
            {
                if (!game.TryGetValue(path, out var spellings) || spellings.Contains(path))
                {
                    continue;
                }

                var ours = path.Split('/');
                var theirs = spellings.OrderBy(spelling => spelling, StringComparer.Ordinal).First().Split('/');
                int differs = Enumerable.Range(0, ours.Length).First(i => !string.Equals(ours[i], theirs[i], StringComparison.Ordinal));
                misses[string.Join('/', ours, 0, differs + 1)] = string.Join('/', theirs, 0, differs + 1);
            }

            if (misses.Count > 0)
            {
                mod.manifestError = true;
                problems.Add(new ModProblem(ModProblemSeverity.Error, ModProblemKind.ContentCaseOverride, new List<Mod>() { mod }, null, $"Mod `{mod}` has files that match the game's only when ignoring case, so they won't replace the game's on any platform: {string.Join(", ", misses.Select(miss => $"`{miss.Key}` (the game's is `{miss.Value}`)"))}"));
            }
        }

        // The path among these that matches the expected one only when ignoring case, or null if it's there exactly or not at all.
        internal static string NameCaseMismatch(IEnumerable<string> paths, string expected)
        {
            if (paths.Contains(expected, StringComparer.Ordinal))
            {
                return null;
            }
            return paths.Where(path => string.Equals(path, expected, StringComparison.OrdinalIgnoreCase)).OrderBy(path => path, StringComparer.Ordinal).FirstOrDefault();
        }

        private static bool ExtensionMiscased(string path, string extension)
        {
            return ModListing.HasExtension(path, extension, StringComparison.OrdinalIgnoreCase) && !ModListing.HasExtension(path, extension, StringComparison.Ordinal);
        }
    }
}
