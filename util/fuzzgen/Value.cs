
using System;
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
}
