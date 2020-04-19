namespace Loaf
{
    using System;
    using System.Linq;

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
                Console.WriteLine(str);
            }
            else
            {
                Console.Write(str);
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