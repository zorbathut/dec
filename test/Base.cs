namespace DecTest
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Loader;

    [TestFixture]
    public class Base
    {
        [SetUp] [TearDown]
        public void Clean()
        {
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

            Dec.Config.ExceptionHandler = e =>
            {
                Dec.Config.ErrorHandler(e.ToString());
            };

            PrepCwd();
        }

        public static void PrepCwd()
        {
            // Find our data directory
            while (!Directory.Exists("data"))
            {
                Environment.CurrentDirectory = Path.GetDirectoryName(Environment.CurrentDirectory);
            }
        }

        protected void ExpectWarnings(Action action)
        {
            Assert.IsFalse(handlingWarnings);
            handlingWarnings = true;
            handledWarning = false;

            action();

            Assert.IsTrue(handlingWarnings);
            Assert.IsTrue(handledWarning, "Expected warning but did not generate one");
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
            Assert.IsTrue(handledError, "Expected error but did not generate one");
            handlingErrors = false;
            handledError = false;
            this.errorValidator = null;
        }

        public class Stub { }
        public class StubDec : Dec.Dec { }
        public class StubRecordable : Dec.IRecordable
        {
            public void Record(Dec.Recorder record)
            {
                // lol
            }
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

        public enum BehaviorMode
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

        public void DoBehavior(BehaviorMode mode, bool rewrite_expectWriteErrors = false, bool rewrite_expectParseErrors = false, bool validation_expectWriteErrors = false, Assembly[] validation_assemblies = null)
        {
            if (mode == BehaviorMode.Bare)
            {
                // we actually have nothing to do here, we're good
            }
            else if (mode == BehaviorMode.RewrittenBare || mode == BehaviorMode.RewrittenPretty)
            {
                string data = null;
                void RunComposer()
                {
                    var composer = new Dec.Composer();
                    data = composer.ComposeXml(mode == BehaviorMode.RewrittenPretty);
                }

                if (rewrite_expectWriteErrors)
                {
                    ExpectErrors(() => RunComposer());
                }
                else
                {
                    RunComposer();
                }

                Dec.Database.Clear();

                // This is a janky hack; it resets the type caches so we also generate errors again properly.
                Dec.Config.UsingNamespaces = Dec.Config.UsingNamespaces;

                void RunParser()
                {
                    var parser = new Dec.Parser();
                    parser.AddString(data);
                    parser.Finish();
                }

                if (rewrite_expectParseErrors)
                {
                    ExpectErrors(() => RunParser());
                }
                else
                {
                    RunParser();
                }
            }
            else if (mode == BehaviorMode.Validation)
            {
                string code = null;
                void RunComposer()
                {
                    var composer = new Dec.Composer();
                    code = composer.ComposeValidation();
                }

                if (validation_expectWriteErrors)
                {
                    ExpectErrors(() => RunComposer());
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

            // Generate validation code beforehand, then run that code.
            Validation,
        }

        public T DoRecorderRoundTrip<T>(T input, RecorderMode mode, Action<string> testSerializedResult = null, bool expectWriteErrors = false, bool expectReadErrors = false)
        {
            if (mode == RecorderMode.Validation)
            {
                var code = Dec.Recorder.WriteValidation(input);

                var ComposeCSFormatted = Assembly.GetAssembly(typeof(Dec.Dec)).GetType("Dec.UtilType").GetMethod("ComposeCSFormatted", BindingFlags.NonPublic | BindingFlags.Static);

                CompileAndRun($"public static void Test({ComposeCSFormatted.Invoke(null, new object[] { input.GetType() })} input) {{\n{code}\n}}", new Assembly[] { this.GetType().Assembly }, "Test", new object[] { input });
            }

            string serialized = null;
            void DoSerialize()
            {
                serialized = Dec.Recorder.Write(input, pretty: mode == RecorderMode.Pretty);
            }
            if (expectWriteErrors)
            {
                ExpectErrors(DoSerialize);
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
                deserialized = Dec.Recorder.Read<T>(serialized);
            }
            if (expectReadErrors)
            {
                ExpectErrors(DoDeserialize);
            }
            else
            {
                DoDeserialize();
            }
            Assert.IsNotNull(serialized);

            return deserialized;
        }
    }
}
