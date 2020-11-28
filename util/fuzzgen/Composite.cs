
using System.Collections.Generic;
using System.Text;

namespace Fuzzgen
{
    internal class Composite
    {
        public string name;

        public enum Type
        {
            Struct,
            Class,
            Dec,
        }
        public Type type;

        public List<Member> members = new List<Member>();

        public string WriteCsharpDefinition()
        {
            var sb = new StringBuilder();

            switch (type)
            {
                case Type.Struct:
                    sb.AppendLine($"public struct {name}");
                    break;

                case Type.Class:
                    sb.AppendLine($"public class {name}");
                    break;

                case Type.Dec:
                    sb.AppendLine($"public class {name} : Dec.Dec");
                    break;
            }

            sb.AppendLine($"{{");

            foreach (var m in members)
            {
                sb.AppendLine($"    {m.WriteCSharpInit()}");
            }

            sb.AppendLine($"}}");

            return sb.ToString();
        }
    }

    internal class CompositeTypeDistributionDec : Distribution<Composite.Type> { }

    [Dec.StaticReferences]
    internal static class CompositeTypeDistribution
    {
        static CompositeTypeDistribution() { Dec.StaticReferencesAttribute.Initialized(); }

        internal static CompositeTypeDistributionDec Distribution;
    }
}