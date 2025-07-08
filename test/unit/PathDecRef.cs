
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
            Dec.Database.RegisterLookup(stub, new PathRoot("stub"));

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
    }
}