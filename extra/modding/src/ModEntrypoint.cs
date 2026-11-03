using System.Collections.Generic;

namespace Dec.Modding
{
    /// <summary>
    /// Optional entry point for a mod's code.
    /// </summary>
    /// <remarks>
    /// A mod may contain at most one class implementing this, with a no-argument constructor. It's created and called once per process, in load order, after every enabled mod's assemblies are loaded and before any dec is parsed. This is the place for work that has to happen before parsing, such as Harmony patches.
    ///
    /// Work that needs parsed decs belongs in a [Dec.Setup] function instead.
    /// </remarks>
    public interface IModEntrypoint
    {
        void Initialize(ModEntrypointInfo info);
    }

    /// <summary>
    /// What a mod's entry point is told about the load it's part of.
    /// </summary>
    public class ModEntrypointInfo
    {
        private readonly Mod mod;
        private readonly IReadOnlyList<Mod> modsEnabled;

        internal ModEntrypointInfo(Mod mod, IReadOnlyList<Mod> modsEnabled)
        {
            this.mod = mod;
            this.modsEnabled = modsEnabled;
        }

        /// <summary>
        /// The mod this entry point belongs to.
        /// </summary>
        public Mod Mod
        {
            get
            {
                return mod;
            }
        }

        /// <summary>
        /// Every mod that will load, in load order, starting with the base game; useful for checking whether a Supported mod is present.
        /// </summary>
        public IReadOnlyList<Mod> ModsEnabled
        {
            get
            {
                return modsEnabled;
            }
        }
    }
}
