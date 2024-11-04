using System.Linq;

namespace Loaf
{
    // Recommend reading the LocationDec documentation for more information.
    //
    // When creating a builder like this, I recommend passing the Dec in as a constructor parameter.
    // The constructor can always ignore it, but if there's any relevant data, the constructor will need access to the Dec.
    public class DungeonDec : LocationDec
    {
        public RollTable<MonsterDec> monsters;

        public override Location Create()
        {
            return new Dungeon(this);
        }
    }

    public class Dungeon : Location
    {
        // Dec has no trouble with classes that are members of other classes, so we can make a little specialized Dec hierarchy here just for the sake of dungeon choices.
        // In fact, we can make *two* little specialized hierarchies.
        [Dec.StaticReferences]
        private static class DungeonChoices
        {
            static DungeonChoices()
            {
                Dec.StaticReferencesAttribute.Initialized();
            }

            public static FightChoiceDec Fight;
            public static FightChoiceDec Run;

            public static DungeonChoiceDec FindMore;
            public static DungeonChoiceDec Leave;
        }
        private class FightChoiceDec : Cns.ChoiceDec { }
        private class DungeonChoiceDec : Cns.ChoiceDec { }

        // It's tempting to combine this with Location.Outcomes, since they contain most of the same elements.
        // It's so cheap and easy to make another Dec type that I recommend avoiding this instinct; keep your types separate.
        [Dec.StaticReferences]
        public new static class Outcomes
        {
            static Outcomes()
            {
                Dec.StaticReferencesAttribute.Initialized();
            }

            public static OutcomeDec Victory;
            public static OutcomeDec Death;
            public static OutcomeDec Fled;
        }
        public new class OutcomeDec : Dec.Dec { }

        private DungeonDec dec;

        public Dungeon(DungeonDec dec)
        {
            this.dec = dec;
        }

        public override Location.OutcomeDec Visit()
        {
            while (true)
            {
                var result = Fight(dec.monsters.Roll());
                if (result == Outcomes.Death)
                {
                    return Location.Outcomes.Death;
                }
                else if (result == Outcomes.Fled)
                {
                    Cns.Out("You escape the dungeon.");
                    return Location.Outcomes.Return;
                }

                var choice = Cns.Choice<DungeonChoiceDec>();
                if (choice == DungeonChoices.Leave)
                {
                    return Location.Outcomes.Return;
                }
            }
        }

        private static OutcomeDec Fight(MonsterDec monster)
        {
            int playerHp = Config.Global.playerHp;
            int monsterHp = monster.hp;

            Cns.Out($"");
            Cns.Out($"A {monster.name} approaches!");

            while (playerHp > 0 && monsterHp > 0)
            {
                Cns.Out($"");
                Cns.Out($"");
                Cns.Out($"{playerHp,4} / {Config.Global.playerHp,4}: Your health");
                Cns.Out($"{monsterHp,4} / {monster.hp,4}: The monster's health");
                Cns.Out($"");

                var choice = Cns.Choice<FightChoiceDec>();
                if (choice == DungeonChoices.Fight)
                {
                    int attack = monster.damage.Roll();
                    int defense = Player.Instance.Inventory.OfType<ArmorDec>().Select(armor => armor.armor.Roll()).Sum();

                    if (attack > defense)
                    {
                        // Okay this is actually a bug - this should be playerHp -= (attack - defense).
                        // Except I already went through and (vaguely) balanced the game, and this isn't meant to be *fun*, just a tech demo.
                        // So I'm just leaving it in place.
                        playerHp -= monster.damage.Roll();
                    }
                    else
                    {
                        Cns.Out("Its attack bounces off your armor!");
                    }

                    monsterHp -= Player.Instance.CurrentWeapon.damage.Roll();
                }
                else if (choice == DungeonChoices.Run)
                {
                    return Outcomes.Fled;
                }
            }

            if (monsterHp > 0)
            {
                return Outcomes.Death;
            }
            else
            {
                Cns.Out("");
                Cns.Out("The monster is slain!", color: System.ConsoleColor.White);

                if (monster.loot != null && !Player.Instance.Inventory.Contains(monster.loot))
                {
                    Cns.Out($"You find a {monster.loot.name}!", color: System.ConsoleColor.Cyan);

                    // You could also make this a virtual function on ItemDec with an override on ArmorDec, maybe named OnPickup().
                    if (monster.loot is ArmorDec)
                    {
                        Cns.Out($"You put it on. It fits perfectly.", color: System.ConsoleColor.Cyan);
                    }

                    Player.Instance.AcquireItem(monster.loot);
                }

                int goldIncome = monster.gold.Roll();
                Player.Instance.AcquireGold(goldIncome);
                Cns.Out($"You root around in the dirt for a bit and find {goldIncome} gold.");

                Cns.Out("");

                return Outcomes.Victory;
            }
        }
    }
}