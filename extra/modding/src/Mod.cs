using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Dec.Modding
{
    /// <summary>
    /// One installed mod as described by its manifest, or the base game.
    /// </summary>
    /// <remarks>
    /// This is manifest data only; whether a mod is enabled is part of a ModResolution.
    /// </remarks>
    public class Mod : IRecordable
    {
        internal const int SchemaVersionSupported = 1;

        private string schemaVersion;
        private int schemaVersionParsed = 1;
        private string id;
        private string name;
        private string description;
        private string version;
        private List<ModAuthor> authors = new List<ModAuthor>();
        private List<ModUrl> urls = new List<ModUrl>();
        private List<ModDependency> dependencies = new List<ModDependency>();

        private SemanticVersioning.Version versionParsed;
        internal string directory;
        internal string fileIcon;
        internal string directoryDecs;
        internal List<ModAssembly> assemblies = new List<ModAssembly>();
        internal bool isBase;
        internal bool manifestError;
        internal bool schemaUnsupported;

        internal Mod() { }

        internal static Mod CreateBase(string id, SemanticVersioning.Version version, string directoryDecs)
        {
            return new Mod() { id = id, versionParsed = version, directoryDecs = directoryDecs, isBase = true };
        }

        /// <summary>
        /// The mod's id, or null if its manifest didn't provide a valid one.
        /// </summary>
        public string Id
        {
            get
            {
                return id;
            }
        }

        public string Name
        {
            get
            {
                return name;
            }
        }

        public string Description
        {
            get
            {
                return description;
            }
        }

        /// <summary>
        /// The mod's version, or null if its manifest didn't provide a valid one.
        /// </summary>
        public SemanticVersioning.Version Version
        {
            get
            {
                return versionParsed;
            }
        }

        public IReadOnlyList<ModAuthor> Authors
        {
            get
            {
                return authors;
            }
        }

        public IReadOnlyList<ModUrl> Urls
        {
            get
            {
                return urls;
            }
        }

        public IReadOnlyList<ModDependency> Dependencies
        {
            get
            {
                return dependencies;
            }
        }

        /// <summary>
        /// The mod's folder, or null for the base game.
        /// </summary>
        public string Directory
        {
            get
            {
                return directory;
            }
        }

        /// <summary>
        /// The path of the mod's icon, or null if it has none.
        /// </summary>
        /// <remarks>
        /// Dec doesn't read the file; its format and size are up to the game.
        /// </remarks>
        public string FileIcon
        {
            get
            {
                return fileIcon;
            }
        }

        public bool IsBase
        {
            get
            {
                return isBase;
            }
        }

        internal int SchemaVersion
        {
            get
            {
                return schemaVersionParsed;
            }
        }

        public override string ToString()
        {
            return id ?? directory ?? "(unidentified mod)";
        }

        void IRecordable.Record(Recorder recorder)
        {
            recorder.Record(ref schemaVersion, "schemaVersion");
            recorder.Record(ref id, "id");
            recorder.Record(ref name, "name");

            if (recorder.Mode == Recorder.Direction.Read && schemaVersion != null)
            {
                if (!int.TryParse(schemaVersion.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out schemaVersionParsed) || schemaVersionParsed < 1)
                {
                    ModManifest.Error(recorder, $"`schemaVersion` must be a whole number, at least 1, but is `{schemaVersion}`");
                    schemaVersionParsed = 1;
                }
            }

            // A newer schema may have reshaped anything else, so it gets no further scrutiny; the id and name are kept only so dependents can say what they're missing.
            if (recorder.Mode == Recorder.Direction.Read && schemaVersionParsed > SchemaVersionSupported)
            {
                id = id?.Trim();
                if (!ModValidation.IsIdValid(id))
                {
                    id = null;
                }
                schemaUnsupported = true;
                manifestError = true;
                return;
            }

            recorder.Record(ref description, "description");
            recorder.Record(ref version, "version");
            recorder.Record(ref authors, "authors");
            recorder.Record(ref urls, "urls");
            recorder.Record(ref dependencies, "dependencies");

            if (recorder.Mode != Recorder.Direction.Read)
            {
                return;
            }

            // Dec keeps string whitespace, but reformatted XML shouldn't change what an id or version means.
            id = id?.Trim();
            version = version?.Trim();

            // Dec accepts null="true" on lists and their elements; none of those mean anything here.
            authors = authors?.Where(author => author != null).ToList() ?? new List<ModAuthor>();
            urls = urls?.Where(url => url != null).ToList() ?? new List<ModUrl>();
            dependencies = dependencies?.Where(dependency => dependency != null).ToList() ?? new List<ModDependency>();

            if (id == null)
            {
                ModManifest.Error(recorder, "Mod has no `id`");
            }
            else if (!ModValidation.IsIdValid(id))
            {
                ModManifest.Error(recorder, $"Mod id `{id}` is invalid; {ModValidation.IdRules}");
                id = null;
            }

            if (version == null)
            {
                ModManifest.Error(recorder, "Mod has no `version`");
            }
            else
            {
                versionParsed = ModValidation.ParseVersion(version);
                if (versionParsed == null)
                {
                    ModManifest.Error(recorder, $"Mod version `{version}` is not a valid semantic version, such as `1.2.0` or `1.2.0-beta.1`");
                }
            }

            foreach (var group in dependencies.Where(dependency => !dependency.invalid).GroupBy(dependency => dependency.Id, StringComparer.Ordinal))
            {
                var entries = group.ToList();

                if (id != null && group.Key == id)
                {
                    ModManifest.Error(recorder.UserSettings, entries[0].context, $"Mod `{id}` lists itself as a dependency");
                }

                foreach (var extra in entries.Where(entry => entry.Kind == ModDependencyKind.Required).Skip(1))
                {
                    ModManifest.Error(recorder.UserSettings, extra.context, $"Mod lists `{group.Key}` as Required more than once; use one entry with a combined version range");
                }

                for (int later = 1; later < entries.Count; ++later)
                {
                    for (int earlier = 0; earlier < later; ++earlier)
                    {
                        if (ModValidation.RangesOverlap(entries[earlier].Version, entries[later].Version))
                        {
                            ModManifest.Error(recorder.UserSettings, entries[later].context, $"Dependency entries for `{group.Key}` have overlapping version ranges `{entries[earlier].Version?.ToString() ?? "any"}` and `{entries[later].Version?.ToString() ?? "any"}`; each version of a mod may match at most one entry");
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// One author of a mod. Every field is optional, but at least one must be present.
    /// </summary>
    public class ModAuthor : IRecordable
    {
        private string name;
        private string email;
        private List<ModUrl> urls = new List<ModUrl>();

        internal ModAuthor() { }

        public string Name
        {
            get
            {
                return name;
            }
        }

        public string Email
        {
            get
            {
                return email;
            }
        }

        public IReadOnlyList<ModUrl> Urls
        {
            get
            {
                return urls;
            }
        }

        void IRecordable.Record(Recorder recorder)
        {
            recorder.Record(ref name, "name");
            recorder.Record(ref email, "email");
            recorder.Record(ref urls, "urls");

            if (recorder.Mode != Recorder.Direction.Read)
            {
                return;
            }

            urls = urls?.Where(url => url != null).ToList() ?? new List<ModUrl>();

            if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(email) && urls.Count == 0)
            {
                ModManifest.Error(recorder, "Author entry has no `name`, `email`, or `urls`");
            }
        }
    }

    /// <summary>
    /// A link, with an optional label for display.
    /// </summary>
    public class ModUrl : IRecordable
    {
        private string url;
        private string label;

        internal ModUrl() { }

        public string Url
        {
            get
            {
                return url;
            }
        }

        public string Label
        {
            get
            {
                return label;
            }
        }

        void IRecordable.Record(Recorder recorder)
        {
            recorder.Record(ref url, "url");
            recorder.Record(ref label, "label");

            if (recorder.Mode != Recorder.Direction.Read)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                ModManifest.Error(recorder, "Url entry has no `url`");
            }
        }
    }

    internal struct ModAssembly
    {
        internal string path;
        internal string name;
    }
}
