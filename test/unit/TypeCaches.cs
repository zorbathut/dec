using System;
using NUnit.Framework;

namespace DecTest
{
    // Dec caches the converters and types it finds; these cover picking up assemblies loaded after it first looked.
    [TestFixture]
    public class TypeCaches : Base
    {
        public class LateConverted
        {
            public int value;
        }

        public class ConverterLate : Dec.ConverterString<LateConverted>
        {
            public override string Write(LateConverted input)
            {
                return input.value.ToString();
            }

            public override LateConverted Read(string input, Dec.Context context)
            {
                return new LateConverted() { value = int.Parse(input) };
            }
        }

        [Test]
        public void ConverterAddedRecorderOnly()
        {
            Dec.Recorder.ReadSimple<int>("<Root>4</Root>", "Root");

            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(ConverterLate) } });

            // The Recorder keeps the converter registry it already built until told to rebuild it.
            ExpectErrors(() => Dec.Recorder.ReadSimple<LateConverted>("<Root>7</Root>", "Root"), err => err.Contains("Falling back to reflection") || err.Contains("Text detected"));

            Dec.Config.TypeCachesClear();
            Assert.AreEqual(7, Dec.Recorder.ReadSimple<LateConverted>("<Root>7</Root>", "Root").value);
        }

        [Test]
        public void TypeLoadedRecorderOnly()
        {
            string ns = "LateRecorderType_" + Guid.NewGuid().ToString("N");

            ExpectErrors(() => Dec.Recorder.ReadSimple<object>($@"<Root class=""{ns}.Thing"" />", "Root"), err => err.Contains($"{ns}.Thing") || err.Contains("Falling back to reflection"));

            var assembly = DecUtilLib.Compilation.Compile($@"
                namespace {ns}
                {{
                    public class Thing : Dec.IRecordable
                    {{
                        public int value;

                        public void Record(Dec.Recorder recorder)
                        {{
                            recorder.Record(ref value, ""value"");
                        }}
                    }}
                }}", new System.Reflection.Assembly[] { });

            ExpectErrors(() => Dec.Recorder.ReadSimple<object>($@"<Root class=""{ns}.Thing""><value>7</value></Root>", "Root"), err => err.Contains($"{ns}.Thing") || err.Contains("Falling back to reflection"));

            Dec.Config.TypeCachesClear();
            var thing = Dec.Recorder.ReadSimple<object>($@"<Root class=""{ns}.Thing""><value>7</value></Root>", "Root");
            Assert.AreEqual(assembly.GetType($"{ns}.Thing"), thing.GetType());
        }

        public class LateConvertedDec : Dec.Dec
        {
            public LateConverted late;
        }

        [Test]
        public void ConverterAddedAfterRecorderUse()
        {
            // The Recorder initializes the converter registry before the converter becomes available, the way a game reading settings before loading plugins would.
            Dec.Recorder.ReadSimple<int>("<Root>4</Root>", "Root");

            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(LateConvertedDec) }, explicitConverters = new Type[] { typeof(ConverterLate) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <LateConvertedDec decName=""Item"">
                        <late>7</late>
                    </LateConvertedDec>
                </Decs>");
            parser.Finish();

            Assert.AreEqual(7, Dec.Database<LateConvertedDec>.Get("Item").late.value);
        }

        public class LateTypeDec : Dec.Dec
        {
            public object thing;
        }

        [Test]
        public void TypeLoadedAfterFailedLookup()
        {
            string ns = "LateType_" + Guid.NewGuid().ToString("N");

            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(LateTypeDec) } });

            // The lookup fails before the assembly exists, caching both the failure and the list of known types.
            ExpectErrors(() => Dec.Recorder.ReadSimple<object>($@"<Root class=""{ns}.Thing"" />", "Root"), err => err.Contains($"{ns}.Thing") || err.Contains("Falling back to reflection"));

            var assembly = DecUtilLib.Compilation.Compile($"namespace {ns} {{ public class Thing {{ public int value; }} }}", new System.Reflection.Assembly[] { });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <LateTypeDec decName=""Item"">
                        <thing class=""{ns}.Thing"">
                            <value>7</value>
                        </thing>
                    </LateTypeDec>
                </Decs>");
            parser.Finish();

            var thing = Dec.Database<LateTypeDec>.Get("Item").thing;
            Assert.AreEqual(assembly.GetType($"{ns}.Thing"), thing.GetType());
        }
    }
}
