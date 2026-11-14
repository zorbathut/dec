using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dec.Modding
{
    // Every file and folder in a mod's folder, read once, by `/`-separated path relative to it as spelled on disk. Dot-prefixed names are left out, along with everything inside dot-prefixed folders, as Dec's own directory loading leaves them out; that keeps a mod author's .git and editor settings out of every check.
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

            // Each folder carries the real paths of the folders above it, so a link back up the tree is listed but not followed; a mod archive can hold one, and following it repeats the tree until the filesystem gives up.
            var pending = new Stack<(string path, List<string> chain)>();
            pending.Push((root, new List<string>() { Real(root) }));
            while (pending.Count > 0)
            {
                var (directory, chain) = pending.Pop();
                files.AddRange(System.IO.Directory.GetFiles(directory).Where(path => !IsHidden(path)).Select(path => Relative(root, path)));
                foreach (var child in System.IO.Directory.GetDirectories(directory).Where(path => !IsHidden(path)))
                {
                    directories.Add(Relative(root, child));

                    var real = Real(child);
                    if (!chain.Contains(real, ModPath.Comparer))
                    {
                        pending.Push((child, chain.Append(real).ToList()));
                    }
                }
            }

            return new ModListing(root, files, directories);
        }

        private static string Real(string directory)
        {
            return System.IO.Path.TrimEndingDirectorySeparator(System.IO.Directory.ResolveLinkTarget(directory, returnFinalTarget: true)?.FullName ?? System.IO.Path.GetFullPath(directory));
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

        private static bool IsHidden(string path)
        {
            return System.IO.Path.GetFileName(path).StartsWith('.');
        }

        internal static bool HasExtension(string relative, string extension)
        {
            return relative.EndsWith(extension, Comparison);
        }
    }
}
