
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
            Cns.Out("You leave your bedroom.");

            return Outcomes.Return;
        }
    }
}

