namespace DecTest
{
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class Bespoke : Base
    {
        public class IgnoreRecordDuringParserDec : Dec.Dec
        {
            public IgnoreRecordDuringParserObj obj;
        }

        public class IgnoreRecordDuringParserObj : Dec.IRecordable
        {
            public int parserMode = 0;

            [Dec.Bespoke.IgnoreRecordDuringParser]
            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref parserMode, "recorderMode");
            }
        }

        [Test]
        public void IgnoreRecordDuringParserParser([Values] BehaviorMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IgnoreRecordDuringParserDec), typeof(IgnoreRecordDuringParserObj) } };

            var parser = new Dec.Parser();
            parser.AddString(@"
                <Decs>
                    <IgnoreRecordDuringParserDec decName=""TestDec"">
                        <obj>
                            <parserMode>20</parserMode>
                        </obj>
                    </IgnoreRecordDuringParserDec>
                </Decs>");
            parser.Finish();

            DoBehavior(mode, xmlValidator: xml => {
                Assert.IsFalse(xml.Contains("recorderMode"));
                Assert.IsTrue(xml.Contains("parserMode"));

                return true;
            });

            Assert.AreEqual(20, Dec.Database<IgnoreRecordDuringParserDec>.Get("TestDec").obj.parserMode);
        }

        [Test]
        public void IgnoreRecordDuringParserRecorder([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            Dec.Config.TestParameters = new Dec.Config.UnitTestParameters { };

            var parser = new Dec.Parser();
            parser.Finish();

            var item = new IgnoreRecordDuringParserObj();
            item.parserMode = 42;

            var deserialized = DoRecorderRoundTrip(item, mode, testSerializedResult: xml => {
                Assert.IsTrue(xml.Contains("recorderMode"));
                Assert.IsFalse(xml.Contains("parserMode"));
            });

            Assert.AreEqual(item.parserMode, deserialized.parserMode);
        }
    }
}
