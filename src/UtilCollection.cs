namespace Def
{
    using System.Collections.Generic;

    internal static class UtilCollection
    {
        internal static V TryGetValue<T, V>(this Dictionary<T, V> dict, T key)
        {
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
                }
            }

            return result;
        }
    }
}
