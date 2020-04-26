
namespace Loaf
{
    using System.Collections.Generic;
    using System.Linq;

    public class Player : SingletonManual<Player>
    {
        private List<ItemDef> inventory = new List<ItemDef>();

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
    }
}
