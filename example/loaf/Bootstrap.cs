
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

            var parser = new Def.Parser();
            foreach (var file in Directory.GetFiles("data"))
            {
                parser.AddFile(file);
            }
            parser.Finish();
            
            Cns.Out("Welcome to Legend of the Amethyst Futon!");
            Cns.Out("Your quest: find the Amethyst Futon, rumored to be the most comfortable resting device in the kingdom.");
            Cns.Out("Good luck!");

            new Dungeon(Def.Database<DungeonDef>.Get("TestDungeon")).Visit();
        }
    }
}