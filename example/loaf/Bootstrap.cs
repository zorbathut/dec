namespace Loaf
{
    public static class Bootstrap
    {
        public static void Main()
        {
            // Configure Dec's error reporting
            Dec.Config.InfoHandler = str => Dbg.Inf(str);
            Dec.Config.WarningHandler = str => Dbg.Wrn(str);
            Dec.Config.ErrorHandler = str => Dbg.Err(str);
            Dec.Config.ExceptionHandler = e => Dbg.Ex(e);

            // Configure Dec's namespaces
            Dec.Config.UsingNamespaces = new string[] { "Loaf" };

            // In most cases, you'll just want to read all the XML files in your data directory, which is easy
            var parser = new Dec.Parser();
            parser.AddDirectory("data");
            parser.Finish();

            // Everything is now initialized; start the game!
            Game.Run();

            // In most actual games you'd put this code behind a loading screen of some sort.
            // At the moment, dec loading is essentially atomic and without a progress bar; also, nobody has ever written enough dec files to justify a progress bar.
            // Please let the developers know if you need this functionality!
        }
    }
}