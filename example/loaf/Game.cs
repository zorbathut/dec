
namespace Loaf
{
    using System.IO;
    using System.Linq;

    public static class Game
    {
        private class StartGameChoiceDef : Cns.ChoiceDef { }

        [Def.StaticReferences]
        private static class StartGameChoices
        {
            static StartGameChoices()
            {
                Def.StaticReferencesAttribute.Initialized();
            }

            public static StartGameChoiceDef NewGame;
            public static StartGameChoiceDef Load;
        }

        private static void InitializePlayer()
        {
            bool newGame = true;

            if (File.Exists(Config.Global.saveFilename))
            {
                var choice = Cns.Choice<StartGameChoiceDef>();
                if (choice == StartGameChoices.Load)
                {
                    newGame = false;
                    Player.Set(Def.Recorder.Read<Player>(File.ReadAllText(Config.Global.saveFilename)));
                }

                Cns.Out("");
                Cns.Out("");
            }

            // Create player
            if (newGame)
            {
                var player = new Player();
                foreach (var item in Config.Global.startingItems)
                {
                    player.AcquireItem(item);
                }
                Player.Set(player);
            }
        }

        public static void Run()
        {
            InitializePlayer();

            Cns.Out("Welcome to Legend of the Amethyst Futon!");
            Cns.Out("Your quest: find the Amethyst Futon, rumored to be the most comfortable resting device in the kingdom.");
            Cns.Out("Good luck!");

            while (true)
            {
                Cns.Out("");
                Cns.Out("");
                Cns.Out("You stand at a crossroads, both literally and metaphorically.");
                Cns.Out("");

                var destinations = Def.Database<LocationDef>.List.Where(loc => loc.requiredItem == null || Player.Instance.Inventory.Contains(loc.requiredItem));

                var location = Cns.Choice(items: destinations.ToArray(), longForm: true).Create();
                var result = location.Visit();

                if (result == Location.Outcomes.Death)
                {
                    Cns.Out("");
                    Cns.Out("You have died.", color: System.ConsoleColor.Red);
                    Cns.Out("But that's okay. You got better.");
                }
                else if (result == Location.Outcomes.Victory)
                {
                    Cns.Out("");
                    Cns.Out("CONGRATULATIONS! You win!");

                    // unceremoniously dump the player out of the game
                    return;
                }
            }
        }
    }
}