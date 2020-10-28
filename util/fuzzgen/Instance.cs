
using System.Collections.Generic;
using System.Text;

namespace Fuzzgen
{
    internal class Instance
    {
        public string defName;

        public Composite composite;
        public Dictionary<Member, Value> values = new Dictionary<Member, Value>();

        public string WriteCsharp()
        {
            var sb = new StringBuilder();

            sb.AppendLine();
            sb.AppendLine($"Assert.IsNotNull(Def.Database<{composite.name}>.Get(\"{defName}\"));");

            foreach (var val in composite.members)
            {
                var contents = values.TryGetValue(val) ?? val.initialized;
                sb.AppendLine($"Assert.AreEqual({contents.WriteCsharp()}, Def.Database<{composite.name}>.Get(\"{defName}\").{val.name});");
            }

            return sb.ToString();
        }

        public string WriteXml()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"<{composite.name} defName=\"{defName}\">");
            foreach (var val in values)
            {
                sb.AppendLine($"  <{val.Key.name}>{val.Value.WriteXml()}</{val.Key.name}>");
            }
            sb.AppendLine($"</{composite.name}>");

            return sb.ToString();
        }
    }
}
