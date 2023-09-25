namespace Dec.RecorderCoroutine
{
    using System;

    internal static class Dbg
    {
        internal static void Inf(string str)
        {
            global::Dec.Config.InfoHandler(str);  
        }

        internal static void Wrn(string str)
        {
            global::Dec.Config.WarningHandler(str);
        }

        internal static void Err(string str)
        {
            global::Dec.Config.ErrorHandler(str);
        }

        internal static void Ex(Exception e)
        {
            global::Dec.Config.ExceptionHandler(e);
        }
    }
}