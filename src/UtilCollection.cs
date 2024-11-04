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

        internal static T SingleOrDefaultChecked<T>(this IEnumerable<T> elements)
        {
            T result = default(T);
            bool first = true;

            foreach (var element in elements)
            {
                if (first)
                {
                    result = element;
                    first = false;
                }
                else
                {
                    // maybe we need a better error message here.
                    Dbg.Err("Multiple items found when only one is expected");

                    // no point in continuing
                    break;
                }
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
    }
}
