
using System;
using System.IO;
using System.Linq;

namespace Fuzzgen
{
    internal static class Rand
    {
        private static readonly Random RandObj = new Random();

        private static readonly string[] AnimalWords;

        static Rand()
        {
            AnimalWords = File.ReadAllText("data/animals.txt").Split(new char[] { ' ', '\r', '\n' }).Distinct().ToArray();
        }

        public static int Next(int max)
        {
            return RandObj.Next(max);
        }

        public static float Next(float max)
        {
            return (float)(RandObj.NextDouble() * max);
        }

        public static int NextInt()
        {
            return RandObj.Next(int.MinValue, int.MaxValue);
        }

        public static long NextLong()
        {
            // this distribution is probably weird but it's good enough
            ulong low = (ulong)NextInt();
            ulong high = (ulong)NextInt() << 32;
            return (long)(low | high);
        }

        public static string NextString()
        {
            string name = "";

            for (int i = 0; i < 3; ++i)
            {
                name += AnimalWords[Fuzzgen.Rand.Next(AnimalWords.Length)];
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
            return (int)( Math.Pow(10, Math.Pow(Fuzzgen.Rand.Next(1f), 2) * 2) - 1);
        }

        // 0-10, weighted towards 0
        // and yes the same thing as above
        public static int WeightedMiniDistribution()
        {
            return (int)( Math.Pow(10, Math.Pow(Fuzzgen.Rand.Next(1f), 1) * 1) - 1);
        }
    }
}