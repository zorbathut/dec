
namespace Fuzzgen
{
    internal class Member
    {
        public string name;

        public enum Type
        {
            Int,
            /*Class,
            Def,
            ContainerList,
            ContainerDictionary,*/
        }
        public Type type;

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
            return $"public {TypeToCSharp()} {name};";
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
