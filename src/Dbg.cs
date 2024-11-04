using System;

namespace Dec
{
    internal static class Dbg
    {
        internal static void Inf(string str)
        {
            Config.InfoHandler(str);
        }

        internal static void Wrn(string str)
        {
            Config.WarningHandler(str);
        }

        internal static void Err(string str)
        {
            Config.ErrorHandler(str);
        }

        internal static void Ex(Exception e)
        {
            Config.ExceptionHandler(e);
        }
    }
}