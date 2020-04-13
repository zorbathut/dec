namespace Def
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Linq;

    internal static class Util
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
    }
}
