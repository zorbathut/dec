using NUnit.Framework;

namespace DecTest
{
    // Tests designed for V1 of the save format
    [TestFixture]
    public class RecorderV1 : Base
    {
        [Test]
        public void Core()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var deserialized = Dec.Recorder.Read<int>(@"
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <data>4</data>
                </Record>");

            Assert.AreEqual(deserialized, 4);
        }

        [Test]
        public void CoreFailures()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var parser = new Dec.Parser();
            parser.Finish();

            ExpectErrors(() => Assert.AreEqual(0, Dec.Recorder.Read<int>(@"")), err => err.Contains("Root element is missing") || err.Contains("XmlException"));

            ExpectErrors(() => Assert.AreEqual(0, Dec.Recorder.Read<int>(@"
                <MismatchedRoot>
                  <recordFormatVersion>1</recordFormatVersion>
                  <data>4</data>
                </Record>")), err => err.Contains("does not match the end tag") || err.Contains("XmlException"));

            ExpectWarnings(() => Assert.AreEqual(4, Dec.Recorder.Read<int>(@"
                <WrongTag>
                  <recordFormatVersion>1</recordFormatVersion>
                  <data>4</data>
                </WrongTag>")), warn => warn.Contains("root element with name `WrongTag`") || warn.Contains("should be `Record`"));

            ExpectErrors(() => Assert.AreEqual(0, Dec.Recorder.Read<int>(@"
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <data>4</data>
                </Record>
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <data>4</data>
                </Record>")), err => err.Contains("multiple root elements") || err.Contains("XmlException"));

            ExpectErrors(() => Assert.AreEqual(0, Dec.Recorder.Read<int>(@"
                <Record>
                  <recordFormatVersion>-2</recordFormatVersion>
                  <data>4</data>
                </Record>")), err => err.Contains("Unknown record format version -2"));

            ExpectErrors(() => Assert.AreEqual(0, Dec.Recorder.Read<int>(@"
                <Record>
                  <recordFormatVersion>9001</recordFormatVersion>
                  <data>4</data>
                </Record>")), err => err.Contains("Unknown record format version 9001"));

            ExpectErrors(() => Assert.AreEqual(0, Dec.Recorder.Read<int>(@"
                <Record>
                  <recordFormatVersion>lizard</recordFormatVersion>
                  <data>4</data>
                </Record>")), err => err.Contains("Unknown record format version lizard"));

            ExpectErrors(() => Assert.AreEqual(0, Dec.Recorder.Read<int>(@"
                <Record>
                </Record>")), err => err.Contains("Missing record format version") || err.Contains("No data element"));

            ExpectErrors(() => Assert.AreEqual(4, Dec.Recorder.Read<int>(@"
                <Record>
                    <data>4</data>
                </Record>")), err => err.Contains("Missing record format version"));

            ExpectErrors(() => Assert.AreEqual(4, Dec.Recorder.Read<int>(@"
                <Record>
                    <recordFormatVersion>1</recordFormatVersion>
                    <recordFormatVersion>1</recordFormatVersion>
                    <data>4</data>
                </Record>")), err => err.Contains("Multiple items named `recordFormatVersion`"));

            ExpectErrors(() => Assert.AreEqual(4, Dec.Recorder.Read<int>(@"
                <Record>
                    <recordFormatVersion>1</recordFormatVersion>
                    <data>4</data>
                    <data>4</data>
                </Record>")), err => err.Contains("Multiple items named `data`"));
        }
    }
}
