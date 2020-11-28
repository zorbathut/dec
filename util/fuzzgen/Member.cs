
using System.Linq;

namespace Fuzzgen
{
    internal class Member
    {
        public readonly Composite parent;

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
            String,
            Bool,
            Composite,
            Dec,
            ContainerList,
            ContainerDictionary,
        }
        public readonly Type type;

        public readonly Value initialized;
        private Composite compositeChild;

        private Member containerKey;
        private Member containerValue;

        public Member(Env env, Composite parent, string name, Type type)
        {
            // In this function, we must fully define what type we are (important in the case of Dec or containers)
            // and also set our default value (if relevant.)

            this.parent = parent;
            this.name = name;
            this.type = type;

            if (type == Type.Composite)
            {
                if (parent.type == Composite.Type.Struct)
                {
                    // We only want to include structs that happen after us in the type list
                    var structs = env.types.SkipWhile(etype => etype != parent).Skip(1).Where(etype => etype.type == Composite.Type.Struct);
                    var classes = env.types.Where(etype => etype.type == Composite.Type.Class);
                    compositeChild = structs.Concat(classes).RandomElement();
                }
                else
                {
                    compositeChild = env.types.Where(etype => etype.type != Composite.Type.Dec).RandomElement();
                }

                if (parent.type == Composite.Type.Struct)
                {
                    initialized = null; // won't be used anywhere anyway, just fall back on defaults
                }
                else
                {
                    initialized = new ValueComposite(env, compositeChild);
                }
            }
            else if (type == Type.Dec)
            {
                compositeChild = env.types.Where(inst => inst.type == Composite.Type.Dec).RandomElement();
                initialized = new ValueSimple("null", "");
            }
            else if (type == Type.ContainerList)
            {
                containerValue = new Member(env, parent, null, MemberTypeDistribution.Distribution.Choose());
                initialized = new ValueSimple("null", "");
            }
            else if (type == Type.ContainerDictionary)
            {
                containerKey = new Member(env, parent, null, MemberTypeDistribution.DictKeyDistribution.Choose());
                containerValue = new Member(env, parent, null, MemberTypeDistribution.Distribution.Choose());
                initialized = new ValueSimple("null", "");
            }
            else if (parent.type == Composite.Type.Struct)
            {
                // structs always init to basic types
                if (type == Type.String || type == Type.Composite && this.compositeChild.type != Composite.Type.Struct)
                {
                    initialized = new ValueSimple("null");
                }
                else if (type == Type.Bool)
                {
                    initialized = new ValueSimple("false");
                }
                else if (type != Type.Composite)
                {
                    initialized = new ValueSimple("0");
                }

                // otherwise it's a struct composite and we don't really have anything sensible to compare it to
            }
            else
            {
                initialized = GenerateValue(env);
            }
        }

        public Value GenerateValue(Env env)
        {
            switch (type)
            {
                case Type.Short:
                    return new ValueSimple(((short)Rand.NextInt()).ToString());
                case Type.Ushort:
                    return new ValueSimple(((ushort)Rand.NextInt()).ToString());
                case Type.Int:
                    return new ValueSimple(((int)Rand.NextInt()).ToString());
                case Type.Uint:
                    return new ValueSimple(((uint)Rand.NextInt()).ToString());
                case Type.Long:
                    return new ValueSimple(((long)Rand.NextLong()).ToString());
                case Type.Ulong:
                    return new ValueSimple(((ulong)Rand.NextLong()).ToString());
                case Type.Float:
                    {
                        float value = Rand.NextFloat();
                        if (float.IsNaN(value))
                        {
                            return new ValueSimple("float.NaN", float.NaN.ToString());
                        }
                        else
                        {
                            // In theory the "R" format here should work, but .net2.1 bugs prevent it from doing so.
                            return new ValueSimple(value.ToString("G17") + 'f', value.ToString("G29"));
                        }
                    }
                case Type.Double:
                    {
                        double value = Rand.NextDouble();
                        if (double.IsNaN(value))
                        {
                            return new ValueSimple("double.NaN", double.NaN.ToString());
                        }
                        else
                        {
                            // In theory the "R" format here should work, but .net2.1 bugs prevent it from doing so.
                            return new ValueSimple(value.ToString("G17") + 'd', value.ToString("G29"));
                        }
                    }
                case Type.String:
                    {
                        string value = Rand.NextString();
                        return new ValueSimple($"\"{value}\"", value);
                    }
                case Type.Bool:
                    {
                        bool value = Rand.OneIn(2f);
                        return new ValueSimple(value.ToString().ToLower(), value.ToString());
                    }
                case Type.Composite:
                    return new ValueComposite(env, compositeChild);
                case Type.Dec:
                    {
                        var instance = env.instances.Where(inst => inst.composite == compositeChild).RandomElementOr(null);
                        if (instance == null)
                        {
                            return new ValueSimple("null", "");
                        }
                        else
                        {
                            return new ValueSimple($"Dec.Database<{compositeChild.name}>.Get(\"{instance.decName}\")", instance.decName);
                        }
                    }
                case Type.ContainerList:
                    return new ValueList(env, () => containerValue.GenerateValue(env));
                case Type.ContainerDictionary:
                    return new ValueDict(env, () => containerKey.GenerateValue(env), () => containerValue.GenerateValue(env));
                default:
                    Dbg.Err("Unknown member type!");
                    return new ValueSimple("0", "0");
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
                case Type.String: return "string";
                case Type.Bool: return "bool";
                case Type.Composite: return compositeChild.name;
                case Type.Dec: return compositeChild.name;
                case Type.ContainerList: return $"List<{containerValue.TypeToCSharp()}>";
                case Type.ContainerDictionary: return $"Dictionary<{containerKey.TypeToCSharp()}, {containerValue.TypeToCSharp()}>";
                default: Dbg.Err("Invalid type!"); return "int";
            }
        }

        public string WriteCSharpInit()
        {
            if (parent.type == Composite.Type.Struct || initialized == null)
            {
                return $"public {TypeToCSharp()} {name};";
            }
            else
            {
                return $"public {TypeToCSharp()} {name}{initialized.WriteCsharpInit()};";
            }
        }
    }

    internal class MemberTypeDistributionDec : Distribution<Member.Type> { }

    [Dec.StaticReferences]
    internal static class MemberTypeDistribution
    {
        static MemberTypeDistribution() { Dec.StaticReferencesAttribute.Initialized(); }

        internal static MemberTypeDistributionDec Distribution;
        internal static MemberTypeDistributionDec DictKeyDistribution;
    }
}
