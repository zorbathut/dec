
namespace Loaf
{
    public static class Random
    {
        private static System.Random Generator;

        static Random()
        {
            Generator = new System.Random();
        }

        public static float Value(float val)
        {
            return (float)(Generator.NextDouble() * val);
        }

        public static int Roll(int sides)
        {
            // this is technically slightly biased
            return Generator.Next() % sides + 1;
        }
    }
}
