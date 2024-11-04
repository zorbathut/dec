using System;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class UserSettings : Base
    {
        public class IgnoreRecordDuringParserDec : Dec.Dec
        {
            public IgnoreRecordDuringParserObj obj;
        }

        public class IgnoreRecordDuringParserObj : Dec.IConditionalRecordable
        {
            public int parserMode = 0;

            public bool ShouldRecord(Dec.Recorder.IUserSettings userSettings)
            {
                return !(userSettings is ParserModeUserSetting);
            }
            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref parserMode, "recorderMode");
            }
        }

        public class ParserModeUserSetting : Dec.Recorder.IUserSettings
        {

        }

        [Test]
        public void IgnoreRecordDuringParserParser([ValuesExcept(ParserMode.Validation)] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(IgnoreRecordDuringParserDec), typeof(IgnoreRecordDuringParserObj) } });

            var parser = new Dec.Parser(new ParserModeUserSetting());
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <IgnoreRecordDuringParserDec decName=""TestDec"">
                        <obj>
                            <parserMode>20</parserMode>
                        </obj>
                    </IgnoreRecordDuringParserDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode, xmlValidator: xml => {
                Assert.IsTrue(xml.Contains("recorderMode"));
                Assert.IsFalse(xml.Contains("parserMode"));

                return true;
            });

            Assert.AreEqual(20, Dec.Database<IgnoreRecordDuringParserDec>.Get("TestDec").obj.parserMode);
        }

        [Test]
        public void IgnoreRecordDuringParserRecorder([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

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

        public class UserSettingsDec : Dec.Dec
        {
            public UserSettingsObj obj;
        }

        public class UserSettingsObj : Dec.IRecordable
        {
            public int specialMode = 0;
            public int normalMode = 0;

            public void Record(Dec.Recorder recorder)
            {
                if (recorder.UserSettings is SpecialParserModeSettings)
                {
                    recorder.Record(ref specialMode, "data");
                    normalMode = 10;
                }
                else
                {
                    recorder.Record(ref normalMode, "data");
                }
            }
        }

        public class SpecialParserModeSettings : Dec.Recorder.IUserSettings
        {

        }

        [Test]
        public void SpecialParserMode([ValuesExcept(ParserMode.Bare, ParserMode.Validation)] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(UserSettingsDec), typeof(UserSettingsObj) } });

            var parser = new Dec.Parser(new SpecialParserModeSettings());
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <UserSettingsDec decName=""TestDec"">
                        <obj>
                            <data>20</data>
                        </obj>
                    </UserSettingsDec>
                </Decs>");
            parser.Finish();

            Assert.AreEqual(20, Dec.Database<UserSettingsDec>.Get("TestDec").obj.specialMode);
            Assert.AreEqual(10, Dec.Database<UserSettingsDec>.Get("TestDec").obj.normalMode);

            DoParserTests(mode);

            Assert.AreEqual(0, Dec.Database<UserSettingsDec>.Get("TestDec").obj.specialMode);
            Assert.AreEqual(10, Dec.Database<UserSettingsDec>.Get("TestDec").obj.normalMode);
        }
    }
}
