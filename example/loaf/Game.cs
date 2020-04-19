
namespace Loaf
{
    public static class Game
    {
        public static void Run()
        {
            Cns.Out("Welcome to Legend of the Amethyst Futon!");
            Cns.Out("Your quest: find the Amethyst Futon, rumored to be the most comfortable resting device in the kingdom.");
            Cns.Out("Good luck!");

            while (true)
            {
                Cns.Out("");
                var dungeon = new Dungeon(Cns.Choice<DungeonDef>(longForm: true));
                var result = dungeon.Visit();

                if (result == Dungeon.Outcomes.Death)
                {
                    // Do death here.
                    Cns.Out("");
                    Cns.Out("You have died.", color: System.ConsoleColor.Red);
                    Cns.Out("But that's okay. You got better.");
                }
            }
        }
    }
}