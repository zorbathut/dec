using NUnit.Framework;

namespace RecorderEnumeratorTest
{
    [TestFixture]
    public class Base : DecTest.Base
    {
        [OneTimeSetUp]
        public void PrepConfig()
        {
            global::Dec.RecorderEnumerator.Config.Setup();
        }
    }
}