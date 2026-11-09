using System.Collections.Generic;

namespace Dec.Modding
{
    public enum ModProblemSeverity
    {
        Info,
        Warning,
        Error,
    }

    /// <summary>
    /// What kind of problem a ModProblem describes, for code that wants to react to it without parsing the message.
    /// </summary>
    public enum ModProblemKind
    {
        /// <summary>
        /// A directory passed to ModLoader.Resolve doesn't exist.
        /// </summary>
        DirectoryMissing,

        /// <summary>
        /// A folder in a mod directory has no manifest, often because the mod was extracted one folder too deep.
        /// </summary>
        ManifestMissing,

        /// <summary>
        /// A manifest is malformed or has invalid contents.
        /// </summary>
        Manifest,

        /// <summary>
        /// A mod has XML but none in its dec folder, or assemblies but none in its assembly folder, so they were probably put in the wrong place.
        /// </summary>
        ContentMisplaced,

        /// <summary>
        /// A mod's file or folder matches one of the expected names only when ignoring case; it works on case-insensitive filesystems, but not on others.
        /// </summary>
        ContentCase,

        /// <summary>
        /// A manifest uses a newer schemaVersion than this mod loader understands.
        /// </summary>
        SchemaUnsupported,

        /// <summary>
        /// Several installed mods share an id; all but one are hidden.
        /// </summary>
        Duplicate,

        /// <summary>
        /// Several mods have ids differing only in case, or a mod uses the base game's id.
        /// </summary>
        IdConflict,

        /// <summary>
        /// A Required dependency isn't installed.
        /// </summary>
        DependencyMissing,

        /// <summary>
        /// A dependency names an installed mod with the wrong capitalization.
        /// </summary>
        DependencyIdCase,

        /// <summary>
        /// A Required dependency is installed, but not in the required version range.
        /// </summary>
        DependencyVersion,

        /// <summary>
        /// A Required dependency is installed, but disabled.
        /// </summary>
        DependencyDisabled,

        /// <summary>
        /// A Recommended or Supported dependency isn't available.
        /// </summary>
        DependencyOptional,

        /// <summary>
        /// A mod is incompatible with another enabled mod.
        /// </summary>
        Incompatible,

        /// <summary>
        /// Two enabled mods are discouraged together.
        /// </summary>
        Discouraged,

        /// <summary>
        /// Mods' explicit load-order constraints form a cycle.
        /// </summary>
        Cycle,

        /// <summary>
        /// Default load orders, from dependency entries that didn't set an order, formed a cycle and were ignored.
        /// </summary>
        CycleBroken,

        /// <summary>
        /// A mod ships an assembly whose name is already used by another mod, or by the game when the game's copy can't stand in for the mod's.
        /// </summary>
        AssemblyConflict,

        /// <summary>
        /// A mod ships copies of assemblies that the game already has at the same or a newer version; the mod's copies are skipped, and its code uses the game's.
        /// </summary>
        AssemblyRedundant,

        /// <summary>
        /// A file in a mod's assembly folder isn't a .NET assembly, such as a native library; it is skipped.
        /// </summary>
        AssemblyNative,

        /// <summary>
        /// A file in a mod's assembly folder couldn't be read.
        /// </summary>
        AssemblyUnreadable,
    }

    /// <summary>
    /// One problem found while resolving mods.
    /// </summary>
    /// <remarks>
    /// No mod named by an Error loads, and every disabled mod is named by at least one Error. A Warning or Info never changes which mods load; its Mods are the mods it concerns.
    /// </remarks>
    public class ModProblem
    {
        private ModProblemSeverity severity;
        private ModProblemKind kind;
        private List<Mod> mods;
        private ModDependency dependency;
        private string message;

        internal ModProblem(ModProblemSeverity severity, ModProblemKind kind, List<Mod> mods, ModDependency dependency, string message)
        {
            this.severity = severity;
            this.kind = kind;
            this.mods = mods;
            this.dependency = dependency;
            this.message = message;
        }

        public ModProblemSeverity Severity
        {
            get
            {
                return severity;
            }
        }

        public ModProblemKind Kind
        {
            get
            {
                return kind;
            }
        }

        public IReadOnlyList<Mod> Mods
        {
            get
            {
                return mods;
            }
        }

        /// <summary>
        /// The dependency entry that caused this problem, if any.
        /// </summary>
        public ModDependency Dependency
        {
            get
            {
                return dependency;
            }
        }

        /// <summary>
        /// A human-readable description of the problem.
        /// </summary>
        public string Message
        {
            get
            {
                return message;
            }
        }

        public override string ToString()
        {
            return $"{severity}: {message}";
        }
    }
}
