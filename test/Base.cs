namespace DefTest
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class Base
    {
        [SetUp] [TearDown]
        public void Clean()
        {
            Def.Database.Clear();
        }

        private bool handlingErrors = false;
        private bool handledError = false;
        [OneTimeSetUp]
        public void PrepErrorHook()
        {
            Def.Config.ErrorHandler = str => {
                System.Diagnostics.Debug.Print(str);
                Console.WriteLine(str);

                if (handlingErrors)
                {
                    handledError = true;
                }
                else
                {
                    throw new ArgumentException(str);
                }
            };
        }

        protected void ExpectError(Action action)
        {
            Assert.IsFalse(handlingErrors);
            handlingErrors = true;
            handledError = false;

            action();

            Assert.IsTrue(handlingErrors);
            Assert.IsTrue(handledError);
            handlingErrors = false;
            handledError = false;
        }


    }
}
