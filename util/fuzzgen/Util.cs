
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
    }
}
