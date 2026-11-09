using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Dec.Modding
{
    /// <summary>
    /// Finds installed mods and resolves them into a load order.
    /// </summary>
    /// <remarks>
    /// Each mod is a folder containing a manifest (Mod.xml), with its dec XML in a Decs folder, its assemblies in an Assemblies folder, and optionally an icon.png. The file and folder names can be changed, for every mod at once, before calling Resolve.
    /// </remarks>
    public class ModLoader
    {
        private readonly string baseId;
        private readonly SemanticVersioning.Version baseVersion;
        private readonly string baseDirectory;

        /// <summary>
        /// The manifest's file name within each mod folder.
        /// </summary>
        public string FileManifest { get; set; } = "Mod.xml";

        /// <summary>
        /// The icon's file name within each mod folder.
        /// </summary>
        public string FileIcon { get; set; } = "icon.png";

        /// <summary>
        /// The folder within each mod folder that holds its dec XML.
        /// </summary>
        public string DirectoryDecs { get; set; } = "Decs";

        /// <summary>
        /// The folder within each mod folder that holds its assemblies.
        /// </summary>
        public string DirectoryAssemblies { get; set; } = "Assemblies";

        /// <param name="baseId">The base game's id. Mods name it to depend on a particular version of the game.</param>
        /// <param name="baseVersion">The base game's version, in semantic-versioning form; this is what mods' version ranges for the base game are checked against. It can be the game's own version, or a separate modding-API version that changes only when mods need to.</param>
        /// <param name="baseDirectory">The folder holding the base game's dec XML.</param>
        public ModLoader(string baseId, string baseVersion, string baseDirectory)
        {
            if (!ModValidation.IsIdValid(baseId))
            {
                throw new ArgumentException($"Base game id `{baseId}` is invalid; {ModValidation.IdRules}", nameof(baseId));
            }

            this.baseVersion = ModValidation.ParseVersion(baseVersion);
            if (this.baseVersion == null)
            {
                throw new ArgumentException($"Base game version `{baseVersion}` is not a valid semantic version, such as `1.2.0`", nameof(baseVersion));
            }

            this.baseId = baseId;
            this.baseDirectory = baseDirectory;
        }

        /// <summary>
        /// Reads every mod found in the given directories and resolves them into a load order.
        /// </summary>
        /// <remarks>
        /// Each folder directly inside one of the directories is a mod, except folders starting with a dot. Problems with mods and their relationships are returned in the resolution, and reported by its Load. Dec's own diagnostics while reading a manifest, such as malformed XML or unknown elements, are reported through Dec's usual handlers as they come up, and with Dec's default handlers they throw.
        /// </remarks>
        public ModResolution Resolve(IEnumerable<string> directories)
        {
            var problems = new List<ModProblem>();
            var candidates = new List<Mod>();

            foreach (var directory in directories.Select(directory => System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(directory))).Distinct(ModPath.Comparer))
            {
                if (!System.IO.Directory.Exists(directory))
                {
                    problems.Add(new ModProblem(ModProblemSeverity.Info, ModProblemKind.DirectoryMissing, new List<Mod>(), null, $"Mod directory `{directory}` doesn't exist"));
                    continue;
                }

                foreach (var folder in System.IO.Directory.GetDirectories(directory).Where(folder => !System.IO.Path.GetFileName(folder).StartsWith('.')).OrderBy(folder => folder, StringComparer.Ordinal))
                {
                    var manifest = System.IO.Path.Combine(folder, FileManifest);
                    if (!File.Exists(manifest))
                    {
                        var nearMiss = NameCaseMismatch(folder, FileManifest);
                        string hint = nearMiss != null ? $"; it has `{nearMiss}`, but names are case-sensitive" : "; if it holds a mod, it may be nested one folder too deep";
                        problems.Add(new ModProblem(ModProblemSeverity.Info, ModProblemKind.ManifestMissing, new List<Mod>(), null, $"Folder `{folder}` has no {FileManifest}, so it isn't a mod{hint}"));
                        continue;
                    }

                    var mod = ModManifest.Read(File.ReadAllText(manifest), manifest, folder, baseId, problems);
                    mod.directoryDecs = System.IO.Path.Combine(folder, DirectoryDecs);

                    var icon = System.IO.Path.Combine(folder, FileIcon);
                    if (File.Exists(icon))
                    {
                        mod.fileIcon = icon;
                    }

                    ScanAssemblies(mod, System.IO.Path.Combine(folder, DirectoryAssemblies), problems);
                    CheckContent(mod, folder, manifest, problems);

                    candidates.Add(mod);
                }
            }

            return ModResolver.Resolve(Mod.CreateBase(baseId, baseVersion, baseDirectory), candidates, problems, AssembliesReserved(AssemblyLoadContext.GetLoadContext(typeof(global::Dec.Dec).Assembly)));
        }

        // Content in the wrong folder otherwise loads nothing without a word, which is easy to do with case-sensitive folder names. A mod with some content in the right place presumably meant the rest, so this only speaks up when nothing landed.
        private void CheckContent(Mod mod, string folder, string manifest, List<ModProblem> problems)
        {
            var decs = System.IO.Path.Combine(folder, DirectoryDecs);
            var assemblies = System.IO.Path.Combine(folder, DirectoryAssemblies);

            // On a case-insensitive filesystem a miscapitalized name works, so its author would never find out that it fails everywhere else.
            foreach (var expected in new[] { FileManifest, DirectoryDecs, DirectoryAssemblies, FileIcon })
            {
                var actual = NameCaseMismatch(folder, expected);
                if (actual != null)
                {
                    problems.Add(new ModProblem(ModProblemSeverity.Warning, ModProblemKind.ContentCase, new List<Mod>() { mod }, null, $"Mod `{mod}` has `{actual}`, which should be named `{expected}`; names are case-sensitive on some systems, so this won't load there"));
                }
            }

            bool decsHasXml = System.IO.Directory.Exists(decs) && System.IO.Directory.EnumerateFiles(decs, "*.xml", SearchOption.AllDirectories).Any();
            if (!decsHasXml)
            {
                var xml = System.IO.Directory.GetFiles(folder, "*.xml", SearchOption.AllDirectories).Where(path => !ModPath.Equal(path, manifest)).OrderBy(path => path, StringComparer.Ordinal).ToList();
                if (xml.Count > 0)
                {
                    problems.Add(new ModProblem(ModProblemSeverity.Warning, ModProblemKind.ContentMisplaced, new List<Mod>() { mod }, null, $"Mod `{mod}` has XML, but none in its {DirectoryDecs} folder, so none of it will be loaded: {string.Join(", ", xml.Select(path => $"`{path}`"))}"));
                }
            }

            bool assembliesHasDll = System.IO.Directory.Exists(assemblies) && System.IO.Directory.EnumerateFiles(assemblies, "*.dll").Any();
            if (!assembliesHasDll)
            {
                var dll = System.IO.Directory.GetFiles(folder, "*.dll", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal).ToList();
                if (dll.Count > 0)
                {
                    problems.Add(new ModProblem(ModProblemSeverity.Warning, ModProblemKind.ContentMisplaced, new List<Mod>() { mod }, null, $"Mod `{mod}` has assemblies, but none directly inside its {DirectoryAssemblies} folder, so none of them will be loaded: {string.Join(", ", dll.Select(path => $"`{path}`"))}"));
                }
            }
        }

        // The name of an entry in the folder that matches the expected name only when ignoring case, or null.
        private static string NameCaseMismatch(string folder, string expected)
        {
            return System.IO.Directory.EnumerateFileSystemEntries(folder)
                .Select(path => System.IO.Path.GetFileName(path))
                .Where(name => string.Equals(name, expected, StringComparison.OrdinalIgnoreCase) && !string.Equals(name, expected, StringComparison.Ordinal))
                .OrderBy(name => name, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        private static void ScanAssemblies(Mod mod, string directory, List<ModProblem> problems)
        {
            if (!System.IO.Directory.Exists(directory))
            {
                return;
            }

            foreach (var path in System.IO.Directory.GetFiles(directory, "*.dll").OrderBy(path => path, StringComparer.Ordinal))
            {
                try
                {
                    mod.assemblies.Add(new ModAssembly() { path = path, name = AssemblyName.GetAssemblyName(path).Name });
                }
                catch (BadImageFormatException)
                {
                    problems.Add(new ModProblem(ModProblemSeverity.Warning, ModProblemKind.AssemblyNative, new List<Mod>() { mod }, null, $"`{path}` isn't a .NET assembly, such as a native library, so it won't be loaded"));
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    mod.manifestError = true;
                    problems.Add(new ModProblem(ModProblemSeverity.Error, ModProblemKind.AssemblyUnreadable, new List<Mod>() { mod }, null, $"`{path}` couldn't be read: {e.Message}"));
                }
            }
        }

        // Assembly names a mod may not ship, mapped to the file behind each, or null when no file is known: everything the runtime would load from TPA, everything already loaded into the given context, and those assemblies' direct references, which covers dependencies the context hasn't loaded yet or hands in from another context. Dependencies of those that the context hasn't loaded yet aren't covered.
        //
        // Mod assemblies' references are left out, so a mod's missing dependency doesn't stop a later mod shipping it. Dynamic assemblies can't be bound to by name, so they don't conflict with a file.
        internal static Dictionary<string, string> AssembliesReserved(AssemblyLoadContext context)
        {
            var reserved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trusted)
            {
                foreach (var path in trusted.Split(System.IO.Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
                {
                    reserved[System.IO.Path.GetFileNameWithoutExtension(path)] = path;
                }
            }

            var loaded = context.Assemblies.Where(assembly => !assembly.IsDynamic).ToList();
            foreach (var assembly in loaded)
            {
                // Assemblies loaded from memory, as Godot's editor does with the game, have no location.
                reserved[assembly.GetName().Name] = assembly.Location.Length > 0 ? assembly.Location : null;
            }

            foreach (var assembly in loaded.Where(assembly => assembly.Location.Length == 0 || !ModResolution.AssembliesLoaded.Contains(assembly.Location)))
            {
                foreach (var reference in assembly.GetReferencedAssemblies())
                {
                    reserved.TryAdd(reference.Name, null);
                }
            }

            return reserved;
        }
    }
}
