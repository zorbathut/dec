
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Fuzzgen
{
    internal static class Util
    {
        internal static string Indent(string input, int levels = 1)
        {
            string suffix = "";
            if (input.Length > 0 && input[input.Length - 1] == '\n')
            {
                suffix = "\n";
                input = input.Substring(0, input.Length - 1);
            }

            return Regex.Replace(input, "^", new string(' ', levels * 4), RegexOptions.Multiline) + suffix;
        }

        internal static V TryGetValue<T, V>(this Dictionary<T, V> dict, T key)
        {
            if (key == null)
            {
                return default(V);
            }

            dict.TryGetValue(key, out V holder);
            return holder;
        }

        internal static T RandomElement<T>(this IEnumerable<T> input)
        {
            var enumerator = input.GetEnumerator();

            enumerator.MoveNext();
            var result = enumerator.Current;
            int count = 1;

            while (enumerator.MoveNext())
            {
                ++count;
                if (Rand.OneIn(count))
                {
                    result = enumerator.Current;
                }
            }

            return result;
        }

        internal static T RandomElementOr<T>(this IEnumerable<T> input, T fallback)
        {
            var result = fallback;
            int count = 0;

            var enumerator = input.GetEnumerator();
            while (enumerator.MoveNext())
            {
                ++count;
                if (Rand.OneIn(count))
                {
                    result = enumerator.Current;
                }
            }

            return result;
        }
    }
}
