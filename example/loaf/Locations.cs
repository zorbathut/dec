using System.IO;
using System.Linq;

namespace Loaf.Locations
{
    // Definitely avoid the urge to *over*generalize.
    // All of these locations require custom code and it's just not worth the effort to try expressing these in XML.
    // Make a class that does your thing, pass it in as a Type, be done with it.

    public class YourBedroom : Location
    {
        public YourBedroom(LocationDec locationDec) { }

        public override OutcomeDec Visit()
        {
            Cns.Out("You visit your bedroom.");
            Cns.Out("You take a quick nap.");
            Cns.Out("Your health is restored to full!");
            Cns.Out("It was full anyway because this game doesn't store health between battles.");
            Cns.Out("You take the opportunity to rummage through your pockets.");

            Cns.Out("");
            Cns.Out("You are carrying:");
            foreach (var item in Player.Instance.Inventory.Where(item => !(item is ArmorDec)))
            {
                Cns.Out($"  {item.name}");
            }
            if (Player.Instance.Gold > 0)
            {
                Cns.Out($"  {Player.Instance.Gold} gold");
            }

            if (Player.Instance.Inventory.Any(item => item is ArmorDec))
            {
                Cns.Out("");
                Cns.Out("You are wearing:");
                foreach (var item in Player.Instance.Inventory.Where(item => item is ArmorDec))
                {
                    Cns.Out($"  {item.name}");
                }
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
        public FairyGrotto(LocationDec locationDec) { }

        public override OutcomeDec Visit()
        {
            Cns.Out("For some reason, you decide to visit the Fairy Grotto.");
            Cns.Out("The Fairy of the Grotto offers to save your game!");
            Cns.Out("You have no idea what this means, but the Fairy is terrifying beyond all reason, so you agree in the desperate hope that it isn't painful.");
            Cns.Out("");
            Cns.Out("It is incredibly painful.");
            Cns.Out("");

            // This seems like an appropriate time to do this.
            // Check out Player.Record() for the implementation of Player serialization.
            File.WriteAllText(Config.Global.saveFilename, Dec.Recorder.Write(Player.Instance));

            Cns.Out("The Fairy cheerfully informs you that your game has been saved.");
            Cns.Out("Come back any time!");

            return Outcomes.Return;
        }
    }

    public class Shop : Location
    {
        public Shop(LocationDec locationDec) { }

        public override OutcomeDec Visit()
        {
            Cns.Out("Welcome to Armor's Shop!");
            Cns.Out("I'm Armor, the owner of this fine joint. We're the national leader in weapons!");
            Cns.Out("We don't sell armor, though. It's just that my name is Armor.");
            Cns.Out("This is a weapon shop, owned by myself, a man named Armor, who does not make armor.");
            Cns.Out("I acknowledge this is confusing.");
            Cns.Out("");
            Cns.Out($"By the size of your wallet, I can see you have {Player.Instance.Gold} gold. See anything you want to buy?");
            Cns.Out("");

            var choices = Dec.Database<WeaponDec>.List.Where(weapon => weapon.price > 0 && !Player.Instance.Inventory.Contains(weapon)).OrderBy(weapon => weapon.price).ToList();
            choices.Add(null);  // we'll use this for the "nothing" option

            var choice = Cns.Choice<WeaponDec>(choices.ToArray(), weapon =>
            {
                if (weapon == null)
                {
                    return "Leave";
                }

                return $"{weapon.name} ({weapon.price} gold)";
            }, longForm: true);

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

    public class Futon : Location
    {
        public Futon(LocationDec locationDec) { }

        public override OutcomeDec Visit()
        {
            Cns.Out("You've finally tracked down the Amethyst Futon!");
            Cns.Out("It was in your living room all along!");
            Cns.Out("");

            if (!Config.Global.alternateEnding)
            {
                Cns.Out("With excitement and trepidation, you sit down upon the futon.");
                Cns.Out("");
                Cns.Out("");
                Cns.Out("");
                Cns.Out("It's not very comfortable.");
                Cns.Out("");
                Cns.Out("");
            }
            else
            {
                Cns.Out("You're feeling sleepy, so you decide to take a nap.");
                Cns.Out("");
                Cns.Out("You can't figure out how to make it turn into a bed.");
                Cns.Out("");
                Cns.Out("Wait, hold on. There it is.");
                Cns.Out("");
                Cns.Out("No, that's not right. Hold on. Still looking.");
                Cns.Out("");
                Cns.Out("Aha! Found it!");
                Cns.Out("");
                Cns.Out("It doesn't work.");
                Cns.Out("Looks like it's broken.");
                Cns.Out("");
                Cns.Out("");
                Cns.Out("");
                Cns.Out("");
                Cns.Out("");
                Cns.Out("you tried so hard");
                Cns.Out("and got so far");
                Cns.Out("but in the end");
                Cns.Out("it doesn't even mattress");
                Cns.Out("");
                Cns.Out("");
            }

            return Outcomes.Victory;
        }
    }
}

