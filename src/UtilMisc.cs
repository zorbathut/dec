using System;
using System.Text.RegularExpressions;

namespace Dec
{
    internal static class UtilMisc
    {
        internal static string LooseMatchCanonicalize(string input)
        {
            return input.Replace("_", "").ToLower();
        }

        internal static int IndexOfUnbounded(this string input, char character)
        {
            int index = input.IndexOf(character);
            if (index == -1)
            {
                return input.Length;
            }
            else
            {
                return index;
            }
        }

        internal static int IndexOfUnbounded(this string input, char character, int start)
        {
            if (start >= input.Length)
            {
                return input.Length;
            }

            int index = input.IndexOf(character, start);
            if (index == -1)
            {
                return input.Length;
            }
            else
            {
                return index;
            }
        }

        internal static string SubstringSafe(this string input, int start)
        {
            start = Math.Clamp(start, 0, input.Length);
            return input.Substring(start);
        }

        internal static string SubstringSafe(this string input, int start, int count)
        {
            start = Math.Clamp(start, 0, input.Length);
            count = Math.Clamp(count, 0, input.Length - start);
            return input.Substring(start, count);
        }

        internal static string[] DefaultTupleNames = new string[]
        {
            "Item1",
            "Item2",
            "Item3",
            "Item4",
            "Item5",
            "Item6",
            "Item7",
            "Rest",
        };

        // this should really be yanked out of here
        private static readonly Regex DecNameValidator = new Regex(@"^[\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}][\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}\p{Mn}\p{Mc}\p{Nd}\p{Pc}\p{Cf}]*$", RegexOptions.Compiled);
        internal static bool ValidateDecName(string name, InputContext context)
        {
            if (DecNameValidator.IsMatch(name))
            {
                return true;
            }

            // This feels very hardcoded, but these are also *by far* the most common errors I've seen, and I haven't come up with a better and more general solution
            if (name.Contains(" "))
            {
                Dbg.Err($"{context}: Dec name `{name}` is not a valid identifier; consider removing spaces");
            }
            else if (name.Contains("\""))
            {
                Dbg.Err($"{context}: Dec name `{name}` is not a valid identifier; consider removing quotes");
            }
            else
            {
                Dbg.Err($"{context}: Dec name `{name}` is not a valid identifier; dec identifiers must be valid C# identifiers");
            }

            return false;
        }

        // Duplicate of Enum.GetValues(), which doesn't exist in old versions of .NET.
        public static T[] GetEnumValues<T>() where T : Enum
        {
            Type enumType = typeof(T);
            Array values = Enum.GetValues(enumType);
            var result = new T[values.Length];
            values.CopyTo(result, 0);
            return result;
        }

        internal static bool IsNullOrEmpty(this string str)
        {
            return str == null || str == "";
        }
    }
}
