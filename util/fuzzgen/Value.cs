
namespace Fuzzgen
{
    internal class Value
    {
        public string valueCs;
        public string valueXml;

        public string WriteCsharp()
        {
            return valueCs;
        }

        public string WriteXml()
        {
            return valueXml;
        }
    }
}
