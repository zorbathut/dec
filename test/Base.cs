namespace DefTest
{
    using NUnit.Framework;
    using System;
    using System.Linq;

    [TestFixture]
    public class Base
    {
        [SetUp] [TearDown]
        public void Clean()
        {
            // we turn on error handling so that Clear can work even if we're in the wrong mode
            handlingErrors = true;

            Def.Database.Clear();

            handlingWarnings = false;
            handledWarning = false;

            handlingErrors = false;
            handledError = false;

            ResetBehaviorParser();
        }

        private bool handlingWarnings = false;
        private bool handledWarning = false;

        private bool handlingErrors = false;
        private bool handledError = false;
        private Func<string, bool> errorValidator = null;
        
        [OneTimeSetUp]
        public void PrepHooks()
        {
            Def.Config.InfoHandler = str =>
            {
                System.Diagnostics.Debug.Print(str);
                Console.WriteLine(str);
            };

            Def.Config.WarningHandler = str =>
            {
                System.Diagnostics.Debug.Print(str);
                Console.WriteLine(str);

                if (handlingWarnings)
                {
                    handledWarning = true;
                }
                else
                {
                    // Throw if we're not handling it - this way we get test failures
                    throw new ArgumentException(str);
                }
            };

            Def.Config.ErrorHandler = str =>
            {
                System.Diagnostics.Debug.Print(str);
                Console.WriteLine(str);

                // Check to see if this is considered a "valid" error.
                Assert.IsTrue(errorValidator == null || errorValidator(str));

                if (handlingErrors)
                {
                    // If we're handling it, don't throw - this way we can validate that fallback behavior is working right
                    handledError = true;
                }
                else
                {
                    // Throw if we're not handling it - this way we get test failures and can validate that exception-passing behavior is working right
                    throw new ArgumentException(str);
                }
            };

            Def.Config.ExceptionHandler = e =>
            {
                Def.Config.ErrorHandler(e.ToString());
            };
        }

        protected void ExpectWarnings(Action action)
        {
            Assert.IsFalse(handlingWarnings);
            handlingWarnings = true;
            handledWarning = false;

            action();

            Assert.IsTrue(handlingWarnings);
            Assert.IsTrue(handledWarning);
            handlingWarnings = false;
            handledWarning = false;
        }

        // Return "true" if this is the expected error, "false" if this is a bad error
        protected void ExpectErrors(Action action, Func<string, bool> errorValidator = null)
        {
            Assert.IsFalse(handlingErrors);
            handlingErrors = true;
            handledError = false;
            this.errorValidator = errorValidator;

            action();

            Assert.IsTrue(handlingErrors);
            Assert.IsTrue(handledError);
            handlingErrors = false;
            handledError = false;
            this.errorValidator = null;
        }

        public class StubDef : Def.Def
        {
        }

        // Everything after here is designed for the Behavior tests, where we run tests in a variety of ways to test serialization.

        private Def.Parser.UnitTestParameters behaviorParserUnitTestParameters = null;

        public void ResetBehaviorParser()
        {
            behaviorParserUnitTestParameters = null;
        }

        public enum BehaviorMode
        {
             // Don't do anything special; just let it pass through.
            Bare,

            // Write it to .xml, clear the database, and reload it.
            Rewritten,
        }

        // This is a thin wrapper around `new Def.Parser` that exists solely so we can recreate a Def.Parser later with the same settings.
        public Def.Parser CreateParserForBehavior(Def.Parser.UnitTestParameters unitTestParameters)
        {
            Assert.IsNull(behaviorParserUnitTestParameters);

            behaviorParserUnitTestParameters = unitTestParameters;

            return new Def.Parser(unitTestParameters: unitTestParameters);
        }

        public void DoBehavior(BehaviorMode mode, bool expectErrors = false)
        {
            if (mode == BehaviorMode.Bare)
            {
                // we actually have nothing to do here, we're good
            }
            else if (mode == BehaviorMode.Rewritten)
            {
                Assert.IsNotNull(behaviorParserUnitTestParameters);

                var writer = new Def.Writer();
                string data = writer.Write();

                Def.Database.Clear();

                void RunParser()
                {
                    var parser = new Def.Parser(unitTestParameters: behaviorParserUnitTestParameters);
                    parser.AddString(data);
                    parser.Finish();
                }

                if (expectErrors)
                {
                    ExpectErrors(() => RunParser());
                }
                else
                {
                    RunParser();
                }
            }
            else
            {
                Assert.IsTrue(false, "Bad case for behavior mode!");
            }
        }

        public System.Reflection.Assembly GetDefAssembly()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Single(asm => asm.ManifestModule.Name == "def.dll");
        }
    }
}
