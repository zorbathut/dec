
namespace Loaf
{
    using System.IO;

    

    public static class Dungeon
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
        }
        private class FightChoiceDef : Def.Def { }

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

        public static OutcomeDef Fight(MonsterDef monster)
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
                    monsterHp -= 4;
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