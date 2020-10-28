
using System.Collections.Generic;
using System.Text;

namespace Fuzzgen
{
    internal class Instance
    {
        public string defName;

        public Composite composite;
        public Dictionary<Member, int> values = new Dictionary<Member, int>();

        public string WriteCsharp()
        {
            var sb = new StringBuilder();

            sb.AppendLine();
            sb.AppendLine($"Assert.IsNotNull(Def.Database<{composite.name}>.Get(\"{defName}\"));");

            foreach (var val in values)
            {
                sb.AppendLine($"Assert.AreEqual({val.Value}, Def.Database<{composite.name}>.Get(\"{defName}\").{val.Key.name});");
            }

            return sb.ToString();
        }

        public string WriteXml()
        {
            var sb = new StringBuilder();

            sb.AppendLine($"<{composite.name} defName=\"{defName}\">");
            foreach (var val in values)
            {
                sb.AppendLine($"  <{val.Key.name}>{val.Value}</{val.Key.name}>");
            }
            sb.AppendLine($"</{composite.name}>");

            return sb.ToString();
        }
    }
}
