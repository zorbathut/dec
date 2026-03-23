using System;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class CollectionArrayByte : Base
    {
        public class ByteArrayDec : Dec.Dec
        {
            public byte[] dataEmpty = null;
            public byte[] dataProvided = new byte[] { 1, 2, 3 };
        }

        [Test]
        public void BasicBase64([Values] ParserMode mode, [Values] bool forceFallbackArray)
        {
            Dec.Config.TestForceFallbackArray = forceFallbackArray;
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ByteArrayDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ByteArrayDec decName=""TestDec"">
                        <dataEmpty>ChQe</dataEmpty>
                        <dataProvided>YWJj</dataProvided>
                    </ByteArrayDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<ByteArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(new byte[] { 10, 20, 30 }, result.dataEmpty);
            Assert.AreEqual(new byte[] { 97, 98, 99 }, result.dataProvided);
        }

        [Test]
        public void BasicLiBased([Values] ParserMode mode, [Values] bool forceFallbackArray)
        {
            Dec.Config.TestForceFallbackArray = forceFallbackArray;
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ByteArrayDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ByteArrayDec decName=""TestDec"">
                        <dataEmpty>
                            <li>10</li>
                            <li>20</li>
                            <li>30</li>
                        </dataEmpty>
                        <dataProvided>
                            <li>11</li>
                            <li>21</li>
                            <li>31</li>
                        </dataProvided>
                    </ByteArrayDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<ByteArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(new byte[] { 10, 20, 30 }, result.dataEmpty);
            Assert.AreEqual(new byte[] { 11, 21, 31 }, result.dataProvided);
        }

        [Test]
        public void EmptyArray([Values] ParserMode mode, [Values] bool forceFallbackArray)
        {
            Dec.Config.TestForceFallbackArray = forceFallbackArray;
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ByteArrayDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ByteArrayDec decName=""TestDec"">
                        <dataEmpty></dataEmpty>
                        <dataProvided></dataProvided>
                    </ByteArrayDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<ByteArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(new byte[] { }, result.dataEmpty);
            Assert.AreEqual(new byte[] { }, result.dataProvided);
        }

        [Test]
        public void NullArray([Values] ParserMode mode, [Values] bool forceFallbackArray)
        {
            Dec.Config.TestForceFallbackArray = forceFallbackArray;
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ByteArrayDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ByteArrayDec decName=""TestDec"">
                        <dataEmpty null=""true""></dataEmpty>
                        <dataProvided null=""true""></dataProvided>
                    </ByteArrayDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<ByteArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.IsNull(result.dataEmpty);
            Assert.IsNull(result.dataProvided);
        }

        [Test]
        public void InvalidBase64([Values] ParserMode mode, [Values] bool forceFallbackArray)
        {
            Dec.Config.TestForceFallbackArray = forceFallbackArray;
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ByteArrayDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ByteArrayDec decName=""TestDec"">
                        <dataEmpty>invalid_base64!</dataEmpty>
                        <dataProvided>invalid_base64!</dataProvided>
                    </ByteArrayDec>
                </Decs>");
            ExpectErrors(() => parser.Finish());

            DoParserTests(mode);

            var result = Dec.Database<ByteArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            // Should fall back to existing data on error
            Assert.AreEqual(new byte[0], result.dataEmpty);
            Assert.AreEqual(new byte[] { 1, 2, 3 }, result.dataProvided);
        }

        [Test]
        public void LargeArray([Values] ParserMode mode, [Values] bool forceFallbackArray)
        {
            Dec.Config.TestForceFallbackArray = forceFallbackArray;
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ByteArrayDec) } });

            // Create a large byte array (1024 bytes)
            var largeData = new byte[1024];
            for (int i = 0; i < 1024; i++)
            {
                largeData[i] = (byte)(i % 256);
            }
            string base64Data = Convert.ToBase64String(largeData);

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <ByteArrayDec decName=""TestDec"">
                        <dataEmpty>{base64Data}</dataEmpty>
                    </ByteArrayDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<ByteArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(largeData, result.dataEmpty);
        }

        [Test]
        public void RecorderRoundTrip([Values] RecorderMode mode, [Values] bool forceFallbackArray)
        {
            Dec.Config.TestForceFallbackArray = forceFallbackArray;
            var testData = new byte[] { 0, 1, 127, 128, 255, 42, 100, 200 };

            var deserialized = DoRecorderRoundTrip(testData, mode);

            Assert.AreEqual(testData, deserialized);
        }

        [Test]
        public void RecorderRoundTripEmpty([Values] RecorderMode mode, [Values] bool forceFallbackArray)
        {
            Dec.Config.TestForceFallbackArray = forceFallbackArray;
            var testData = new byte[] { };

            var deserialized = DoRecorderRoundTrip(testData, mode);

            Assert.AreEqual(testData, deserialized);
        }

        [Test]
        public void RecorderRoundTripNull([ValuesExcept(RecorderMode.Validation)] RecorderMode mode, [Values] bool forceFallbackArray)
        {
            Dec.Config.TestForceFallbackArray = forceFallbackArray;
            byte[] testData = null;

            var deserialized = DoRecorderRoundTrip(testData, mode);

            Assert.AreEqual(testData, deserialized);
        }

        [Test]
        public void RecorderRoundTripLarge([Values] RecorderMode mode, [Values] bool forceFallbackArray)
        {
            Dec.Config.TestForceFallbackArray = forceFallbackArray;
            // Test with a large array to ensure efficiency
            var testData = new byte[2048];
            for (int i = 0; i < testData.Length; i++)
            {
                testData[i] = (byte)(i * 7 % 256); // Some pattern
            }

            var deserialized = DoRecorderRoundTrip(testData, mode);

            Assert.AreEqual(testData, deserialized);
        }

        public class ByteArrayRecordable : Dec.IRecordable
        {
            public byte[] data;
            public byte[] data2;

            public void Record(Dec.Recorder record)
            {
                record.Shared().Record(ref data, "data");
                record.Shared().Record(ref data2, "data2");
            }
        }

        [Test]
        public void RecorderArrayRef([ValuesExcept(RecorderMode.Simple)] RecorderMode mode, [Range(10, 12)] int length, [Values] bool forceFallbackArray)
        {
            Dec.Config.TestForceFallbackArray = forceFallbackArray;
            // Test that array reuse works correctly for reference preservation
            // This also verifies that we're doing our length calculations correctly
            var original = new ByteArrayRecordable();
            original.data = new byte[length];
            for (int i = 0; i < length; i++)
            {
                original.data[i] = (byte)(i * 2);
            }
            original.data2 = original.data; // Same reference

            var deserialized = DoRecorderRoundTrip(original, mode);

            Assert.AreEqual(original.data, deserialized.data);
            Assert.AreSame(deserialized.data, deserialized.data2);
        }

        [Test]
        public void MixedFormats([Values] ParserMode mode, [Values] bool forceFallbackArray)
        {
            Dec.Config.TestForceFallbackArray = forceFallbackArray;
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ByteArrayDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ByteArrayDec decName=""TestDec"">
                        <dataEmpty>ChQe</dataEmpty>
                        <dataProvided>
                            <li>15</li>
                            <li>25</li>
                            <li>35</li>
                        </dataProvided>
                    </ByteArrayDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<ByteArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(new byte[] { 10, 20, 30 }, result.dataEmpty);
            Assert.AreEqual(new byte[] { 15, 25, 35 }, result.dataProvided);
        }

        [Test]
        public void Base64Padding([Values] ParserMode mode, [Values] bool forceFallbackArray)
        {
            Dec.Config.TestForceFallbackArray = forceFallbackArray;
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ByteArrayDec) } });

            // Test different padding scenarios
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ByteArrayDec decName=""TestDec"">
                        <dataEmpty>YQ==</dataEmpty>
                        <dataProvided>YWI=</dataProvided>
                    </ByteArrayDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var result = Dec.Database<ByteArrayDec>.Get("TestDec");
            Assert.IsNotNull(result);

            Assert.AreEqual(new byte[] { 97 }, result.dataEmpty); // "a"
            Assert.AreEqual(new byte[] { 97, 98 }, result.dataProvided); // "ab"
        }
    }
}