
namespace Loaf
{
    using System.IO;

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

        [Def.StaticReferences]
        public static class Outcomes
        {
            static Outcomes()
            {
                Def.StaticReferencesAttribute.Initialized();
            }

            public static OutcomeDef Victory;
            public static OutcomeDef Death;
            public static OutcomeDef Fled;
        }
        public class OutcomeDef : Def.Def { }

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
            int playerHp = 10;
            int monsterHp = monster.hp;

            Cns.Out($"");
            Cns.Out($"A {monster.name} approaches!");

            while (playerHp > 0 && monsterHp > 0)
            {
                Cns.Out($"");
                Cns.Out($"");
                Cns.Out($"{playerHp,4} / {20,4}: Your health");
                Cns.Out($"{monsterHp,4} / {monster.hp,4}: The monster's health");
                Cns.Out($"");

                var choice = Cns.Choice<FightChoiceDef>();
                if (choice == DungeonChoices.Fight)
                {
                    playerHp -= monster.damage.Roll();
                    monsterHp -= Player.Instance.CurrentWeapon.damage.Roll();
                }
                else if (choice == DungeonChoices.Run)
                {
                    return Outcomes.Fled;
                }
            }

            if (playerHp <= 0)
            {
                return Outcomes.Death;
            }
            else
            {
                Cns.Out("");
                Cns.Out("The monster is slain!", color: System.ConsoleColor.White);

                int goldIncome = monster.gold.Roll();
                Player.Instance.AcquireGold(goldIncome);
                Cns.Out($"You root around in the dirt for a bit and find {goldIncome} gold.");

                Cns.Out("");

                return Outcomes.Victory;
            }
        }
    }
}