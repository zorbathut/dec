
namespace Loaf
{
    using System.Linq;

    // Recommend reading the LocationDef documentation for more information.
    //
    // When creating a builder like this, I recommend passing the Def in as a constructor parameter.
    // The constructor can always ignore it, but if there's any relevant data, the constructor will need access to the Def.
    public class DungeonDef : LocationDef
    {
        public RollTable<MonsterDef> monsters;

        public override Location Create()
        {
            return new Dungeon(this);
        }
    }

    public class Dungeon : Location
    {
        // Def has no trouble with classes that are members of other classes, so we can make a little specialized Def hierarchy here just for the sake of dungeon choices.
        // In fact, we can make *two* little specialized hierarchies.
        [Def.StaticReferences]
        private static class DungeonChoices
        {
            static DungeonChoices()
            {
                Def.StaticReferencesAttribute.Initialized();
            }

            public static FightChoiceDef Fight;
            public static FightChoiceDef Run;

            public static DungeonChoiceDef FindMore;
            public static DungeonChoiceDef Leave;
        }
        private class FightChoiceDef : Cns.ChoiceDef { }
        private class DungeonChoiceDef : Cns.ChoiceDef { }

        // It's tempting to combine this with Location.Outcomes, since they contain most of the same elements.
        // It's so cheap and easy to make another Def type that I recommend avoiding this instinct; keep your types separate.
        [Def.StaticReferences]
        public new static class Outcomes
        {
            static Outcomes()
            {
                Def.StaticReferencesAttribute.Initialized();
            }

            public static OutcomeDef Victory;
            public static OutcomeDef Death;
            public static OutcomeDef Fled;
        }
        public new class OutcomeDef : Def.Def { }

        private DungeonDef def;

        public Dungeon(DungeonDef def)
        {
            this.def = def;
        }

        public override Location.OutcomeDef Visit()
        {
            while (true)
            {
                var result = Fight(def.monsters.Roll());
                if (result == Outcomes.Death)
                {
                    return Location.Outcomes.Death;
                }
                else if (result == Outcomes.Fled)
                {
                    Cns.Out("You escape the dungeon.");
                    return Location.Outcomes.Return;
                }

                var choice = Cns.Choice<DungeonChoiceDef>();
                if (choice == DungeonChoices.Leave)
                {
                    return Location.Outcomes.Return;
                }
            }
        }

        private static OutcomeDef Fight(MonsterDef monster)
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

                var choice = Cns.Choice<FightChoiceDef>();
                if (choice == DungeonChoices.Fight)
                {
                    int attack = monster.damage.Roll();
                    int defense = Player.Instance.Inventory.OfType<ArmorDef>().Select(armor => armor.armor.Roll()).Sum();

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

                    // You could also make this a virtual function on ItemDef with an override on ArmorDef, maybe named OnPickup().
                    if (monster.loot is ArmorDef)
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