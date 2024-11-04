using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Loaf
{
    // The Cns class handles console output for Loaf. This consists of two pieces of functionality.
    //
    // Cns.Out prints lines and does the slow-modem-transfer effect.
    // Cns.Choice is much more interesting; it uses the Dec system to make it easy to print out prompts.
    //
    // Using the Dec system for prompt elements seems like massive overkill, but it has advantages.
    // First, it makes prompts very easy to mod.
    // Dec doesn't (yet) support mods, but Locations could be modded by just making more LocationDec's, while other UI elements could be modded with use of new Dec's and with Harmony.
    // Second, it allows you to attach information to prompts easily.
    // During the development of Loaf, I added custom labels and keypresses to prompts; on a GUI game, one could easily add tooltips or other scriptable elements.
    public static class Cns
    {
        // This is our base Choice class.
        // Note that it's tagged Dec.Abstract so that choices for different UI elements end up in different Dec hierarchies and can share DecNames.
        // Otherwise you'd need a unique name for every item that inherits from ChoiceDec.
        [Dec.Abstract]
        public abstract class ChoiceDec : Dec.Dec
        {
            private string label;
            private char key = '\0';

            public string Label
            {
                get
                {
                    return label ?? DecName;
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

        // I also want to support choices for things that aren't ChoiceDecs, so this is a thin adapter to make ChoiceDecs work transparently.
        // Note that, if it isn't given an explicit list of items, it just yanks every possible item out of the Dec database.
        internal static T Choice<T>(T[] items = null, bool longForm = false) where T : ChoiceDec
        {
            return Choice(items ?? Dec.Database<T>.List, choice => choice.Label, key: choice => choice.Key, longForm: longForm);
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
                while (!Config.Global.suppressDelay && stopwatch.ElapsedTicks < (idx * Stopwatch.Frequency * 10 / Config.Global.baud)) { }

                Console.Write(str[idx]);
            }

            if (!Config.Global.suppressDelay && crlf)
            {
                Thread.Sleep((int)(Config.Global.crlfDelay * 1000));
            }

            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}