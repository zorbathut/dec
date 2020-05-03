
namespace Loaf
{
    public class ItemDef : Def.Def
    {
        public string name;
    }

    public class WeaponDef : ItemDef
    {
        public Dice damage;
        public int price;
    }

    public class ArmorDef : ItemDef
    {
        public Dice armor;
    }
}
