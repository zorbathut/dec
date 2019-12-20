namespace DefTest
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class Error : Base
    {
        [Test]
        public void WarningTesting()
        {
            // This doesn't happen normally, but does in our test framework
            Assert.Throws(typeof(ArgumentException), () => Def.Config.WarningHandler("Test"));

            ExpectWarnings(() => Def.Config.WarningHandler("Test"));

            // Make sure things are deinited properly
            Assert.Throws(typeof(ArgumentException), () => Def.Config.WarningHandler("Test"));
        }

        [Test]
        public void ErrorTesting()
        {
            Assert.Throws(typeof(ArgumentException), () => Def.Config.ErrorHandler("Test"));

            ExpectErrors(() => Def.Config.ErrorHandler("Test"));

            // Make sure things are deinited properly
            Assert.Throws(typeof(ArgumentException), () => Def.Config.ErrorHandler("Test"));
        }

        [Test]
        public void ErrorValidator()
        {
            ExpectErrors(() => Def.Config.ErrorHandler("Test"), str => str == "Test");

            // Make sure we get a real error if in fact we shouldn't have passed the error
            Assert.Throws(typeof(NUnit.Framework.AssertionException), () => ExpectErrors(() => Def.Config.ErrorHandler("Test"), str => str == "Toast"));
        }
    }
}
