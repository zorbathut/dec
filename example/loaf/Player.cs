
namespace Loaf
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    // For the sake of this example, we're just using a player singleton.
    // It's marked IRecordable so we can save our game. 
    public class Player : SingletonManual<Player>, Def.IRecordable
    {
        private List<ItemDef> inventory = new List<ItemDef>();
        private int gold = 0;

        public int Gold
        {
            get => gold;
        }

        public IEnumerable<ItemDef> Inventory
        {
            get => inventory;
        }

        public WeaponDef CurrentWeapon
        {
            get => inventory.OfType<WeaponDef>().MaxOfOrDefault(weapon => weapon.damage.Average);
        }

        public void AcquireItem(ItemDef item)
        {
            if (!inventory.Contains(item))
            {
                inventory.Add(item);
            }
        }

        public void AcquireGold(int gold)
        {
            this.gold = (int)Math.Min((long)this.gold + gold, int.MaxValue);
        }

        public bool SpendGold(int gold)
        {
            if (this.gold >= gold)
            {
                this.gold = this.gold - gold;
                return true;
            }
            else
            {
                return false;
            }
        }

        // This is the entire serialization and deserialization code.
        // 
        // I recognize that this is almost suspiciously simple.
        // It feels like one of those tutorials where it shows you an example that is specifically customized to the abilities of the library,
        // and where any actual real-world implementation is many times more complicated and finicky.
        //
        // It isn't, though. It really is this simple.
        //
        // Recorder handles primitive types, collections, Def references, class references, non-tree structures, and circular structures automatically.
        // In most cases all you need to do is list your fields and Recorder will do the rest.
        public void Record(Def.Recorder recorder)
        {
            recorder.Record(ref inventory, "inventory");
            recorder.Record(ref gold, "gold");
        }
    }
}
