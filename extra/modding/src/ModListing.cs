using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Dec.Modding
{
    // Every file and folder in a mod's folder, read once, by `/`-separated path relative to it as spelled on disk. Dot-prefixed names are left out, along with everything inside dot-prefixed folders, as Dec's own directory loading leaves them out; that keeps a mod author's .git and editor settings out of every check. Lookups match case exactly unless told otherwise, so a mod behaves the same whether or not the filesystem ignores case.
    internal class ModListing
    {
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

        // The spelling of the file at this path within the folder, preferring an exact match and otherwise matching ignoring case, or null if there's none; it looks only along the path, so a folder that turns out not to be a mod isn't walked.
        internal static string FindFile(string root, string relative)
        {
            var found = new List<string>();
            var directory = root;
            var segments = relative.Split('/');
            for (int i = 0; i < segments.Length; ++i)
            {
                var entries = i == segments.Length - 1 ? System.IO.Directory.GetFiles(directory) : System.IO.Directory.GetDirectories(directory);
                var names = entries.Where(path => !IsHidden(path)).Select(path => System.IO.Path.GetFileName(path)).ToList();
                var name = names.Contains(segments[i], StringComparer.Ordinal) ? segments[i] : names.Where(candidate => string.Equals(candidate, segments[i], StringComparison.OrdinalIgnoreCase)).OrderBy(candidate => candidate, StringComparer.Ordinal).FirstOrDefault();
                if (name == null)
                {
                    return null;
                }
                found.Add(name);
                directory = System.IO.Path.Combine(directory, name);
            }
            return string.Join('/', found);
        }

        internal bool HasFile(string relative)
        {
            return files.Contains(relative, StringComparer.Ordinal);
        }

        // Files inside the given folder, directly or at any depth.
        internal IEnumerable<string> FilesIn(string directory, bool nested, StringComparison comparison)
        {
            var prefix = directory + "/";
            return files.Where(path => path.StartsWith(prefix, comparison) && (nested || path.IndexOf('/', prefix.Length) < 0));
        }

        private static bool IsHidden(string path)
        {
            return System.IO.Path.GetFileName(path).StartsWith('.');
        }

        internal static bool HasExtension(string relative, string extension, StringComparison comparison)
        {
            return relative.EndsWith(extension, comparison);
        }
    }
}
