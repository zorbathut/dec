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
    }
}
