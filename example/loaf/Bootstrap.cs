
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

            Game.Run();
        }
    }
}