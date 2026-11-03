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
    }
}
