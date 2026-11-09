using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Dec.Modding
{
    /// <summary>
    /// The outcome of ModLoader.Resolve: which mods load, in what order, and every problem found along the way.
    /// </summary>
    /// <remarks>
    /// Its problems haven't been reported or anything loaded yet. A game with a mod screen can show ModsInstalled, GetStatus, and Problems, then call Load.
    /// </remarks>
    public class ModResolution
    {
        // Assemblies can't be unloaded, so this tracks every mod assembly this process has ever loaded.
        internal static readonly HashSet<string> AssembliesLoaded = new HashSet<string>(ModPath.Comparer);

        // Entry points run once per process, like the assembly loads they follow; running a Harmony patch twice would apply it twice.
        private static readonly HashSet<Type> EntrypointsRun = new HashSet<Type>();

        private readonly List<Mod> installed;
        private readonly Dictionary<Mod, ModStatus> status;
        private readonly List<Mod> mods;
        private readonly List<ModProblem> problems;
        private readonly HashSet<ModAssembly> assembliesSkipped;
        private bool loaded;

        internal ModResolution(List<Mod> installed, Dictionary<Mod, ModStatus> status, List<Mod> mods, List<ModProblem> problems, HashSet<ModAssembly> assembliesSkipped)
        {
            this.installed = installed;
            this.status = status;
            this.mods = mods;
            this.problems = problems;
            this.assembliesSkipped = assembliesSkipped;
        }

        /// <summary>
        /// Every mod that will load, in load order, starting with the base game.
        /// </summary>
        public IReadOnlyList<Mod> Mods
        {
            get
            {
                return mods;
            }
        }

        /// <summary>
        /// The base game and every mod found, including disabled and hidden ones, in a stable order.
        /// </summary>
        public IReadOnlyList<Mod> ModsInstalled
        {
            get
            {
                return installed;
            }
        }

        /// <summary>
        /// Every problem found while finding, reading, and resolving mods.
        /// </summary>
        /// <remarks>
        /// Problems that come up later, while Load is loading assemblies or running entry points, are reported directly and not listed here.
        /// </remarks>
        public IReadOnlyList<ModProblem> Problems
        {
            get
            {
                return problems;
            }
        }

        public ModStatus GetStatus(Mod mod)
        {
            return status[mod];
        }

        // A mod's assemblies, less any copies of the game's that it uses the game's copy of instead.
        internal IEnumerable<ModAssembly> AssembliesLoading(Mod mod)
        {
            return mod.assemblies.Where(assembly => !assembliesSkipped.Contains(assembly));
        }

        internal static void ClearAssembliesLoadedForTest()
        {
            AssembliesLoaded.Clear();
            EntrypointsRun.Clear();
        }

        internal void Report()
        {
            foreach (var problem in problems)
            {
                switch (problem.Severity)
                {
                    case ModProblemSeverity.Info:
                        Dbg.Inf(problem.Message);
                        break;
                    case ModProblemSeverity.Warning:
                        Dbg.Wrn(problem.Message);
                        break;
                    case ModProblemSeverity.Error:
                        Dbg.Err(problem.Message);
                        break;
                }
            }
        }

        /// <summary>
        /// Reports every problem, loads the enabled mods' assemblies other than copies of the game's, runs their entry points, and builds a ParserModular with one module per mod in load order.
        /// </summary>
        /// <remarks>
        /// The returned parser hasn't been finished; the game may add more modules, which load after every mod, then call Finish.
        ///
        /// Problems are reported through Dec's usual handlers, which by default throw on warnings as well as errors; a game that ships with mod support should configure them.
        ///
        /// Assemblies can't be unloaded. Loading again in the same process (after Database.Clear) reuses assemblies already loaded from the same path and doesn't run their entry points again, but a mod whose code was loaded earlier and isn't enabled now keeps running, and is reported as an error; changing which code mods are enabled needs a restart.
        ///
        /// Like Parser, this must run on a single thread with no other Dec activity.
        /// </remarks>
        public ParserModular Load(Recorder.IUserSettings userSettings = null)
        {
            if (loaded)
            {
                throw new InvalidOperationException("ModResolution.Load was called more than once; resolve again to load another set of mods");
            }
            loaded = true;

            Report();

            var pathsEnabled = new HashSet<string>(mods.SelectMany(AssembliesLoading).Select(assembly => System.IO.Path.GetFullPath(assembly.path)), ModPath.Comparer);
            foreach (var path in AssembliesLoaded.Where(path => !pathsEnabled.Contains(path)).OrderBy(path => path, StringComparer.Ordinal))
            {
                Dbg.Err($"Assembly `{path}` was loaded by an earlier ModResolution.Load, but its mod isn't enabled now; its code keeps running until the game restarts");
            }

            var assembliesByMod = LoadAssemblies();

            // Entry points and the parse both need the mods' own converters and types.
            global::Dec.Config.TypeCachesClear();

            var parser = new ParserModular(userSettings);

            foreach (var mod in mods)
            {
                RunEntrypoint(mod, assembliesByMod[mod]);
            }

            foreach (var mod in mods)
            {
                var module = parser.CreateModule(mod.Id);
                if (System.IO.Directory.Exists(mod.directoryDecs))
                {
                    module.AddDirectory(mod.directoryDecs);
                }
                else if (mod.IsBase)
                {
                    Dbg.Err($"Base game content directory `{mod.directoryDecs}` doesn't exist");
                }
            }

            return parser;
        }

        private Dictionary<Mod, List<Assembly>> LoadAssemblies()
        {
            // Load into whichever context holds Dec, so mods bind to the game's own copies of Dec and the game.
            var context = AssemblyLoadContext.GetLoadContext(typeof(global::Dec.Dec).Assembly);

            var result = new Dictionary<Mod, List<Assembly>>();
            foreach (var mod in mods)
            {
                var loadedForMod = new List<Assembly>();
                result[mod] = loadedForMod;

                foreach (var assembly in AssembliesLoading(mod))
                {
                    string path = System.IO.Path.GetFullPath(assembly.path);

                    var existing = context.Assemblies.FirstOrDefault(candidate => !candidate.IsDynamic && candidate.Location.Length > 0 && ModPath.Comparer.Equals(candidate.Location, path));
                    if (existing != null)
                    {
                        loadedForMod.Add(existing);
                        continue;
                    }

                    try
                    {
                        loadedForMod.Add(context.LoadFromAssemblyPath(path));
                        AssembliesLoaded.Add(path);
                    }
                    catch (Exception e) when (e is FileLoadException || e is BadImageFormatException || e is FileNotFoundException)
                    {
                        Dbg.Err($"Failed to load assembly `{path}` for mod `{mod.Id}`: {e.Message}");
                    }
                }
            }
            return result;
        }

        private void RunEntrypoint(Mod mod, List<Assembly> assemblies)
        {
            var entries = new List<Type>();
            foreach (var assembly in assemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    foreach (var loaderException in e.LoaderExceptions.Where(loaderException => loaderException != null))
                    {
                        Dbg.Err($"Failed to load a type from `{assembly.Location}` for mod `{mod.Id}`: {loaderException.Message}");
                    }
                    types = e.Types.Where(type => type != null).ToArray();
                }

                entries.AddRange(types.Where(type => type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters && typeof(IModEntrypoint).IsAssignableFrom(type)));
            }

            if (entries.Count == 0)
            {
                return;
            }

            if (entries.Count > 1)
            {
                Dbg.Err($"Mod `{mod.Id}` has more than one IModEntrypoint implementation ({string.Join(", ", entries.Select(type => type.FullName).OrderBy(name => name, StringComparer.Ordinal))}); none of them will run");
                return;
            }

            if (!EntrypointsRun.Add(entries[0]))
            {
                return;
            }

            try
            {
                var entry = (IModEntrypoint)Activator.CreateInstance(entries[0]);
                entry.Initialize(new ModEntrypointInfo(mod, mods));
            }
            catch (Exception e)
            {
                Dbg.Ex(new Exception($"Exception thrown while creating or initializing the IModEntrypoint of {mod.Id}", e));
            }
        }
    }
}
