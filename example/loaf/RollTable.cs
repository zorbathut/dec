
namespace Loaf
{
    using System.Collections.Generic;
    using System.Linq;

    // This is parsed by Def as part of the DungeonDef class.
    // There's no extra magic required; DungeonDef specifies which RollTable<> instantiation it is, and Def just handles it from there.
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
