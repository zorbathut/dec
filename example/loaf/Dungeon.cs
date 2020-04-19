
namespace Loaf
{
    using System.IO;

    public class ChoiceFightDef : Def.Def { }

    public static class Dungeon
    {
        [Def.StaticReferences]
        public static class DungeonChoices
        {
            static DungeonChoices()
            {
                Def.StaticReferencesAttribute.Initialized();
            }

            public static ChoiceFightDef Fight;
            public static ChoiceFightDef Run;
        }

        public static void Fight(MonsterDef monster)
        {
            int playerHp = 10;
            int monsterHp = monster.hp;

            Cns.Out($"");
            Cns.Out($"A {monster.name} approaches!");

            while (playerHp > 0 && monsterHp > 0)
            {
                Cns.Out($"");
                Cns.Out($"{playerHp,4} / {20,4}: Your health");
                Cns.Out($"{monsterHp,4} / {monster.hp,4}: The monster's health");
                Cns.Out($"");

                var choice = Cns.Choice<ChoiceFightDef>();
                if (choice == DungeonChoices.Fight)
                {
                    playerHp -= 2;
                    monsterHp -= 4;
                }
                else if (choice == DungeonChoices.Run)
                {
                    break;
                }
            }
        }
    }
}