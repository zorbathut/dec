
namespace Loaf.Locations
{
    public class YourBedroom : Location
    {
        public YourBedroom(LocationDef locationDef) { }

        public override OutcomeDef Visit()
        {
            Cns.Out("You visit your bedroom.");
            Cns.Out("You take a quick nap.");
            Cns.Out("Your health is restored to full!");
            Cns.Out("It was full anyway because this game doesn't store health between battles.");
            Cns.Out("You take the opportunity to rummage through your pockets.");
            Cns.Out("");
            Cns.Out("You are carrying:");
            foreach (var item in Player.Instance.Inventory)
            {
                Cns.Out($"  {item.name}");
            }
            Cns.Out("");
            Cns.Out($"You are currently wielding a {Player.Instance.CurrentWeapon.name}.");
            Cns.Out("");
            Cns.Out("You leave your bedroom.");

            return Outcomes.Return;
        }
    }
}

