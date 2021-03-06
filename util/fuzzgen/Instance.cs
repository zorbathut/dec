
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fuzzgen
{
    internal class Instance
    {
        public string decName { get; private set; }

        public Composite composite { get; private set; }
        private Dictionary<Member, Value> values = new Dictionary<Member, Value>();

        public Instance(Env env, Composite composite)
        {
            this.decName = Rand.NextString();
            this.composite = composite;

            float chance = Rand.Next(1f);

            // cut down on infinite loops
            chance /= Math.Max(env.instances.Count / 1000f, 1);

            foreach (var member in composite.members)
            {
                if (Rand.Next(1f) < chance)
                {
                    // generate a value for this member
                    values[member] = member.GenerateValue(env);
                }
            }
        }

        public string WriteCSharpInit()
        {
            var sb = new StringBuilder();

            sb.Append($"= new {composite.name}{{");

            sb.Append(string.Join(", ", composite.members.Select(val =>
            {
                var contents = values.TryGetValue(val);
                if (contents == null)
                {
                    return null;
                }

                return $"{val.name} {contents.WriteCsharpInit()}";
            }).Where(term => term != null)));

            sb.Append($"}}");

            return sb.ToString();
        }

        public string WriteXmlDec()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"<{composite.name} decName=\"{decName}\">");
            sb.AppendLine(WriteXmlComposite());
            sb.AppendLine($"</{composite.name}>");

            return sb.ToString();
        }

        public string WriteXmlComposite()
        {
            var sb = new StringBuilder();

            foreach (var val in values)
            {

                sb.AppendLine($"  <{val.Key.name}>{val.Value.WriteXml()}</{val.Key.name}>");
            }

            return sb.ToString();
        }
    }
}
