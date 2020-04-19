namespace Loaf
{
    using System;
    using System.Linq;

    internal static class Cns
    {
        internal static void Out(string str, bool crlf = true)
        {
            if (crlf)
            {
                Console.WriteLine(str);
            }
            else
            {
                Console.Write(str);
            }
        }

        internal static T Choice<T>() where T : Def.Def
        {
            T[] choices = Def.Database<T>.List;
            string choiceList = string.Join(", ", choices.Select(choice => $"({choice.DefName[0]}){choice.DefName.Substring(1)})"));

            Out($"{choiceList}? ", crlf: false);
            while (true)
            {
                var key = Console.ReadKey(true);

                var choice = choices.Where(c => char.ToLower(c.DefName[0]) == char.ToLower(key.KeyChar)).FirstOrDefault();
                if (choice != null)
                {
                    return choice;
                }
            }
        }
    }
}