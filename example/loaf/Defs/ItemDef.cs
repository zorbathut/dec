
namespace Loaf
{
    public class ItemDec : Dec.Dec
    {
        public string name;
    }

    public class WeaponDec : ItemDec
    {
        public Dice damage;
        public int price;
    }

    public class ArmorDec : ItemDec
    {
        public Dice armor;
    }
}
