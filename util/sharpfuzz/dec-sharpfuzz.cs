using System;
using SharpFuzz;

namespace DecSharpFuzz
{
    class Program
    {
        static void Main(string[] args)
        {
            Fuzzer.Run(str =>
            {
                Dec.Config.DefaultHandlerThrowExceptions = Dec.Config.DefaultExceptionBehavior.Never;
                var parser = new Dec.Parser();
                parser.AddString(str);
                parser.Finish();
            });
        }
    }
}

// We need to provide some dec types it can work with.
class SimpleDec : Dec.Dec
{
    int value;
}
