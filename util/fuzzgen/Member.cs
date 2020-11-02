
namespace Fuzzgen
{
    internal class Member
    {
        public readonly Composite composite;

        public readonly string name;

        public enum Type
        {
            Short,
            Ushort,
            Int,
            Uint,
            Long,
            Ulong,
            Float,
            Double,
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
                initialized = new Value() { valueCs = "0", valueXml = "0" };
            }
            else
            {
                initialized = GenerateValue();
            }
        }

        public Value GenerateValue()
        {
            switch (type)
            {
                case Type.Short:
                    {
                        short value = (short)Rand.NextInt();
                        return new Value() { valueCs = value.ToString(), valueXml = value.ToString() };
                    }
                case Type.Ushort:
                    {
                        ushort value = (ushort)Rand.NextInt();
                        return new Value() { valueCs = value.ToString(), valueXml = value.ToString() };
                    }
                case Type.Int:
                    {
                        int value = (int)Rand.NextInt();
                        return new Value() { valueCs = value.ToString(), valueXml = value.ToString() };
                    }
                case Type.Uint:
                    {
                        uint value = (uint)Rand.NextInt();
                        return new Value() { valueCs = value.ToString(), valueXml = value.ToString() };
                    }
                case Type.Long:
                    {
                        long value = (long)Rand.NextLong();
                        return new Value() { valueCs = value.ToString(), valueXml = value.ToString() };
                    }
                case Type.Ulong:
                    {
                        ulong value = (ulong)Rand.NextLong();
                        return new Value() { valueCs = value.ToString(), valueXml = value.ToString() };
                    }
                case Type.Float:
                    {
                        float value = Rand.NextFloat();
                        if (float.IsNaN(value))
                        {
                            return new Value() { valueCs = "float.NaN", valueXml = float.NaN.ToString() };
                        }
                        else
                        {
                            // In theory the "R" format here should work, but .net2.1 bugs prevent it from doing so.
                            return new Value() { valueCs = value.ToString("G17") + 'f', valueXml = value.ToString("G29") };
                        }
                    }
                case Type.Double:
                    {
                        double value = Rand.NextDouble();
                        if (double.IsNaN(value))
                        {
                            return new Value() { valueCs = "double.NaN", valueXml = double.NaN.ToString() };
                        }
                        else
                        {
                            // In theory the "R" format here should work, but .net2.1 bugs prevent it from doing so.
                            return new Value() { valueCs = value.ToString("G17") + 'd', valueXml = value.ToString("G29") };
                        }
                    }
                default:
                    Dbg.Err("Unknown member type!");
                    return new Value() { valueCs = "0", valueXml = "0" };
            }
        }

        public string TypeToCSharp()
        {
            switch (type)
            {
                case Type.Short: return "short";
                case Type.Ushort: return "ushort";
                case Type.Int: return "int";
                case Type.Uint: return "uint";
                case Type.Long: return "long";
                case Type.Ulong: return "ulong";
                case Type.Float: return "float";
                case Type.Double: return "double";
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
