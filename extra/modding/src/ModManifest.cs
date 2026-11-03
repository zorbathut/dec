using System.Collections.Generic;

namespace Dec.Modding
{
    // Passed to Recorder.ReadSimple so the manifest classes' Record functions can report problems as data, attributed to their mod.
    internal class ModManifestSettings : Recorder.IUserSettings
    {
        internal readonly string baseId;
        internal readonly List<string> errors = new List<string>();

        internal ModManifestSettings(string baseId)
        {
            this.baseId = baseId;
        }
    }

    internal static class ModManifest
    {
        internal static Mod Read(string xml, string identifier, string directory, string baseId, List<ModProblem> problems)
        {
            var settings = new ModManifestSettings(baseId);
            var mod = Recorder.ReadSimple<Mod>(xml, "Mod", identifier, settings);

            if (mod == null)
            {
                // Dec reports why, such as malformed XML, as it reads.
                mod = new Mod() { directory = directory, manifestError = true };
                problems.Add(new ModProblem(ModProblemSeverity.Error, ModProblemKind.Manifest, new List<Mod>() { mod }, null, $"{identifier}: Manifest could not be read"));
                return mod;
            }

            mod.directory = directory;

            // A newer schema's contents weren't checked, so the schema version is the whole story.
            if (mod.schemaUnsupported)
            {
                problems.Add(new ModProblem(ModProblemSeverity.Error, ModProblemKind.SchemaUnsupported, new List<Mod>() { mod }, null, $"{identifier}: Mod `{mod}` uses manifest schemaVersion {mod.SchemaVersion}, but this mod loader supports only up to {Mod.SchemaVersionSupported}; it needs a newer mod loader"));
                return mod;
            }

            foreach (var error in settings.errors)
            {
                mod.manifestError = true;
                problems.Add(new ModProblem(ModProblemSeverity.Error, ModProblemKind.Manifest, new List<Mod>() { mod }, null, error));
            }

            return mod;
        }

        internal static void Error(Recorder recorder, string message)
        {
            Error(recorder.UserSettings, recorder.Context, message);
        }

        internal static void Error(Recorder.IUserSettings settings, Context context, string message)
        {
            if (settings is ModManifestSettings manifest)
            {
                manifest.errors.Add($"{context}: {message}");
            }
            else
            {
                // Read some other way than through ModLoader, with nowhere to collect problems.
                context.Err(message);
            }
        }
    }
}
