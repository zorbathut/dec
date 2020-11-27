
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
            Def,
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

                case Type.Def:
                    sb.AppendLine($"public class {name} : Def.Def");
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

    internal class CompositeTypeDistributionDef : Distribution<Composite.Type> { }

    [Def.StaticReferences]
    internal static class CompositeTypeDistribution
    {
        static CompositeTypeDistribution() { Def.StaticReferencesAttribute.Initialized(); }

        internal static CompositeTypeDistributionDef Distribution;
    }
}