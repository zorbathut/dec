
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
    }
}