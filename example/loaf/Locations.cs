
namespace Loaf.Locations
{
    using System.IO;

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

    public class FairyGrotto : Location
    {
        public FairyGrotto(LocationDef locationDef) { }

        public override OutcomeDef Visit()
        {
            Cns.Out("For some reason, you decide to visit the Fairy Grotto.");
            Cns.Out("The Fairy of the Grotto offers to save your game!");
            Cns.Out("You have no idea what this means, but the Fairy is terrifying beyond all reason, so you agree in the desperate hope that it isn't painful.");
            Cns.Out("");
            Cns.Out("It is incredibly painful.");
            Cns.Out("");

            // This seems like an appropriate time to do this.
            File.WriteAllText("loaf.sav", Def.Recorder.Write(Player.Instance));

            Cns.Out("The Fairy cheerfully informs you that your game has been saved.");
            Cns.Out("Come back any time!");

            return Outcomes.Return;
        }
    }
}

