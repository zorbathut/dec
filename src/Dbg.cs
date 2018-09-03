namespace Def
{
    internal static class Dbg
    {
        internal static void Inf(string format, params object[] args)
        {
            Config.InfoHandler(string.Format(format, args));
        }

        internal static void Wrn(string format, params object[] args)
        {
            Config.WarningHandler(string.Format(format, args));
        }

        internal static void Err(string format, params object[] args)
        {
            Config.ErrorHandler(string.Format(format, args));
        }
    }
}