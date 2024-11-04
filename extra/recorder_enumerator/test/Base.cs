using Dec;
using NUnit.Framework;

namespace RecorderEnumeratorTest
{
    [TestFixture]
    public class Base : DecTest.Base
    {
        [OneTimeSetUp]
        public void PrepConfig()
        {
            Config.ConverterFactory = global::Dec.RecorderEnumerator.Config.ConverterFactory;
        }
    }
}