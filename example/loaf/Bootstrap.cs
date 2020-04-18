
namespace Loaf
{
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

            var parser = new Def.Parser();
            // load files
            parser.Finish();
            
            Dbg.Inf("Welcome to Legend of the Amethyst Futon!");
            Dbg.Inf("Your quest: find the Amethyst Futon, rumored to be the most comfortable resting device in the kingdom.");
            Dbg.Inf("Good luck!");
        }
    }
}