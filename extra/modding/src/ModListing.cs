using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dec.Modding
{
    // Every file and folder in a mod's folder, read once, by `/`-separated path relative to it as spelled on disk; dot-prefixed entries are included, for each check to skip as it sees fit.
    internal class ModListing
    {
        // Lookups ignore case where the usual filesystem does, as looking on disk would.
        private static readonly StringComparison Comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        internal readonly string root;
        internal readonly List<string> files;
        internal readonly List<string> directories;

        internal ModListing(string root, IEnumerable<string> files, IEnumerable<string> directories)
        {
            this.root = root;
            this.files = files.OrderBy(path => path, StringComparer.Ordinal).ToList();
            this.directories = directories.OrderBy(path => path, StringComparer.Ordinal).ToList();
        }

        internal static ModListing Read(string root)
        {
            var files = new List<string>();
            var directories = new List<string>();

            var pending = new Stack<string>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                var directory = pending.Pop();
                files.AddRange(System.IO.Directory.GetFiles(directory).Select(path => Relative(root, path)));
                foreach (var child in System.IO.Directory.GetDirectories(directory))
                {
                    directories.Add(Relative(root, child));
                    pending.Push(child);
                }
            }

            return new ModListing(root, files, directories);
        }

        private static string Relative(string root, string path)
        {
            return System.IO.Path.GetRelativePath(root, path).Replace('\\', '/');
        }

        internal string Full(string relative)
        {
            return System.IO.Path.Combine(root, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));
        }

        internal bool HasFile(string relative)
        {
            return files.Contains(relative, StringComparer.FromComparison(Comparison));
        }

        internal bool HasDirectory(string relative)
        {
            return directories.Contains(relative, StringComparer.FromComparison(Comparison));
        }

        // Files inside the given folder, directly or at any depth.
        internal IEnumerable<string> FilesIn(string directory, bool nested)
        {
            var prefix = directory + "/";
            return files.Where(path => path.StartsWith(prefix, Comparison) && (nested || path.IndexOf('/', prefix.Length) < 0));
        }

        // The names directly inside the mod's folder, files and folders alike.
        internal IEnumerable<string> NamesTop()
        {
            return files.Concat(directories).Where(path => !path.Contains('/'));
        }

        internal static bool IsHidden(string relative)
        {
            return relative.Split('/').Any(segment => segment.StartsWith('.'));
        }

        internal static bool HasExtension(string relative, string extension)
        {
            return relative.EndsWith(extension, Comparison);
        }
    }
}
