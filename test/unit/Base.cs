namespace DecTest
{
    using NUnit.Framework;
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    [TestFixture]
    public class Base
    {
        [SetUp] [TearDown]
        public void Clean()
        {
            // Turns out Hebrew is basically the worst-case scenario for parsing of this sort.
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("he-IL");

            // But reset this just in case.
            Dec.Config.CultureInfo = new System.Globalization.CultureInfo("en-US");

            // flatten these entirely for the sake of tests that don't list anything
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] {}, explicitConverters = new Type[] {}, explicitStaticRefs = new Type[] {}});

            // stop verifying things
            errorValidator = null;

            // we turn on error handling so that Clear can work even if we're in the wrong mode
            handlingErrors = true;

            Dec.Database.Clear();

            handlingWarnings = false;
            handledWarning = false;

            handlingErrors = false;
            handledError = false;

            Dec.Config.UsingNamespaces = new string[0];

            AssertWrapper.Assert.FailureCallback = null;
        }

        private bool handlingWarnings = false;
        private bool handledWarning = false;

        private bool handlingErrors = false;
        private bool handledError = false;
        private Func<string, bool> errorValidator = null;
        private Func<string, bool> warningValidator = null;

        [OneTimeSetUp]
        public void PrepHooks()
        {
            Dec.Config.InfoHandler = str =>
            {
                System.Diagnostics.Debug.Print(str);
                Console.WriteLine(str);

                // we forgot to do the string interpolation correctly
                Assert.IsFalse(str.Contains("{"));
                Assert.IsFalse(str.Contains("}"));
            };

            Dec.Config.WarningHandler = str =>
            {
                System.Diagnostics.Debug.Print(str);
                Console.WriteLine(str);

                // we forgot to do the string interpolation correctly
                Assert.IsFalse(str.Contains("{"));
                Assert.IsFalse(str.Contains("}"));

                // Check to see if this is considered a "valid" warning.
                Assert.IsTrue(warningValidator == null || warningValidator(str), $"Warning message validation failed: {str}");

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

            Dec.Config.ErrorHandler = str =>
            {
                System.Diagnostics.Debug.Print(str);
                Console.WriteLine(str);

                // we forgot to do the string interpolation correctly
                Assert.IsFalse(str.Contains("{"));
                Assert.IsFalse(str.Contains("}"));

                // Check to see if this is considered a "valid" error.
                Assert.IsTrue(errorValidator == null || errorValidator(str), $"Error message validation failed: {str}");

                if (str.Contains("Internal error", StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new InvalidOperationException(str);
                }

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

            Dec.Config.ExceptionHandler = e =>
            {
                Dec.Config.ErrorHandler(e.ToString());
            };

            PrepCwd();
        }

        public static void UpdateTestParameters(Dec.Config.UnitTestParameters parameters)
        {
            typeof(Dec.Config).GetField("TestParameters", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, parameters);
        }

        public static void UpdateTestRefEverything(bool testRefEverything)
        {
            typeof(Dec.Config).GetField("TestRefEverything", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, testRefEverything);
        }

        public static void PrepCwd()
        {
            // Find our data directory
            while (!Directory.Exists("data"))
            {
                Environment.CurrentDirectory = Path.GetDirectoryName(Environment.CurrentDirectory);
            }
        }

        protected void ExpectWarnings(Action action, string context = "unlabeled context", Func<string, bool> warningValidator = null)
        {
            Assert.IsFalse(handlingWarnings);
            handlingWarnings = true;
            handledWarning = false;
            this.warningValidator = warningValidator;

            action();

            Assert.IsTrue(handlingWarnings);
            Assert.IsTrue(handledWarning, $"Expected warning in {context} but did not generate one");
            handlingWarnings = false;
            handledWarning = false;
            this.warningValidator = null;
        }

        // Return "true" if this is the expected error, "false" if this is a bad error
        protected void ExpectErrors(Action action, string context = "unlabeled context", Func<string, bool> errorValidator = null)
        {
            Assert.IsFalse(handlingErrors);
            handlingErrors = true;
            handledError = false;
            this.errorValidator = errorValidator;

            action();

            Assert.IsTrue(handlingErrors);
            Assert.IsTrue(handledError, $"Expected error in {context} but did not generate one");
            handlingErrors = false;
            handledError = false;
            this.errorValidator = null;
        }

        // Some stubs and universally-useful tools

        public class Stub { }
        public class StubDec : Dec.Dec { }
        public class StubRecordable : Dec.IRecordable
        {
            public void Record(Dec.Recorder record)
            {
                // lol
            }
        }

        public enum GenericEnum
        {
            Alpha,
            Beta,
            Gamma,
            Delta,
        }

        public static object CompileAndRun(string code, Assembly[] assemblies, string functionName, params object[] param)
        {
            string source = @"
                    using DecTest.AssertWrapper;

                    public static class TestClass
                    {
                        " + code + @"
                    }";

            var assembly = DecUtilLib.Compilation.Compile(source, assemblies);
            var t = assembly.GetType("TestClass");
            var m = t.GetMethod(functionName);

            return m.Invoke(null, param);
        }

        // Everything after here is designed for the Behavior tests, where we run tests in a variety of ways to test serialization.

        public enum ParserMode
        {
             // Don't do anything special; just let it pass through.
            Bare,

            // Write it to .xml, clear the database, and reload it.
            RewrittenPretty,

            // Same as above, but without nice tab indents.
            RewrittenBare,

            // Run it through the Validation writer, compile that code at runtime, make sure it matches.
            Validation,
        }

        public void DoParserTests(ParserMode mode,
            bool rewrite_expectWriteErrors = false,
            bool rewrite_expectParseErrors = false,
            bool validation_expectWriteErrors = false,
            Assembly[] validation_assemblies = null,
            Func<string, bool> errorValidator = null,
            Func<string, bool> xmlValidator = null)
        {
            if (mode == ParserMode.Bare)
            {
                // we actually have nothing to do here, we're good
            }
            else if (mode == ParserMode.RewrittenBare || mode == ParserMode.RewrittenPretty)
            {
                string data = null;
                void RunComposer()
                {
                    var composer = new Dec.Composer();
                    data = composer.ComposeXml(mode == ParserMode.RewrittenPretty);
                }

                if (rewrite_expectWriteErrors)
                {
                    ExpectErrors(() => RunComposer(), "DoParserTests.Write", errorValidator: errorValidator);
                }
                else
                {
                    RunComposer();
                }

                Assert.IsTrue(xmlValidator == null || xmlValidator(data));

                Dec.Database.Clear();

                // This is a janky hack; it resets the type caches so we also generate errors again properly.
                Dec.Config.UsingNamespaces = Dec.Config.UsingNamespaces;

                void RunParser()
                {
                    var parser = new Dec.Parser();
                    parser.AddString(Dec.Parser.FileType.Xml, data);
                    parser.Finish();
                }

                if (rewrite_expectParseErrors)
                {
                    ExpectErrors(() => RunParser(), "DoParserTests.Read", errorValidator: errorValidator);
                }
                else
                {
                    RunParser();
                }
            }
            else if (mode == ParserMode.Validation)
            {
                string code = null;
                void RunComposer()
                {
                    var composer = new Dec.Composer();
                    code = composer.ComposeValidation();
                }

                if (validation_expectWriteErrors)
                {
                    ExpectErrors(() => RunComposer(), errorValidator: errorValidator);
                }
                else
                {
                    RunComposer();
                }

                var assemblies = new Assembly[] { this.GetType().Assembly };
                if (validation_assemblies != null)
                {
                    assemblies = assemblies.Concat(validation_assemblies).ToArray();
                }

                CompileAndRun($"public static void Test() {{\n{code}\n}}", assemblies, "Test", null);
            }
            else
            {
                Assert.IsTrue(false, "Bad case for behavior mode!");
            }
        }

        public System.Reflection.Assembly GetDecAssembly()
        {
            return AppDomain.CurrentDomain.GetAssemblies().Single(asm => asm.ManifestModule.Name == "dec.dll");
        }

        // Everything after here is designed for the Recorder tests, where we run tests in a variety of ways to test serialization.

        public enum RecorderMode
        {
            // Write and read in compact form.
            Bare,

            // Write and read in pretty form.
            Pretty,

            // Make everything possible into a ref.
            RefEverything,

            // Use the Clone function instead of a write/read pair.
            Clone,

            // Generate validation code beforehand, then run that code.
            Validation,
        }

        public T DoRecorderRoundTrip<T>(T input, RecorderMode mode, Action<string> testSerializedResult = null, bool expectWriteErrors = false, bool expectWriteWarnings = false, bool expectReadErrors = false, bool expectReadWarnings = false, Func<string, bool> readErrorValidator = null)
        {
            if (mode == RecorderMode.Clone)
            {
                bool expectErrors = expectWriteErrors || expectReadErrors;
                bool expectWarnings = expectWriteWarnings || expectReadWarnings;
                Assert.IsFalse(expectErrors && expectWarnings);

                T result = default;
                void DoClone()
                {
                    result = Dec.Recorder.Clone(input);
                }
                if (expectErrors)
                {
                    ExpectErrors(DoClone, "DoRecorder.Clone", errorValidator: readErrorValidator);
                }
                else if (expectWarnings)
                {
                    ExpectWarnings(DoClone, "DoRecorder.Clone");
                }
                else
                {
                    DoClone();
                }

                return result;
            }

            UpdateTestRefEverything(mode == RecorderMode.RefEverything);

            if (mode == RecorderMode.Validation)
            {
                string code = "";

                if (expectWriteErrors)
                {
                    // We don't really insist on an error, but we tolerate one.
                    ExpectErrors(() =>
                    {
                        code = Dec.Recorder.WriteValidation(input);
                        handledError = true; // good enough, just continue
                    }, "DoRecorder.Validation.Write");
                }
                else
                {
                    code = Dec.Recorder.WriteValidation(input);
                }

                var ComposeCSFormatted = Assembly.GetAssembly(typeof(Dec.Dec)).GetType("Dec.UtilType").GetMethod("ComposeCSFormatted", BindingFlags.NonPublic | BindingFlags.Static);

                CompileAndRun($"public static void Test({ComposeCSFormatted.Invoke(null, new object[] { input.GetType() })} input) {{\n{code}\n}}", new Assembly[] { this.GetType().Assembly }, "Test", new object[] { input });
            }

            string serialized = null;
            void DoSerialize()
            {
                serialized = Dec.Recorder.Write(input, pretty: mode == RecorderMode.Pretty);
            }
            Assert.IsFalse(expectWriteErrors && expectWriteWarnings); // nyi
            if (expectWriteErrors)
            {
                ExpectErrors(DoSerialize, "DoRecorder.Write");
            }
            else if (expectWriteWarnings)
            {
                ExpectWarnings(DoSerialize, "DoRecorder.Write");
            }
            else
            {
                DoSerialize();
            }
            Assert.IsNotNull(serialized);

            if (testSerializedResult != null)
            {
                testSerializedResult(serialized);
            }

            T deserialized = default;
            void DoDeserialize()
            {
                deserialized = Dec.Recorder.Read<T>(serialized, stringName: "recorderTestInput");
            }
            Assert.IsFalse(expectReadErrors && expectReadWarnings); // nyi
            if (expectReadErrors)
            {
                ExpectErrors(DoDeserialize, "DoRecorder.Read", errorValidator: readErrorValidator);
            }
            else if (expectReadWarnings)
            {
                ExpectWarnings(DoDeserialize, "DoRecorder.Read");
            }
            else
            {
                DoDeserialize();
            }
            Assert.IsNotNull(serialized);

            // reset
            UpdateTestRefEverything(false);

            return deserialized;
        }
    }
}
