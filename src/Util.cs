namespace Def
{
    using System.Collections.Generic;

    internal static class Util
    {
        internal static V TryGetValue<T, V>(this Dictionary<T, V> dict, T key)
        {
            dict.TryGetValue(key, out V holder);
            return holder;
        }
    }
}
