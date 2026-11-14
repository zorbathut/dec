using System;
using System.Collections.Generic;
using System.Linq;

namespace Dec.Modding
{
    // The names every mod folder shares, from ModLoader's properties.
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
        // Content in the wrong folder otherwise loads nothing without a word, which is easy to do with case-sensitive folder names. A mod with some content in the right place presumably meant the rest, so this only speaks up when nothing landed.
        internal static void CheckContent(Mod mod, ModListing listing, ModLayout layout, List<ModProblem> problems)
        {
            // On a case-insensitive filesystem a miscapitalized name works, so its author would never find out that it fails everywhere else.
            foreach (var expected in new[] { layout.manifest, layout.decs, layout.assemblies, layout.icon })
            {
                var actual = NameCaseMismatch(listing.NamesTop(), expected);
                if (actual != null)
                {
                    problems.Add(new ModProblem(ModProblemSeverity.Warning, ModProblemKind.ContentCase, new List<Mod>() { mod }, null, $"Mod `{mod}` has `{actual}`, which should be named `{expected}`; names are case-sensitive on some systems, so this won't load there"));
                }
            }

            bool decsHasXml = listing.HasDirectory(layout.decs) && listing.FilesIn(layout.decs, nested: true).Any(path => ModListing.HasExtension(path, ".xml"));
            if (!decsHasXml)
            {
                var xml = listing.files.Where(path => ModListing.HasExtension(path, ".xml") && !ModPath.Comparer.Equals(path, layout.manifest)).Select(listing.Full).ToList();
                if (xml.Count > 0)
                {
                    problems.Add(new ModProblem(ModProblemSeverity.Warning, ModProblemKind.ContentMisplaced, new List<Mod>() { mod }, null, $"Mod `{mod}` has XML, but none in its {layout.decs} folder, so none of it will be loaded: {string.Join(", ", xml.Select(path => $"`{path}`"))}"));
                }
            }

            bool assembliesHasDll = listing.HasDirectory(layout.assemblies) && listing.FilesIn(layout.assemblies, nested: false).Any(path => ModListing.HasExtension(path, ".dll"));
            if (!assembliesHasDll)
            {
                var dll = listing.files.Where(path => ModListing.HasExtension(path, ".dll")).Select(listing.Full).ToList();
                if (dll.Count > 0)
                {
                    problems.Add(new ModProblem(ModProblemSeverity.Warning, ModProblemKind.ContentMisplaced, new List<Mod>() { mod }, null, $"Mod `{mod}` has assemblies, but none directly inside its {layout.assemblies} folder, so none of them will be loaded: {string.Join(", ", dll.Select(path => $"`{path}`"))}"));
                }
            }
        }

        // The name among these that matches the expected name only when ignoring case, or null.
        internal static string NameCaseMismatch(IEnumerable<string> names, string expected)
        {
            return names
                .Where(name => string.Equals(name, expected, StringComparison.OrdinalIgnoreCase) && !string.Equals(name, expected, StringComparison.Ordinal))
                .OrderBy(name => name, StringComparer.Ordinal)
                .FirstOrDefault();
        }
    }
}
