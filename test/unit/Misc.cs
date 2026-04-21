using System;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class Misc : Base
    {
        // Attempting to parse a struct without Recorder or a converter can result in an unhandled exception.

        struct TestStruct
        {
            public int value;
        }

        struct TestStructAsThis : Dec.IRecordable
        {
            public TestStruct value;

            public void Record(Dec.Recorder recorder)
            {
                recorder.RecordAsThis(ref value);
            }
        }

        struct TestStructMember : Dec.IRecordable
        {
            public TestStruct value;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref value, nameof(value));
            }
        }

        [Test]
        public void UnavailableStructRecorderNormal()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            string recorded = "<Record><recordFormatVersion>1</recordFormatVersion><data /></Record>";

            ExpectErrors(() => Dec.Recorder.Read<TestStruct>(recorded), err => err.Contains("TestStruct") && err.Contains("reflection"));
        }

        [Test]
        public void UnavailableStructRecorderSimple()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            string recorded = "<record />";

            ExpectErrors(() => Dec.Recorder.ReadSimple<TestStruct>(recorded, "record"), err => err.Contains("TestStruct") && err.Contains("reflection"));
        }

        [Test]
        public void UnavailableStructRecorderAsThis()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            string recorded = "<Record><recordFormatVersion>1</recordFormatVersion><data /></Record>";

            ExpectErrors(() => Dec.Recorder.Read<TestStructAsThis>(recorded), err => err.Contains("TestStruct") && err.Contains("reflection"));
        }

        [Test]
        public void UnavailableStructRecorderClone()
        {
            var initial = new TestStructMember();

            ExpectErrors(() => Dec.Recorder.Clone(initial), err => err.Contains("TestStruct") && err.Contains("Couldn't find a composition method"));
        }
    }
}
