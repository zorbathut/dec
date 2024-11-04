using NUnit.Framework;
using System;

namespace DecTest
{
    [TestFixture]
    public class Error : Base
    {
        [Test]
        public void WarningTesting()
        {
            // This doesn't happen normally, but does in our test framework
            Assert.Throws(typeof(ArgumentException), () => Dec.Config.WarningHandler("Test"));

            ExpectWarnings(() => Dec.Config.WarningHandler("Test"));

            // Make sure things are deinited properly
            Assert.Throws(typeof(ArgumentException), () => Dec.Config.WarningHandler("Test"));
        }

        [Test]
        public void ErrorTesting()
        {
            Assert.Throws(typeof(ArgumentException), () => Dec.Config.ErrorHandler("Test"));

            ExpectErrors(() => Dec.Config.ErrorHandler("Test"));

            // Make sure things are deinited properly
            Assert.Throws(typeof(ArgumentException), () => Dec.Config.ErrorHandler("Test"));
        }

        [Test]
        public void ErrorValidator()
        {
            ExpectErrors(() => Dec.Config.ErrorHandler("Test"), errorValidator: str => str == "Test");

            // Make sure we get a real error if in fact we shouldn't have passed the error
            Assert.Throws(typeof(NUnit.Framework.AssertionException), () => ExpectErrors(() => Dec.Config.ErrorHandler("Test"), errorValidator: str => str == "Toast"));
        }
    }
}
