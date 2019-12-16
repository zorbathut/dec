namespace Def
{
    using System;

    internal static class Dbg
    {
        internal static void Inf(string format)
        {
            Config.InfoHandler(format);
        }

        internal static void Wrn(string format)
        {
            Config.WarningHandler(format);
        }

        internal static void Err(string format)
        {
            Config.ErrorHandler(format);
        }

        internal static void Ex(Exception e)
        {
            Config.ExceptionHandler(e);
        }
    }
}