using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;

namespace Dec.Modding
{
    /// <summary>
    /// Finds installed mods, ready to resolve into a load order.
    /// </summary>
    /// <remarks>
    /// Each mod is a folder containing a manifest (mod.xml), with its dec XML in a dec folder, its assemblies in an assembly folder, and optionally an icon.png; the rest of it mirrors the game's own folder, for FileResolve. The file and folder names can be changed, for every mod at once, before calling Scan.
    /// </remarks>
    public class ModLoader
    {
        private readonly string baseId;
        private readonly SemanticVersioning.Version baseVersion;

        /// <summary>
        /// The manifest's file name within each mod folder.
        /// </summary>
        public string FileManifest { get; set; } = "mod.xml";

        /// <summary>
        /// The icon's file name within each mod folder.
        /// </summary>
        public string FileIcon { get; set; } = "icon.png";

        /// <summary>
        /// The folder within each mod folder that holds its dec XML.
        /// </summary>
        public string DirectoryDecs { get; set; } = "dec";

        /// <summary>
        /// The folder within each mod folder that holds its assemblies.
        /// </summary>
        public string DirectoryAssemblies { get; set; } = "assembly";

        /// <param name="baseId">The base game's id. Mods name it to depend on a particular version of the game.</param>
        /// <param name="baseVersion">The base game's version, in semantic-versioning form; this is what mods' version ranges for the base game are checked against. It can be the game's own version, or a separate modding-API version that changes only when mods need to.</param>
        public ModLoader(string baseId, string baseVersion)
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
        }

        /// <summary>
        /// Reads every mod found in the given directories, ready to resolve into a load order.
        /// </summary>
        /// <remarks>
        /// Each folder directly inside one of the directories is a mod, except folders starting with a dot. Problems with mods and their relationships are returned in each resolution of the scan, and reported by its Load. Dec's own diagnostics while reading a manifest, such as malformed XML or unknown elements, are reported through Dec's usual handlers as they come up, and with Dec's default handlers they throw.
        ///
        /// Assemblies are read for their names and references, not loaded.
        /// </remarks>
        public ModScan Scan(IEnumerable<string> directories)
        {
            var layout = Layout();
            var problems = new List<ModProblem>();
            var candidates = new List<Mod>();

            foreach (var directory in directories.Select(directory => System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(directory))).Distinct(ModPath.Comparer))
            {
                if (!System.IO.Directory.Exists(directory))
                {
                    problems.Add(new ModProblem(ModProblemSeverity.Info, ModProblemKind.DirectoryMissing, new List<Mod>(), null, $"Mod directory `{directory}` doesn't exist"));
                    continue;
                }

                string[] folders;
                try
                {
                    folders = System.IO.Directory.GetDirectories(directory);
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    problems.Add(UnreadableProblem(directory, e));
                    continue;
                }

                foreach (var folder in folders.Where(folder => !System.IO.Path.GetFileName(folder).StartsWith('.')).OrderBy(folder => folder, StringComparer.Ordinal))
                {
                    // A folder that can't be read, say mid-extraction or without permission, is skipped rather than failing the whole scan, along with anything it had reported so far about a mod that won't be listed.
                    int problemsBefore = problems.Count;
                    try
                    {
                        // A miscased manifest is read anyway, so the mod is listed and then disabled by the content check, the same on every filesystem.
                        var manifestFound = ModListing.FindFile(folder, layout.manifest);
                        if (manifestFound == null)
                        {
                            problems.Add(new ModProblem(ModProblemSeverity.Info, ModProblemKind.ManifestMissing, new List<Mod>(), null, $"Folder `{folder}` has no {FileManifest}, so it isn't a mod; if it holds a mod, it may be nested one folder too deep"));
                            continue;
                        }

                        var listing = ModListing.Read(folder);
                        var manifest = listing.Full(manifestFound);
                        var mod = ModManifest.Read(File.ReadAllText(manifest), manifest, folder, baseId, problems);
                        mod.directoryDecs = listing.Full(layout.decs);

                        if (listing.HasFile(layout.icon))
                        {
                            mod.fileIcon = listing.Full(layout.icon);
                        }

                        ScanAssemblies(mod, listing, layout.assemblies, problems);
                        ModContent.CheckContent(mod, listing, layout, problems);
                        ModContent.CheckCollisions(mod, listing, problems);
                        IndexFiles(mod, listing);

                        candidates.Add(mod);
                    }
                    catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                    {
                        problems.RemoveRange(problemsBefore, problems.Count - problemsBefore);
                        problems.Add(UnreadableProblem(folder, e));
                    }
                }
            }

            var decName = typeof(global::Dec.Dec).Assembly.GetName().Name;
            var decBuiltInto = string.Equals(decName, ModResolver.AssemblyDec, StringComparison.OrdinalIgnoreCase) ? null : decName;
            return new ModScan(Mod.CreateBase(baseId, baseVersion), candidates, problems, decBuiltInto);
        }

        // The layout names as paths within a mod's listing, checked here since they're the game's own settings rather than anything a mod can get wrong.
        internal ModLayout Layout()
        {
            return new ModLayout(LayoutName(FileManifest, nameof(FileManifest)), LayoutName(FileIcon, nameof(FileIcon)), LayoutName(DirectoryDecs, nameof(DirectoryDecs)), LayoutName(DirectoryAssemblies, nameof(DirectoryAssemblies)));
        }

        private static string LayoutName(string name, string property)
        {
            var usage = $"{property} `{name}` must be a path within each mod folder, like `dec` or `data/dec`";
            if (string.IsNullOrEmpty(name) || name.Contains(':') || System.IO.Path.IsPathRooted(name) || name.StartsWith('/') || name.StartsWith('\\'))
            {
                throw new ArgumentException(usage, property);
            }

            var normalized = name.Replace('\\', '/').TrimEnd('/');
            if (normalized.Split('/').Any(segment => segment.Length == 0 || segment.StartsWith('.')))
            {
                throw new ArgumentException($"{usage}, with no doubled separators, `.` or `..` segments, or names starting with a dot, which Dec ignores in mods", property);
            }
            return normalized;
        }

        private static void IndexFiles(Mod mod, ModListing listing)
        {
            foreach (var relative in listing.files)
            {
                mod.files[relative] = listing.Full(relative);
            }
        }

        private static void ScanAssemblies(Mod mod, ModListing listing, string directory, List<ModProblem> problems)
        {
            foreach (var path in listing.FilesIn(directory, nested: false, StringComparison.Ordinal).Where(path => ModListing.HasExtension(path, ".dll", StringComparison.Ordinal)).Select(listing.Full))
            {
                try
                {
                    using (var stream = File.OpenRead(path))
                    using (var reader = new PEReader(stream))
                    {
                        var metadata = reader.HasMetadata ? reader.GetMetadataReader() : null;
                        if (metadata == null || !metadata.IsAssembly)
                        {
                            problems.Add(AssemblyNativeProblem(mod, path));
                            continue;
                        }

                        var assembly = new ModAssembly() { path = path, name = metadata.GetAssemblyDefinition().GetAssemblyName() };
                        assembly.references.AddRange(metadata.AssemblyReferences.Select(handle => metadata.GetAssemblyReference(handle).GetAssemblyName()));
                        mod.assemblies.Add(assembly);
                    }
                }
                catch (BadImageFormatException)
                {
                    problems.Add(AssemblyNativeProblem(mod, path));
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    mod.manifestError = true;
                    problems.Add(new ModProblem(ModProblemSeverity.Error, ModProblemKind.AssemblyUnreadable, new List<Mod>() { mod }, null, $"`{path}` couldn't be read: {e.Message}"));
                }
            }
        }

        private static ModProblem UnreadableProblem(string path, Exception e)
        {
            return new ModProblem(ModProblemSeverity.Warning, ModProblemKind.DirectoryUnreadable, new List<Mod>(), null, $"`{path}` couldn't be read, so any mods in it are skipped: {e.Message}");
        }

        private static ModProblem AssemblyNativeProblem(Mod mod, string path)
        {
            return new ModProblem(ModProblemSeverity.Warning, ModProblemKind.AssemblyNative, new List<Mod>() { mod }, null, $"`{path}` isn't a .NET assembly, such as a native library, so it won't be loaded");
        }

        // Which of the given assembly names the game already has, with what it knows of the game's copy of each: everything the runtime would load from TPA, everything already loaded into the given context, and those assemblies' direct references, which covers dependencies the context hasn't loaded yet or hands in from another context. Dependencies of those that the context hasn't loaded yet aren't covered.
        //
        // Mod assemblies' references are left out, so a mod's missing dependency doesn't stop a later mod shipping it. Dynamic assemblies can't be bound to by name, so they don't conflict with a file.
        internal static Dictionary<string, AssemblyReserved> AssembliesReserved(AssemblyLoadContext context, ISet<string> names)
        {
            var reserved = new Dictionary<string, AssemblyReserved>(StringComparer.OrdinalIgnoreCase);

            if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trusted)
            {
                foreach (var path in trusted.Split(System.IO.Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
                {
                    var name = System.IO.Path.GetFileNameWithoutExtension(path);
                    if (names.Contains(name))
                    {
                        // A single-file app lists its bundled assemblies at paths that don't exist.
                        reserved[name] = new AssemblyReserved() { location = path, name = File.Exists(path) ? AssemblyName.GetAssemblyName(path) : null };
                    }
                }
            }

            var loaded = context.Assemblies.Where(assembly => !assembly.IsDynamic).ToList();
            foreach (var assembly in loaded)
            {
                var name = assembly.GetName();
                if (names.Contains(name.Name))
                {
                    // Assemblies loaded from memory, as Godot's editor does with the game, have no location.
                    reserved[name.Name] = new AssemblyReserved() { location = assembly.Location.Length > 0 ? assembly.Location : null, name = name };
                }
            }

            // Whatever binds to a reference has to satisfy the highest version anything asks for.
            var references = new Dictionary<string, AssemblyName>(StringComparer.OrdinalIgnoreCase);
            foreach (var assembly in loaded.Where(assembly => assembly.Location.Length == 0 || !ModResolution.AssembliesLoaded.Contains(assembly.Location)))
            {
                foreach (var reference in assembly.GetReferencedAssemblies().Where(reference => names.Contains(reference.Name)))
                {
                    if (!references.TryGetValue(reference.Name, out var existing) || reference.Version.CompareTo(existing.Version) > 0)
                    {
                        references[reference.Name] = reference;
                    }
                }
            }
            foreach (var (name, reference) in references)
            {
                reserved.TryAdd(name, new AssemblyReserved() { location = null, name = reference });
            }

            return reserved;
        }
    }

    // What's known of the game's copy of an assembly a mod also ships.
    internal struct AssemblyReserved
    {
        internal string location;   // null when no file is known
        internal AssemblyName name; // null when its version is unknown
    }
}
