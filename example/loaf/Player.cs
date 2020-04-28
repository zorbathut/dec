
namespace Loaf
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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

        public void Record(Def.Recorder recorder)
        {
            recorder.Record(ref inventory, "inventory");
            recorder.Record(ref gold, "gold");
        }
    }
}
