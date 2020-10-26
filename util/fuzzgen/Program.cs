
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Fuzzgen
{
    internal static class Program
    {
        private static Random Rand = new Random();

        private static string[] AnimalWords;
        private static string GenerateClassName()
        {
            string name = "";

            for (int i = 0; i < 3; ++i)
            {
                name += AnimalWords[Rand.Next(0, AnimalWords.Length)];
            }

            return name;
        }

        private static void Main()
        {
            // Init animal words list
            AnimalWords = File.ReadAllText("data/animals.txt").Split(new char[] {' ', '\n'}).Distinct().ToArray();

            for (int i = 0; i < 100; ++i)
            {
                Console.WriteLine(GenerateClassName());
            }
        }
    }
}
