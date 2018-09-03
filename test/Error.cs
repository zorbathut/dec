namespace DefTest
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class Error : Base
    {
        [Test]
        public void ErrorTest()
        {
            Assert.Throws(typeof(ArgumentException), () => Def.Config.ErrorHandler("Test"));

            ExpectError(() => Def.Config.ErrorHandler("Test"));

            // Make sure things are deinited properly
            Assert.Throws(typeof(ArgumentException), () => Def.Config.ErrorHandler("Test"));
        }
    }
}
