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
            private string label;
            private char key = '\0';

            public string Label
            {
                get
                {
                    return label ?? DefName;
                }
            }

            public char Key
            {
                get
                {
                    return (key != '\0') ? key : Label[0];
                }
            }

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

        internal static T Choice<T>(T[] items, Func<T, string> label, Func<T, char> key = null, bool longForm = false)
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

            if (key == null)
            {
                key = item => label(item)[0];
            }

            T[] choices = items;
            string choiceList = string.Join(separator, choices.Select(choice =>
            {
                string choiceLabel = label(choice);
                int index = choiceLabel.IndexOf(key(choice), StringComparison.InvariantCultureIgnoreCase);
                return $"{choiceLabel.Substring(0, index)}({char.ToUpper(key(choice))}){choiceLabel.Substring(index + 1)}";
            }));

            Out($"{choiceList}{prompt}", crlf: false);
            while (true)
            {
                // get rid of pending input
                while (Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                }

                var userKey = Console.ReadKey(true);

                var success = choices.Where(c => char.ToLower(key(c)) == char.ToLower(userKey.KeyChar));
                var choice = success.SingleOrDefault();
                if (success.Any())
                {
                    Out(""); // we need a CRLF
                    return choice;
                }
            }
        }

        internal static T Choice<T>(T[] items = null, bool longForm = false) where T : ChoiceDef
        {
            return Choice(items ?? Def.Database<T>.List, choice => choice.Label, key: choice => choice.Key, longForm: longForm);
        }
    }
}