
namespace Fuzzgen
{
    internal class Member
    {
        public string name;

        public enum Type
        {
            Primitive,
            Class,
            Def,
            ContainerList,
            ContainerDictionary,
        }
        public Type type;

        public string WriteToOutput()
        {
            return $"{type} {name};";
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
