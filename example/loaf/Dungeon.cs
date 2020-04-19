
namespace Loaf
{
    using System.IO;

    public class DungeonDef : Def.Def
    {
        public string name;
        public RollTable<MonsterDef> monsters;
    }

    public class Dungeon
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

        public void Visit()
        {
            while (true)
            {
                var result = Fight(def.monsters.Roll());
                if (result == Outcomes.Death)
                {
                    Cns.Out("You have died.", color: System.ConsoleColor.Red);
                    return;
                }
                else if (result == Outcomes.Fled)
                {
                    Cns.Out("You escape the dungeon.");
                    return;
                }

                Cns.Out("");
                Cns.Out("The monster is slain!", color: System.ConsoleColor.White);

                var choice = Cns.Choice<DungeonChoiceDef>();
                if (choice == DungeonChoices.Leave)
                {
                    return;
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
                    monsterHp -= Player.Instance.GetCurrentWeapon().damage.Roll();
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
                return Outcomes.Victory;
            }
        }
    }
}