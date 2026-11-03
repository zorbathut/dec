using System;

namespace Dec.Modding
{
    internal static class Dbg
    {
        internal static void Inf(string str)
        {
            global::Dec.Config.InfoHandler("Modding: " + str);
        }

        internal static void Wrn(string str)
        {
            global::Dec.Config.WarningHandler("Modding: " + str);
        }

        internal static void Err(string str)
        {
            global::Dec.Config.ErrorHandler("Modding: " + str);
        }

        internal static void Ex(Exception e)
        {
            global::Dec.Config.ExceptionHandler(e);
        }
    }
}