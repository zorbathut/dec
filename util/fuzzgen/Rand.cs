
using System;
using System.Linq;
using System.IO;

namespace Fuzzgen
{
    internal static class Rand
    {
        private static Random rand = new Random();

        private static string[] AnimalWords;

        static Rand()
        {
            AnimalWords = File.ReadAllText("data/animals.txt").Split(new char[] { ' ', '\r', '\n' }).Distinct().ToArray();
        }

        public static int Next(int max)
        {
            return rand.Next(max);
        }

        public static float Next(float max)
        {
            return (float)(rand.NextDouble() * max);
        }

        public static int NextInt()
        {
            return rand.Next(int.MinValue, int.MaxValue);
        }

        public static long NextLong()
        {
            // this distribution is probably weird but it's good enough
            return (long)((ulong)NextInt() | ((ulong)NextInt() << 32));
        }

        public static string NextString()
        {
            string name = "";

            for (int i = 0; i < 3; ++i)
            {
                name += AnimalWords[Rand.Next(AnimalWords.Length)];
            }

            return name;
        }

        public static bool OneIn(float val)
        {
            return Next(val) < 1f;
        }

        public static float NextFloat()
        {
            // this distribution is *definitely* weird
            return BitConverter.Int32BitsToSingle(NextInt());
        }

        public static double NextDouble()
        {
            return BitConverter.Int64BitsToDouble(NextLong());
        }

        // 0-99, weighted heavily towards 0
        // yes alright this is very adhoc
        public static int WeightedDistribution()
        {
            return (int)(Math.Pow(10, Math.Pow(Rand.Next(1f), 2) * 2) - 1);
        }
    }
}