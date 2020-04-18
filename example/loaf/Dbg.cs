namespace Loaf
{
    using System;

    internal static class Dbg
    {
        internal static void Inf(string str)
        {
            Console.WriteLine(str);
        }

        internal static void Wrn(string str)
        {
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.WriteLine(str);
            Console.BackgroundColor = ConsoleColor.White;
        }

        internal static void Err(string str)
        {
            Console.BackgroundColor = ConsoleColor.Red;
            Console.WriteLine(str);
            Console.BackgroundColor = ConsoleColor.Red;
        }

        internal static void Ex(Exception e)
        {
            Err(e.ToString());
        }
    }
}