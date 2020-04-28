namespace Loaf
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;

    public static class Cns
    {
        [Def.Abstract]
        public abstract class ChoiceDef : Def.Def
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
                while (!Config.Global.suppressDelay && stopwatch.ElapsedTicks < (idx * Stopwatch.Frequency * 10 / Config.Global.baud));

                Console.Write(str[idx]);
            }

            if (!Config.Global.suppressDelay && crlf)
            {
                Thread.Sleep((int)(Config.Global.crlfDelay * 1000));
            }

            Console.ForegroundColor = ConsoleColor.White;
        }

        internal static T Choice<T>(T[] items, Func<T, string> label, bool longForm = false)
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

            T[] choices = items;
            string choiceList = string.Join(separator, choices.Select(choice =>
            {
                string choiceLabel = label(choice);
                return $"({choiceLabel[0]}){choiceLabel.Substring(1)}";
            }));

            Out($"{choiceList}{prompt}", crlf: false);
            while (true)
            {
                var key = Console.ReadKey(true);

                var success = choices.Where(c => char.ToLower(label(c)[0]) == char.ToLower(key.KeyChar));
                var choice = success.SingleOrDefault();
                if (success.Any())
                {
                    Out(""); // we need a CRLF
                    return choice;
                }
            }
        }

        internal static T Choice<T>(bool longForm = false) where T : ChoiceDef
        {
            return Choice(Def.Database<T>.List, choice => choice.DefName, longForm: longForm);
        }
    }
}