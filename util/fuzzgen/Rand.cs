
using System;

namespace Fuzzgen
{
    internal static class Rand
    {
        private static Random rand = new Random();

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

        // 0-99, weighted heavily towards 0
        // yes alright this is very adhoc
        public static int WeightedDistribution()
        {
            return (int)(Math.Pow(10, Math.Pow(Rand.Next(1f), 2) * 2) - 1);
        }
    }
}