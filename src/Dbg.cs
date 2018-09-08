namespace Def
{
    using System;

    internal static class Dbg
    {
        internal static void Inf(string format)
        {
            Config.InfoHandler(format);
        }

        internal static void Inf(string format, params object[] args)
        {
            Config.InfoHandler(string.Format(format, args));
        }

        internal static void Wrn(string format)
        {
            Config.WarningHandler(format);
        }

        internal static void Wrn(string format, params object[] args)
        {
            Config.WarningHandler(string.Format(format, args));
        }

        internal static void Err(string format)
        {
            Config.ErrorHandler(format);
        }

        internal static void Err(string format, params object[] args)
        {
            Config.ErrorHandler(string.Format(format, args));
        }

        internal static void Ex(Exception e)
        {
            Config.ExceptionHandler(e);
        }
    }
}