namespace Loaf
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;

    public static class Cns
    {
        public class ChoiceDef : Def.Def
        {

        }

        internal static void Out(string str, bool crlf = true, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            if (crlf)
            {
                str = str + "\n";
            }

            var stopwatch = Stopwatch.StartNew();

            // Print it slow to make it feel more like a game coming over a modem.
            for (int idx = 0; idx < str.Length; ++idx)
            {
                while (stopwatch.ElapsedTicks < (idx * Stopwatch.Frequency * 10 / Config.Global.baud));

                Console.Write(str[idx]);
            }

            if (crlf)
            {
                Thread.Sleep((int)(Config.Global.crlfDelay * 1000));
            }

            Console.ForegroundColor = ConsoleColor.White;
        }

        internal static T Choice<T>(bool longForm = false) where T : ChoiceDef
        {
            string separator;
            string prompt;
            if (longForm)
            {
                separator = "\n";
                prompt = "\n";
            }
            else
            {
                separator = ", ";
                prompt = "? ";
            }

            T[] choices = Def.Database<T>.List;
            string choiceList = string.Join(separator, choices.Select(choice => $"({choice.DefName[0]}){choice.DefName.Substring(1)}"));

            Out($"{choiceList}{prompt}", crlf: false);
            while (true)
            {
                var key = Console.ReadKey(true);

                var choice = choices.Where(c => char.ToLower(c.DefName[0]) == char.ToLower(key.KeyChar)).FirstOrDefault();
                if (choice != null)
                {
                    Out(""); // we need a CRLF
                    return choice;
                }
            }
        }
    }
}