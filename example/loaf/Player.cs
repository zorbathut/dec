
namespace Loaf
{
    public class Player : Singleton<Player>
    {
        public WeaponDef GetCurrentWeapon()
        {
            return Def.Database<WeaponDef>.List[0];
        }
    }
}
