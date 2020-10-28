
namespace Fuzzgen
{
    internal class Member
    {
        public readonly Composite composite;

        public readonly string name;

        public enum Type
        {
            Int,
            /*Class,
            Def,
            ContainerList,
            ContainerDictionary,*/
        }
        public readonly Type type;

        public readonly Value initialized;

        public Member(Composite composite, string name, Type type)
        {
            this.composite = composite;
            this.name = name;
            this.type = type;

            if (composite.type == Composite.Type.Struct)
            {
                // structs always init to zero
                initialized = new Value() { value = 0 };
            }
            else
            {
                initialized = GenerateValue();
            }
        }

        public Value GenerateValue()
        {
            return new Value() { value = Rand.NextInt() };
        }

        public string TypeToCSharp()
        {
            switch (type)
            {
                case Type.Int: return "int";
                default: Dbg.Err("Invalid type!"); return "int";
            }
        }

        public string WriteCsharp()
        {
            if (composite.type == Composite.Type.Struct)
            {
                return $"public {TypeToCSharp()} {name};";
            }
            else
            {
                return $"public {TypeToCSharp()} {name} = {initialized.WriteCsharp()};";
            }
        }
    }

    internal class MemberTypeDistributionDef : Distribution<Member.Type> { }

    [Def.StaticReferences]
    internal static class MemberTypeDistribution
    {
        static MemberTypeDistribution() { Def.StaticReferencesAttribute.Initialized(); }

        internal static MemberTypeDistributionDef Distribution;
    }
}
