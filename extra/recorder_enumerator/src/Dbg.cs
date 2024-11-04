using System;

namespace Dec.RecorderEnumerator
{
    internal static class Dbg
    {
        internal static void Inf(string str)
        {
            global::Dec.Config.InfoHandler("RecorderEnumerator: " + str);
        }

        internal static void Wrn(string str)
        {
            global::Dec.Config.WarningHandler("RecorderEnumerator: " + str);
        }

        internal static void Err(string str)
        {
            global::Dec.Config.ErrorHandler("RecorderEnumerator: " + str);
        }

        internal static void Ex(Exception e)
        {
            global::Dec.Config.ExceptionHandler(e);
        }
    }
}