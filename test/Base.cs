namespace DefTest
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

            Def.Database.Clear();

            handlingWarnings = false;
            handledWarning = false;

            handlingErrors = false;
            handledError = false;

            Def.Config.UsingNamespaces = new string[0];
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

        public class StubDef : Def.Def
        {
        }

        // Everything after here is designed for the Behavior tests, where we run tests in a variety of ways to test serialization.

        public enum BehaviorMode
        {
             // Don't do anything special; just let it pass through.
            Bare,

            // Write it to .xml, clear the database, and reload it.
            Rewritten,

            // Run it through the Validation writer, compile that code at runtime, make sure it matches.
            Validation,
        }

        public void DoBehavior(BehaviorMode mode, bool rewrite_expectWriteErrors = false, bool rewrite_expectParseErrors = false, bool validation_expectWriteErrors = false)
        {
            if (mode == BehaviorMode.Bare)
            {
                // we actually have nothing to do here, we're good
            }
            else if (mode == BehaviorMode.Rewritten)
            {
                string data = null;
                void RunComposer()
                {
                    var composer = new Def.Composer();
                    data = composer.ComposeXml();
                }

                if (rewrite_expectWriteErrors)
                {
                    ExpectErrors(() => RunComposer());
                }
                else
                {
                    RunComposer();
                }

                Def.Database.Clear();

                // This is a janky hack; it resets the type caches so we also generate errors again properly.
                Def.Config.UsingNamespaces = Def.Config.UsingNamespaces;

                void RunParser()
                {
                    var parser = new Def.Parser();
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
                string data = null;
                void RunComposer()
                {
                    var composer = new Def.Composer();
                    data = composer.ComposeValidation();
                }

                if (validation_expectWriteErrors)
                {
                    ExpectErrors(() => RunComposer());
                }
                else
                {
                    RunComposer();
                }

                string source = @"
                    using NUnit.Framework;

                    public static class TestClass
                    { 
                        public static void Eval()
                        {" +
                            data + @"
                        } 
                    }";

                var syntaxTree = CSharpSyntaxTree.ParseText(source);
                string assemblyName = Path.GetRandomFileName();
                var refPaths = new[] {
                    typeof(object).GetTypeInfo().Assembly.Location,
                    typeof(Def.Def).GetTypeInfo().Assembly.Location,
                    typeof(NUnit.Framework.Assert).GetTypeInfo().Assembly.Location,
                    this.GetType().GetTypeInfo().Assembly.Location,
                    Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "netstandard.dll"),
                    Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Runtime.dll"),
                    Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Collections.dll"),
                };

                var references = refPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray();

                var compilation = CSharpCompilation.Create(
                    assemblyName,
                    syntaxTrees: new[] { syntaxTree },
                    references: references,
                    options: new CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));

                var ms = new MemoryStream();
                var result = compilation.Emit(ms);
                if (!result.Success)
                {
                    Assert.IsTrue(false, string.Join("\n", result.Diagnostics.Select(err => err.ToString())));
                }

                ms.Seek(0, SeekOrigin.Begin);

                var assembly = AssemblyLoadContext.Default.LoadFromStream(ms);
                var t = assembly.GetType("TestClass");
                var m = t.GetMethod("Eval");

                m.Invoke(null, null);
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

        // Everything after here is designed for the Recorder tests, where we run tests in a variety of ways to test serialization.

        public enum RecorderMode
        {
            // Write and read in compact form.
            Bare,

            // Write and read in pretty form.
            Pretty,
        }

        public T DoRecorderRoundTrip<T>(T input, RecorderMode mode, Action<string> testSerializedResult = null, bool expectWriteErrors = false, bool expectReadErrors = false)
        {
            string serialized = null;
            void DoSerialize()
            {
                serialized = Def.Recorder.Write(input, pretty: mode == RecorderMode.Pretty);
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
                deserialized = Def.Recorder.Read<T>(serialized);
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
