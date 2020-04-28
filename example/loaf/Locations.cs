
namespace Loaf.Locations
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

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
            if (Player.Instance.Gold > 0)
            {
                Cns.Out($"  {Player.Instance.Gold} gold");
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

    public class Shop : Location
    {
        public Shop(LocationDef locationDef) { }

        public override OutcomeDef Visit()
        {
            Cns.Out("Welcome to Armor's Shop!");
            Cns.Out("I'm Armor, the owner of this fine joint. We're the national leader in weapons!");
            Cns.Out("We don't sell armor, though. It's just that my name is Armor.");
            Cns.Out("This is a weapon shop, owned by myself, a man named Armor, who does not make armor.");
            Cns.Out("I acknowledge this is confusing.");
            Cns.Out("");
            Cns.Out($"By the size of your wallet, I can see you have {Player.Instance.Gold} gold. See anything you want to buy?");
            Cns.Out("");

            var choices = Def.Database<WeaponDef>.List.Where(weapon => weapon.price > 0 && !Player.Instance.Inventory.Contains(weapon)).OrderBy(weapon => weapon.price).ToList();
            choices.Add(null);  // we'll use this for the "nothing" option

            var choice = Cns.Choice<WeaponDef>(choices.ToArray(), weapon =>
            {
                if (weapon == null)
                {
                    return "Leave";
                }

                return $"{weapon.name} ({weapon.price} gold)";
            }, true);

            if (choice != null)
            {
                if (Player.Instance.SpendGold(choice.price))
                {
                    Cns.Out("All yours, enjoy!");

                    Player.Instance.AcquireItem(choice);
                }
                else
                {
                    Cns.Out("You can't afford it! Get out of my shop and come back when you've got more money.");
                }
            }

            return Outcomes.Return;
        }
    }
}

