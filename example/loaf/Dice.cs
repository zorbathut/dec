
namespace Loaf
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

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

        private class DiceConverter : Def.Converter
        {
            public override HashSet<Type> HandledTypes()
            {
                return new HashSet<Type>() { typeof(Dice) };
            }

            private static Regex Parser = new Regex("(?<count>[0-9]+)d(?<sides>[0-9]+)", RegexOptions.Compiled);
            public override object FromString(string input, Type type, string inputName, int lineNumber)
            {
                var result = Parser.Match(input);
                if (result == null)
                {
                    Dbg.Err("{inputName}:{lineNumber}: Failed to parse dice; {input}");
                    return null;
                }

                var rv = new Dice();
                rv.count = int.Parse(result.Groups["count"].Value);
                rv.sides = int.Parse(result.Groups["sides"].Value);

                return rv;
            }
        }
    }
}