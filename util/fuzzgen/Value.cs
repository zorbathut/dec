
using System;
using System.Collections.Generic;
using System.Text;

namespace Fuzzgen
{
    internal abstract class Value
    {
        public abstract string WriteCsharpInit();
        public abstract string WriteXml();
    }

    internal class ValueSimple : Value
    {
        public string valueCs;
        public string valueXml;

        public ValueSimple(string value)
        {
            this.valueCs = value;
            this.valueXml = value;
        }

        public ValueSimple(string valueCs, string valueXml)
        {
            this.valueCs = valueCs;
            this.valueXml = valueXml;
        }

        public override string WriteCsharpInit()
        {
            return $" = {valueCs}";
        }

        public override string WriteXml()
        {
            return valueXml;
        }
    }

    internal class ValueComposite : Value
    {
        private Instance instance;

        public ValueComposite(Env env, Composite c)
        {
            instance = new Instance(env, c);
        }

        public override string WriteCsharpInit()
        {
            return instance.WriteCSharpInit();
        }

        public override string WriteXml()
        {
            return instance.WriteXmlComposite();
        }
    }

    internal class ValueList : Value
    {
        private List<Value> values = new List<Value>();

        public ValueList(Env env, Func<Value> generateValue)
        {
            int count = Rand.WeightedMiniDistribution();
            for (int i = 0; i < count; ++i)
            {
                values.Add(generateValue());
            }
        }

        public override string WriteCsharpInit()
        {
            // no, for now
            return " = null";
        }

        public override string WriteXml()
        {
            var sb = new StringBuilder();
            foreach (var val in values)
            {
                sb.AppendLine($"<li>{val.WriteXml()}</li>");
            }
            return sb.ToString();
        }
    }

    internal class ValueDict : Value
    {
        private List<Value> keys = new List<Value>();
        private List<Value> values = new List<Value>();

        public ValueDict(Env env, Func<Value> generateKey, Func<Value> generateValue)
        {
            int count = Rand.WeightedMiniDistribution();
            for (int i = 0; i < count; ++i)
            {
                keys.Add(generateKey());
                values.Add(generateValue());
            }

            // we should really de-duplicate keys, but I'm not right now
        }

        public override string WriteCsharpInit()
        {
            // no, for now
            return " = null";
        }

        public override string WriteXml()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < keys.Count; ++i)
            {
                sb.AppendLine($"<li><key>{keys[i].WriteXml()}</key><value>{values[i].WriteXml()}</value></li>");
            }
            return sb.ToString();
        }
    }
}
