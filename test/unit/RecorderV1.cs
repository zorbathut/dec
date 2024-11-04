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

            ExpectErrors(() => Assert.AreEqual(0, Dec.Recorder.Read<int>(@"")));

            ExpectErrors(() => Assert.AreEqual(0, Dec.Recorder.Read<int>(@"
                <MismatchedRoot>
                  <recordFormatVersion>1</recordFormatVersion>
                  <data>4</data>
                </Record>")));

            ExpectWarnings(() => Assert.AreEqual(4, Dec.Recorder.Read<int>(@"
                <WrongTag>
                  <recordFormatVersion>1</recordFormatVersion>
                  <data>4</data>
                </WrongTag>")));

            ExpectErrors(() => Assert.AreEqual(0, Dec.Recorder.Read<int>(@"
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <data>4</data>
                </Record>
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <data>4</data>
                </Record>")));

            ExpectErrors(() => Assert.AreEqual(0, Dec.Recorder.Read<int>(@"
                <Record>
                  <recordFormatVersion>-2</recordFormatVersion>
                  <data>4</data>
                </Record>")));

            ExpectErrors(() => Assert.AreEqual(0, Dec.Recorder.Read<int>(@"
                <Record>
                  <recordFormatVersion>9001</recordFormatVersion>
                  <data>4</data>
                </Record>")));

            ExpectErrors(() => Assert.AreEqual(0, Dec.Recorder.Read<int>(@"
                <Record>
                  <recordFormatVersion>lizard</recordFormatVersion>
                  <data>4</data>
                </Record>")));

            ExpectErrors(() => Assert.AreEqual(0, Dec.Recorder.Read<int>(@"
                <Record>
                </Record>")));

            ExpectErrors(() => Assert.AreEqual(4, Dec.Recorder.Read<int>(@"
                <Record>
                    <data>4</data>
                </Record>")));

            ExpectErrors(() => Assert.AreEqual(4, Dec.Recorder.Read<int>(@"
                <Record>
                    <recordFormatVersion>1</recordFormatVersion>
                    <recordFormatVersion>1</recordFormatVersion>
                    <data>4</data>
                </Record>")));

            ExpectErrors(() => Assert.AreEqual(4, Dec.Recorder.Read<int>(@"
                <Record>
                    <recordFormatVersion>1</recordFormatVersion>
                    <data>4</data>
                    <data>4</data>
                </Record>")));
        }
    }
}
