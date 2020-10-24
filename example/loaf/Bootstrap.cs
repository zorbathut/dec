
namespace Loaf
{
    using System.IO;

    public static class Bootstrap
    {
        public static void Main()
        {
            // Configure Def's error reporting
            Def.Config.InfoHandler = str => Dbg.Inf(str);
            Def.Config.WarningHandler = str => Dbg.Wrn(str);
            Def.Config.ErrorHandler = str => Dbg.Err(str);
            Def.Config.ExceptionHandler = e => Dbg.Ex(e);

            // Configure Def's namespaces
            Def.Config.UsingNamespaces = new string[] { "Loaf" };

            // In most cases, you'll just want to read all the XML files in your data directory, which is easy
            var parser = new Def.Parser();
            parser.AddDirectory("data");
            parser.Finish();

            // Everything is now initialized; start the game!
            Game.Run();

            // In most actual games you'd put this code behind a loading screen of some sort.
            // At the moment, def loading is essentially atomic and without a progress bar; also, nobody has ever written enough def files to justify a progress bar.
            // Please let the developers know if you need this functionality!
        }
    }
}