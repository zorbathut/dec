using System.IO;
using System.Linq;

namespace Loaf
{
    public static class Game
    {
        // Definitely read the Loaf.Cns documentation if this is your first encounter with Cns.ChoiceDec.
        private class StartGameChoiceDec : Cns.ChoiceDec { }

        [Dec.StaticReferences]
        private static class StartGameChoices
        {
            static StartGameChoices()
            {
                Dec.StaticReferencesAttribute.Initialized();
            }

            public static StartGameChoiceDec NewGame;
            public static StartGameChoiceDec Load;
        }

        private static void InitializePlayer()
        {
            bool newGame = true;

            // If this is your first readthrough of the source, I recommend checking out the documentation for Loaf.Config and Loaf.Cns.Choice at this time.
            if (File.Exists(Config.Global.saveFilename))
            {
                var choice = Cns.Choice<StartGameChoiceDec>();
                if (choice == StartGameChoices.Load)
                {
                    newGame = false;

                    // Check out Player.Record() for the implementation of Player deserialization.
                    Player.Set(Dec.Recorder.Read<Player>(File.ReadAllText(Config.Global.saveFilename)));
                }

                Cns.Out("");
                Cns.Out("");
            }

            // Create player according to our global config.
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

                // This is a good example of traversing an entire database for objects.
                // There's no function that returns the LocationDecs we should be using, nor is there a place where we enumerate them explicitly.
                // Instead, we just grab *all* the Locations, verify which ones are available, and then show those.
                // If someone wanted to make a game mod to introduce a new Location, all they'd need would be a new LocationDec and its associated code or data.
                // If you needed more complicated (and moddable) Location accessibility, it'd be reasonable to just make it a virtual function on LocationDec.
                var destinations = Dec.Database<LocationDec>.List.Where(loc => loc.requiredItem == null || Player.Instance.Inventory.Contains(loc.requiredItem));

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

                    // Unceremoniously dump the player out of the game.
                    return;
                }
            }
        }
    }
}