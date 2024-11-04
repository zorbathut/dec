using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Loaf
{
    // This is an example of using a custom Converter class.
    // Nobody wants to write <damage><count>4</count><sides>6</sides></damage>, so instead you can just do <damage>4d6</damage>.
    public class Dice
    {
        private int count;
        private int sides;

        public float Average
        {
            get => (sides / 2f + 0.5f) * count;
        }

        public int Roll()
        {
            int accumulator = 0;
            for (int i = 0; i < count; ++i)
            {
                accumulator += Random.Roll(sides);
            }
            return accumulator;
        }

        private class DiceConverter : Dec.ConverterString<Dice>
        {
            // We don't have to worry *too* much about exceptions here; any exceptions that are thrown are guaranteed to be caught, reported, and recovered from.
            private static Regex Parser = new Regex("(?<count>[0-9]+)d(?<sides>[0-9]+)", RegexOptions.Compiled);
            public override Dice Read(string input, Dec.InputContext inputContext)
            {
                var result = Parser.Match(input);
                if (result == null)
                {
                    Dbg.Err("{inputContext}: Failed to parse dice; {input}");
                    return null;
                }

                var rv = new Dice();
                rv.count = int.Parse(result.Groups["count"].Value);
                rv.sides = int.Parse(result.Groups["sides"].Value);

                return rv;
            }

            public override string Write(Dice input)
            {
                return $"{input.count}d{input.sides}";
            }
        }
    }
}