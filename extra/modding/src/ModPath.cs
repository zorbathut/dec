using System;

namespace Dec.Modding
{
    internal static class ModPath
    {
        // Windows paths are case-insensitive; elsewhere, assume they aren't.
        internal static readonly StringComparison Comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        internal static readonly StringComparer Comparer = StringComparer.FromComparison(Comparison);

        internal static bool Equal(string a, string b)
        {
            return Comparer.Equals(System.IO.Path.GetFullPath(a), System.IO.Path.GetFullPath(b));
        }

        internal static bool IsWithin(string path, string directory)
        {
            var prefix = System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(directory)) + System.IO.Path.DirectorySeparatorChar;
            return System.IO.Path.GetFullPath(path).StartsWith(prefix, Comparison);
        }
    }
}
