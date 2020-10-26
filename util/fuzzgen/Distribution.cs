
using System.Collections.Generic;

namespace Fuzzgen
{
    [Def.Abstract]
    public abstract class Distribution<T> : Def.Def
    {
        private readonly Dictionary<T, float> elements = new Dictionary<T, float>();

        public T Choose()
        {
            var chosen = default(T);
            float total = 0f;

            foreach (var elem in elements)
            {
                if (Rand.Next(total + elem.Value) >= total)
                {
                    chosen = elem.Key;
                }

                total += elem.Value;
            }

            return chosen;
        }
    }
}