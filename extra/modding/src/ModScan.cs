using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;

namespace Dec.Modding
{
    /// <summary>
    /// The mods found by ModLoader.Scan, ready to resolve into a load order.
    /// </summary>
    /// <remarks>
    /// Resolving rereads no manifests and reports nothing, so a mod screen can resolve one scan again each time the player turns a mod on or off, without repeating the manifests' diagnostics. It does look again at which assemblies the game has, since a Load since the scan may have changed that.
    /// </remarks>
    public class ModScan
    {
        private readonly Mod modBase;
        private readonly List<Mod> candidates;
        private readonly List<ModProblem> problems;
        private readonly string decBuiltInto;

        internal ModScan(Mod modBase, List<Mod> candidates, List<ModProblem> problems, string decBuiltInto)
        {
            this.modBase = modBase;
            this.candidates = candidates;
            this.problems = problems;
            this.decBuiltInto = decBuiltInto;
        }

        /// <summary>
        /// Resolves the scanned mods into a load order.
        /// </summary>
        /// <param name="idsOff">Ids of mods the player turned off; null turns nothing off. See ModStatus.Off.</param>
        public ModResolution Resolve(IEnumerable<string> idsOff = null)
        {
            var off = new HashSet<string>(idsOff ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
            if (off.Contains(modBase.Id))
            {
                throw new ArgumentException($"The base game `{modBase.Id}` can't be turned off", nameof(idsOff));
            }

            var names = new HashSet<string>(candidates.SelectMany(mod => mod.assemblies).Select(assembly => assembly.name.Name), StringComparer.OrdinalIgnoreCase);
            var reserved = ModLoader.AssembliesReserved(AssemblyLoadContext.GetLoadContext(typeof(global::Dec.Dec).Assembly), names);
            return ModResolver.Resolve(modBase, candidates, new List<ModProblem>(problems), reserved, decBuiltInto, off);
        }
    }
}
