using System;
using SharpFuzz;

namespace DefSharpFuzz
{
    class Program
    {
        static void Main(string[] args)
        {
            Fuzzer.Run(str =>
            {
                Def.Config.DefaultHandlerThrowExceptions = Def.Config.DefaultExceptionBehavior.Never;
                var parser = new Def.Parser();
                parser.AddString(str);
                parser.Finish();
            });
        }
    }
}

// We need to provide some def types it can work with.
class SimpleDef : Def.Def
{
    int value;
}
