
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fuzzgen
{
    internal class Instance
    {
        private string defName;

        private Composite composite;
        private Dictionary<Member, Value> values = new Dictionary<Member, Value>();

        public Instance(Env env, Composite composite)
        {
            this.defName = Rand.NextString();
            this.composite = composite;

            float chance = Rand.Next(1f);
            foreach (var member in composite.members)
            {
                if (Rand.Next(1f) < chance)
                {
                    // generate a value for this member
                    values[member] = member.GenerateValue(env);
                }
            }
        }

        public string WriteCsharpCompareDef()
        {
            var sb = new StringBuilder();

            sb.AppendLine();
            sb.AppendLine($"Assert.IsNotNull(Def.Database<{composite.name}>.Get(\"{defName}\"));");
            sb.Append(WriteCsharpCompareComposite(composite.name, defName, ""));

            return sb.ToString();
        }

        public string WriteCsharpCompareComposite(string defType, string defName, string prefix)
        {
            var sb = new StringBuilder();

            foreach (var val in composite.members)
            {
                var contents = values.TryGetValue(val) ?? val.initialized;
                if (contents == null)
                {
                    // a struct composite that hasn't been overridden; we should really be comparing this against null
                    continue;
                }

                sb.AppendLine(contents.WriteCsharpCompare(defType, defName, prefix + val.name));
            }

            return sb.ToString();
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

        public string WriteXmlDef()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"<{composite.name} defName=\"{defName}\">");
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
