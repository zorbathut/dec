using NUnit.Framework;

namespace RecorderEnumeratorTest
{
    [TestFixture]
    public class Base : DecTest.Base
    {
        // A subclass fixture sets this to rerun its suite on Dec's reflection fallback instead of its generated code.
        protected bool forceFallbackEmit = false;

        [OneTimeSetUp]
        public void PrepConfig()
        {
            global::Dec.RecorderEnumerator.Config.Setup();
        }

        // Runs after DecTest.Base's setup, which clears the switch.
        [SetUp]
        public void PrepFallback()
        {
            UpdateTestForceFallbackEmit(forceFallbackEmit);
        }
    }
}
