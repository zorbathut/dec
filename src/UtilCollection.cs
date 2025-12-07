using System;
using System.Collections.Generic;

namespace Dec
{
    internal static class UtilCollection
    {
        internal static V TryGetValue<T, V>(this Dictionary<T, V> dict, T key)
        {
            if (key == null)
            {
                return default(V);
            }

            dict.TryGetValue(key, out V holder);
            return holder;
        }

        internal static string ToCommaString(this IEnumerable<string> list)
        {
            string result = "";
            bool first = true;
            foreach (var str in list)
            {
                if (!first)
                {
                    result += ", ";
                }
                first = false;

                result += str;
            }
            return result;
        }

        internal static IEnumerable<T> Concat<T>(this IEnumerable<T> enumerable, T element)
        {
            foreach (var e in enumerable)
            {
                yield return e;
            }

            yield return element;
        }

        internal static int FirstIndexOf<T>(this IEnumerable<T> enumerable, Func<T, bool> func)
        {
            int index = 0;
            var enumerator = enumerable.GetEnumerator();

            while (enumerator.MoveNext())
            {
                if (func(enumerator.Current))
                {
                    return index;
                }

                ++index;
            }

            return -1;
        }

        internal static IEnumerable<T> FindDuplicates<T>(this IEnumerable<T> source)
        {
            var seen = new HashSet<T>();
            var duplicates = new HashSet<T>();

            foreach (var item in source)
            {
                if (!seen.Add(item))
                    duplicates.Add(item);
            }

            return duplicates;
        }
    }
}
