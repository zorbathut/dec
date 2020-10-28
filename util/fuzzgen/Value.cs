
namespace Fuzzgen
{
    internal class Value
    {
        public int value;

        public string WriteCsharp()
        {
            return value.ToString();
        }

        public string WriteXml()
        {
            return value.ToString();
        }
    }
}
