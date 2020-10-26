
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
            Def,
        }
        public Type type;

        public string WriteToOutput()
        {
            var sb = new StringBuilder();

            switch (type)
            {
                case Type.Struct:
                    sb.AppendLine($"internal struct {name}");
                    break;

                case Type.Class:
                    sb.AppendLine($"internal class {name}");
                    break;

                case Type.Def:
                    sb.AppendLine($"internal class {name} : Def.Def");
                    break;
            }

            sb.AppendLine($"{{");
            sb.AppendLine($"}}");

            return sb.ToString();
        }
    }

    internal class CompositeTypeDistributionDef : Distribution<Composite.Type> { }

    [Def.StaticReferences]
    internal static class CompositeTypeDistribution
    {
        static CompositeTypeDistribution() { Def.StaticReferencesAttribute.Initialized(); }

        internal static CompositeTypeDistributionDef Distribution;
    }
}