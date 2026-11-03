using System;
using System.Text.RegularExpressions;

namespace Dec.Modding
{
    internal static class ModValidation
    {
        // Explicit ASCII classes and \A...\z anchors: \d matches non-ASCII digits, IgnoreCase lets the Kelvin sign through, and $ accepts a trailing newline.
        private static readonly Regex IdRegex = new Regex(@"\A[A-Za-z][A-Za-z0-9_.\-]{0,63}\z", RegexOptions.CultureInvariant);

        // The official SemVer 2.0 grammar, ASCII-only. SemanticVersioning's own strict parser accepts a trailing newline.
        private static readonly Regex VersionRegex = new Regex(@"\A(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-((0|[1-9][0-9]*|[0-9]*[a-zA-Z-][0-9a-zA-Z-]*)(\.(0|[1-9][0-9]*|[0-9]*[a-zA-Z-][0-9a-zA-Z-]*))*))?(\+[0-9a-zA-Z-]+(\.[0-9a-zA-Z-]+)*)?\z", RegexOptions.CultureInvariant);

        // SemanticVersioning represents an empty intersection as this range.
        private static readonly SemanticVersioning.Range RangeEmpty = new SemanticVersioning.Range("<0.0.0");

        internal const string IdRules = "ids are 1 to 64 ASCII letters, digits, `_`, `.`, and `-`, starting with a letter";

        internal static bool IsIdValid(string id)
        {
            return id != null && IdRegex.IsMatch(id);
        }

        internal static SemanticVersioning.Version ParseVersion(string text)
        {
            if (text == null || !VersionRegex.IsMatch(text))
            {
                return null;
            }

            try
            {
                return new SemanticVersioning.Version(text, loose: false);
            }
            catch (OverflowException)
            {
                return null;
            }
        }

        internal static SemanticVersioning.Range ParseRange(string text)
        {
            // SemanticVersioning reads a blank range as "any version"; we'd rather people say that by leaving the element out.
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            try
            {
                return new SemanticVersioning.Range(text, loose: false);
            }
            catch (Exception e) when (e is ArgumentException || e is OverflowException)
            {
                return null;
            }
        }

        // Exact names only; Enum.TryParse would also take numbers and differently-cased names.
        internal static bool TryParseName<T>(string text, out T value) where T : struct, Enum
        {
            var trimmed = text.Trim();
            if (Array.IndexOf(Enum.GetNames(typeof(T)), trimmed) >= 0)
            {
                value = (T)Enum.Parse(typeof(T), trimmed);
                return true;
            }

            value = default;
            return false;
        }

        internal static bool IsSatisfied(SemanticVersioning.Range range, SemanticVersioning.Version version)
        {
            // Dec checks installed versions rather than picking versions to install, so a prerelease of an in-range version counts.
            return range == null || range.IsSatisfied(version, includePrerelease: true);
        }

        internal static bool RangesOverlap(SemanticVersioning.Range a, SemanticVersioning.Range b)
        {
            return a == null || b == null || !a.Intersect(b).Equals(RangeEmpty);
        }
    }
}
