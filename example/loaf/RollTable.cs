using System.Collections.Generic;
using System.Linq;

namespace Loaf
{
    // This is parsed by Dec as part of the DungeonDec class.
    // There's no extra magic required; DungeonDec specifies which RollTable<> instantiation it is, and Dec just handles it from there.
    public class RollTable<T>
    {
        private Dictionary<T, float> items;

        public T Roll()
        {
            float total = items.Values.Sum();
            float value = Random.Value(total);

            foreach (var item in items)
            {
                value -= item.Value;
                if (value < 0)
                {
                    return item.Key;
                }
            }

            return items.First().Key;
        }
    }
}
