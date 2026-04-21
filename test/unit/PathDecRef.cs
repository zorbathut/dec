
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Dec;

namespace DecTest
{
    [TestFixture]
    public class PathDecRefStd : Base
    {
        private class PathDec : Dec.Dec
        {
            public StubRecordable member;
            public StubRecordable[] array;
            public StubRecordable[,] arrayMulti;
            public List<StubRecordable> list;
            public Dictionary<string, StubRecordable> dictionary;

            public override void PostLoad(Action<string> reporter)
            {
                base.PostLoad(reporter);

                Dec.Database.DecLookupEnable(member);
                Dec.Database.DecLookupEnable(array);
                foreach (var item in array)
                {
                    Dec.Database.DecLookupEnable(item);
                }
                Dec.Database.DecLookupEnable(arrayMulti);
                foreach (var item in arrayMulti)
                {
                    Dec.Database.DecLookupEnable(item);
                }
                Dec.Database.DecLookupEnable(list);
                foreach (var item in list)
                {
                    Dec.Database.DecLookupEnable(item);
                }
                Dec.Database.DecLookupEnable(dictionary);
                foreach (var item in dictionary.Values)
                {
                    Dec.Database.DecLookupEnable(item);
                }
            }
        }

        [SetUp]
        public void Setup()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(PathDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <PathDec decName=""TestDec"">
                        <member />
                        <array>
                            <li />
                            <li />
                        </array>
                        <arrayMulti>
                            <li>
                                <li />
                                <li />
                            </li>
                            <li>
                                <li />
                                <li />
                            </li>
                        </arrayMulti>
                        <list>
                            <li />
                            <li />
                        </list>
                        <dictionary>
                          <horse />
                          <dog />
                        </dictionary>
                    </PathDec>
                </Decs>");
            parser.Finish();
        }

        [Test]
        public void Member([ValuesExcept(RecorderMode.Simple)] RecorderMode mode)
        {
            var dec = Dec.Database<PathDec>.Get("TestDec");

            var item = dec.member;

            var newItem = DoRecorderRoundTrip(item, mode);

            Assert.AreSame(item, newItem);
        }

        [Test]
        public void Array([ValuesExcept(RecorderMode.Simple)] RecorderMode mode, [Values(0, 1)] int index)
        {
            var dec = Dec.Database<PathDec>.Get("TestDec");

            var item = dec.array[index];

            var newItem = DoRecorderRoundTrip(item, mode);

            Assert.AreSame(item, newItem);
        }

        [Test]
        public void MultiDimensionalArray([ValuesExcept(RecorderMode.Simple)] RecorderMode mode,
            [Values(0, 1)] int index1, [Values(0, 1)] int index2)
        {
            var dec = Dec.Database<PathDec>.Get("TestDec");

            var item = dec.arrayMulti[index1, index2];

            var newItem = DoRecorderRoundTrip(item, mode);

            Assert.AreSame(item, newItem);
        }

        [Test]
        public void List([ValuesExcept(RecorderMode.Simple)] RecorderMode mode, [Values(0, 1)] int index)
        {
            var dec = Dec.Database<PathDec>.Get("TestDec");

            var item = dec.list[index];

            var newItem = DoRecorderRoundTrip(item, mode);

            Assert.AreSame(item, newItem);
        }

        [Test]
        public void Dictionary([ValuesExcept(RecorderMode.Simple)] RecorderMode mode,
            [Values("horse", "dog")] string key)
        {
            var dec = Dec.Database<PathDec>.Get("TestDec");

            var item = dec.dictionary[key];

            var newItem = DoRecorderRoundTrip(item, mode);

            Assert.AreSame(item, newItem);
        }

        [Test]
        public void Register()
        {
            // This maybe shouldn't work.
            var stub = new StubRecordable();
            Dec.Database.DecLookupRegisterCustom(stub, new PathRoot("stub"));

            var newItem = DoRecorderRoundTrip(stub, RecorderMode.Pretty);

            Assert.AreSame(stub, newItem);
        }
    }

    [TestFixture]
    public class PathDecRef : Base
    {
        public class TypeHolderDec : Dec.Dec
        {
            public Type type;
        }

        [Test]
        public void TypeNotReffed([Values] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TypeHolderDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <TypeHolderDec decName=""Test"">
                        <type>int</type>
                    </TypeHolderDec>
                </Decs>");
            parser.Finish();

            var type = typeof(int);

            string serialized = Dec.Recorder.Write(type);

            // nuke the entire environment
            Clean();

            var deserialized = Dec.Recorder.Read<Type>(serialized);

            Assert.AreEqual(type, deserialized);
        }

        public class ArrayHolderDec : Dec.Dec
        {
            public int[] data;
        }

        [Test]
        public void NoAutoRef([Values] ParserMode parserMode, [Values] RecorderMode recorderMode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ArrayHolderDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ArrayHolderDec decName=""Test"">
                        <data><li>3</li></data>
                    </ArrayHolderDec>
                </Decs>");
            parser.Finish();

            DoParserTests(parserMode);

            var theArray = Dec.Database<ArrayHolderDec>.Get("Test").data;
            Assert.IsNotNull(theArray);

            var result = DoRecorderRoundTrip(theArray, recorderMode);

            Assert.AreNotSame(theArray, result);
            Assert.AreEqual(theArray, result);
        }

        public class ForbidDec : Dec.Dec
        {
            public StubRecordable member;
        }

        [Test]
        public void ForbidRecord([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var stub = new StubRecordable();
            Dec.Database.DecRegisterForbid(stub);

            var result = DoRecorderRoundTrip(stub, mode, expectWriteErrors: true, errorValidator: err => err.Contains("explicitly forbidden from recording"));

            Assert.IsNull(result);
        }

        [Test]
        public void ForbidThenRegister()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var stub = new StubRecordable();
            Dec.Database.DecRegisterForbid(stub);

            ExpectErrors(() => Dec.Database.DecLookupRegisterCustom(stub, new PathRoot("stub")), err => err.Contains("explicitly forbidden from recording"));
        }

        [Test]
        public void RegisterThenForbid()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var stub = new StubRecordable();
            Dec.Database.DecLookupRegisterCustom(stub, new PathRoot("stub"));

            ExpectErrors(() => Dec.Database.DecRegisterForbid(stub), err => err.Contains("already registered"));
        }

        [Test]
        public void ForbidThenEnable()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ForbidDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ForbidDec decName=""TestDec"">
                        <member />
                    </ForbidDec>
                </Decs>");
            parser.Finish();

            var dec = Dec.Database<ForbidDec>.Get("TestDec");
            Dec.Database.DecRegisterForbid(dec.member);

            ExpectErrors(() => Dec.Database.DecLookupEnable(dec.member), err => err.Contains("explicitly forbidden from recording"));
        }

        [Test]
        public void ForbidValueType()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            ExpectErrors(() => Dec.Database.DecRegisterForbid(42), err => err.Contains("value type"));
        }

        [Test]
        public void ForbidDecInstance()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(ForbidDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ForbidDec decName=""TestDec"">
                        <member />
                    </ForbidDec>
                </Decs>");
            parser.Finish();

            var dec = Dec.Database<ForbidDec>.Get("TestDec");
            ExpectErrors(() => Dec.Database.DecRegisterForbid(dec), err => err.Contains("is a Dec"));
        }
    }
}