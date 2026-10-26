
using Dec;
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class Checksum : Base
    {
        public void ChecksumDiffTests<T>(T value1, T value2)
        {
            List<string> differences = new List<string>();
            Dec.Recorder.ChecksumDiff(value1, value2, diff => differences.Add(diff));

            Assert.That(differences, Has.Count.EqualTo(1), "ChecksumDiff should report exactly one difference");
            Assert.That(differences[0], Does.Contain("Difference found"), "Report should indicate a difference was found");

            // Also test that identical values report no differences
            differences.Clear();
            Dec.Recorder.ChecksumDiff(value1, value1, diff => differences.Add(diff));
            Assert.That(differences, Has.Count.EqualTo(0), "ChecksumDiff should report no differences for identical values");
        }

        [Test]
        public void Integer()
        {
            int value1 = 5;
            int value2 = 10;

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different integers should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical integers should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        [Test]
        public void Long()
        {
            long value1 = 9876543210;
            long value2 = -9876543210;

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different longs should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical longs should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        [Test]
        public void ULong()
        {
            ulong value1 = 18446744073709551615UL; // max ulong
            ulong value2 = 9223372036854775808UL;  // half of max

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different ulongs should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical ulongs should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        [Test]
        public void UInt()
        {
            uint value1 = 4294967295;  // max uint
            uint value2 = 2147483648;  // half of max

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different uints should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical uints should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        [Test]
        public void Short()
        {
            short value1 = 32767;  // max short
            short value2 = -32768; // min short

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different shorts should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical shorts should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        [Test]
        public void UShort()
        {
            ushort value1 = 65535;  // max ushort
            ushort value2 = 32768;  // half of max

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different ushorts should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical ushorts should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        [Test]
        public void SByte()
        {
            sbyte value1 = 127;  // max sbyte
            sbyte value2 = -128; // min sbyte

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different sbytes should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical sbytes should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        [Test]
        public void Byte()
        {
            byte value1 = 255; // max byte
            byte value2 = 128; // half of max

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different bytes should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical bytes should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        [Test]
        public void Float()
        {
            float value1 = 3.14159f;
            float value2 = 2.71828f;

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different floats should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical floats should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        [Test]
        public void Double()
        {
            double value1 = 3.14159265358979;
            double value2 = 2.71828182845905;

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different doubles should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical doubles should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        [Test]
        public void Boolean()
        {
            bool value1 = true;
            bool value2 = false;

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different booleans should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical booleans should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        [Test]
        public void String()
        {
            string value1 = "Hello, world!";
            string value2 = "Hello, World!"; // Capital W

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different strings should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical strings should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        [Test]
        public void Enum()
        {
            GenericEnum value1 = GenericEnum.Alpha;
            GenericEnum value2 = GenericEnum.Beta;

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different enums should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical enums should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        public enum EnumByte : byte { Low = 1, High = 0xFF }
        public enum EnumSByte : sbyte { Low = -1, High = 1 }
        public enum EnumShort : short { Low = -1, High = short.MaxValue }
        public enum EnumUShort : ushort { Low = 1, High = 0xFFFF }
        public enum EnumUInt : uint { Low = 1, High = 0x80000000 }
        public enum EnumLong : long { Low = -1, High = long.MaxValue }
        public enum EnumULong : ulong { Low = 1, High = 0x8000000000000000 }

        public class EnumHolder<T> : Dec.IRecordable
        {
            public T value;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref value, nameof(value));
            }
        }

        private void AssertEnumChecksums<T>(T value1, T value2)
        {
            Assert.AreNotEqual(Dec.Recorder.Checksum(value1), Dec.Recorder.Checksum(value2));
            ChecksumDiffTests(value1, value2);

            var holder1 = new EnumHolder<T> { value = value1 };
            var holder2 = new EnumHolder<T> { value = value2 };
            Assert.AreNotEqual(Dec.Recorder.Checksum(holder1), Dec.Recorder.Checksum(holder2));
            Assert.AreEqual(Dec.Recorder.Checksum(holder1), Dec.Recorder.Checksum(Dec.Recorder.Clone(holder1)));
            ChecksumDiffTests(holder1, holder2);
        }

        [Test]
        public void EnumUnderlyingTypes()
        {
            AssertEnumChecksums(EnumByte.Low, EnumByte.High);
            AssertEnumChecksums(EnumSByte.Low, EnumSByte.High);
            AssertEnumChecksums(EnumShort.Low, EnumShort.High);
            AssertEnumChecksums(EnumUShort.Low, EnumUShort.High);
            AssertEnumChecksums(EnumUInt.Low, EnumUInt.High);
            AssertEnumChecksums(EnumLong.Low, EnumLong.High);
            AssertEnumChecksums(EnumULong.Low, EnumULong.High);
        }

        [Test]
        public void Type()
        {
            Type value1 = typeof(int);
            Type value2 = typeof(string);

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different types should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical types should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        [Test]
        public void DecObject()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StubDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StubDec decName=""TestDecA"" />
                    <StubDec decName=""TestDecB"" />
                </Decs>");
            parser.Finish();

            StubDec value1 = Dec.Database<StubDec>.Get("TestDecA");
            StubDec value2 = Dec.Database<StubDec>.Get("TestDecB");

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different decs should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical decs should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        class RefHolder : IRecordable
        {
            public RefHolder obj;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref obj, nameof(obj));
            }
        }

        [Test]
        public void ReferencedObject()
        {
            RefHolder[] value1 = new RefHolder[3] { new RefHolder(), new RefHolder(), new RefHolder() };
            RefHolder[] value2 = new RefHolder[3] { new RefHolder(), new RefHolder(), new RefHolder() };

            value1[0].obj = value1[1];
            value2[0].obj = value2[2];

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different decs should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical decs should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        [Test]
        public void ReferencedObjectCycle()
        {
            RefHolder[] value1 = new RefHolder[3] { new RefHolder(), new RefHolder(), new RefHolder() };
            RefHolder[] value2 = new RefHolder[3] { new RefHolder(), new RefHolder(), new RefHolder() };

            value1[0].obj = value1[1];
            value1[1].obj = value1[2];
            value1[2].obj = value1[0];

            value2[0].obj = value2[2];
            value2[1].obj = value2[0];
            value2[2].obj = value2[1];

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different decs should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical decs should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        class EqualsAlwaysPair : IRecordable
        {
            public StubRecordableEqualsAlways a;
            public StubRecordableEqualsAlways b;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref a, nameof(a));
                recorder.Shared().Record(ref b, nameof(b));
            }
        }

        [Test]
        public void DistinctEqualObjects()
        {
            // Same contents throughout; only whether one object fills both fields differs.
            var distinct = new EqualsAlwaysPair { a = new StubRecordableEqualsAlways { data = 1 }, b = new StubRecordableEqualsAlways { data = 1 } };
            var item = new StubRecordableEqualsAlways { data = 1 };
            var same = new EqualsAlwaysPair { a = item, b = item };

            ulong checksumDistinct = Dec.Recorder.Checksum(distinct);
            ulong checksumSame = Dec.Recorder.Checksum(same);

            Assert.AreNotEqual(checksumDistinct, checksumSame, "Two distinct objects should not checksum as one shared object");
            Assert.AreEqual(checksumDistinct, Dec.Recorder.Checksum(Dec.Recorder.Clone(distinct)), "Identical graphs should produce the same checksums");
            Assert.AreEqual(checksumSame, Dec.Recorder.Checksum(Dec.Recorder.Clone(same)), "Identical graphs should produce the same checksums");
        }

        class EqualsAlwaysItemHolder : IRecordable
        {
            public StubRecordableEqualsAlways item;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref item, nameof(item));
            }
        }

        [Test]
        public void DistinctEqualObjectsUnordered()
        {
            // The holders hash by identity, so the set holds both; their items are distinct objects that happen to compare equal, and inside an unordered context neither may be taken for a reference to the other.
            var value = new HashSet<EqualsAlwaysItemHolder>
            {
                new EqualsAlwaysItemHolder { item = new StubRecordableEqualsAlways { data = 1 } },
                new EqualsAlwaysItemHolder { item = new StubRecordableEqualsAlways { data = 2 } },
            };

            ulong checksum = Dec.Recorder.Checksum(value);

            Assert.AreEqual(checksum, Dec.Recorder.Checksum(Dec.Recorder.Clone(value)), "Identical graphs should produce the same checksums");
        }

        [Test]
        public void UnreferencedObjectUnordered()
        {
            HashSet<StubRecordableInt> value1 = new HashSet<StubRecordableInt> { new StubRecordableInt() { data = 1 }, new StubRecordableInt() { data = 2 }, new StubRecordableInt() { data = 3 } };
            HashSet<StubRecordableInt> value2 = new HashSet<StubRecordableInt> { new StubRecordableInt() { data = 1 }, new StubRecordableInt() { data = 3 }, new StubRecordableInt() { data = 3 } };

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different decs should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical decs should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        private class ReferencedChecksumTester : IRecordable
        {
            public HashSet<StubRecordable> prefix;
            public List<StubRecordable> ordered;
            public HashSet<StubRecordable> suffix;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref prefix, nameof(prefix));
                recorder.Record(ref ordered, nameof(ordered));
                recorder.Record(ref suffix, nameof(suffix));
            }
        }

        [Test]
        public void ReferencedObjectUnorderedFromOrdered()
        {
            ReferencedChecksumTester value1 = new ReferencedChecksumTester()
            {
                prefix = new HashSet<StubRecordable>(),
                ordered = new List<StubRecordable> { new StubRecordable() },
                suffix = new HashSet<StubRecordable>(),
            };
            value1.suffix.Add(value1.ordered[0]);
            ReferencedChecksumTester value2 = new ReferencedChecksumTester()
            {
                prefix = new HashSet<StubRecordable>(),
                ordered = new List<StubRecordable> { new StubRecordable(), new StubRecordable() },
                suffix = new HashSet<StubRecordable>(),
            };
            value2.suffix.Add(value1.ordered[0]);

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different decs should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical decs should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        [Test]
        public void ReferencedObjectOrderedFromUnordered()
        {
            ReferencedChecksumTester value1 = new ReferencedChecksumTester()
            {
                prefix = new HashSet<StubRecordable>(),
                ordered = new List<StubRecordable> { new StubRecordable() },
                suffix = new HashSet<StubRecordable>(),
            };
            value1.prefix.Add(value1.ordered[0]);
            ReferencedChecksumTester value2 = new ReferencedChecksumTester()
            {
                prefix = new HashSet<StubRecordable>(),
                ordered = new List<StubRecordable> { new StubRecordable(), new StubRecordable() },
                suffix = new HashSet<StubRecordable>(),
            };
            value2.prefix.Add(value2.ordered[0]);

            ulong checksum1 = 0;
            ulong checksum2 = 0;

            ExpectErrors(() => checksum1 = Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), err => err.Contains("reference object first seen in an unordered context"));
            ExpectErrors(() => checksum2 = Dec.Recorder.Checksum(value2), err => err.Contains("reference object first seen in an unordered context"));

            ulong checksum3 = 0;

            ExpectErrors(() => checksum3 = Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), err => err.Contains("reference object first seen in an unordered context"));

            Assert.AreNotEqual(checksum1, checksum2, "Different decs should produce different checksums");
            Assert.AreEqual(checksum1, checksum3, "Identical decs should produce the same checksums");
        }

        [Test]
        public void Array()
        {
            int[] value1 = new int[] { 1, 2, 3 };
            int[] value2 = new int[] { 1, 2, 4 };

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different arrays should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical arrays should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        [Test]
        public void List()
        {
            var value1 = new List<int> { 1, 2, 3 };
            var value2 = new List<int> { 1, 2, 4 };

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different lists should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical lists should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        [Test]
        public void Dictionary()
        {
            var value1 = new Dictionary<string, int> { { "a", 1 }, { "b", 2 } };
            var value2 = new Dictionary<string, int> { { "a", 1 }, { "b", 3 } };

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different dictionaries should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical dictionaries should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        [Test]
        public void HashSet()
        {
            var value1 = new HashSet<int> { 1, 2, 3 };
            var value2 = new HashSet<int> { 1, 2, 4 };

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different hashsets should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical hashsets should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        [Test]
        public void Queue()
        {
            var value1 = new Queue<int>(new[] { 1, 2, 3 });
            var value2 = new Queue<int>(new[] { 1, 2, 4 });

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different queues should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical queues should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        [Test]
        public void Stack()
        {
            var value1 = new Stack<int>(new[] { 1, 2, 3 });
            var value2 = new Stack<int>(new[] { 1, 2, 4 });

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different stacks should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical stacks should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        [Test]
        public void Tuple()
        {
            var value1 = System.Tuple.Create(1, "hello");
            var value2 = System.Tuple.Create(1, "world");

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different tuples should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical tuples should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        [Test]
        public void ValueTuple()
        {
            var value1 = (1, "hello");
            var value2 = (1, "world");

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different value tuples should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical value tuples should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        [Test]
        public void IRecordable()
        {
            var value1 = new StubRecordableInt() { data = 1 };
            var value2 = new StubRecordableInt() { data = 2 };

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different recordables should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical recordables should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        private class DictValueSharedRefTester : Dec.IRecordable
        {
            public Dictionary<string, StubRecordable> dict;
            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref dict, nameof(dict));
            }
        }

        [Test]
        public void DictionaryValueSharedRef()
        {
            var sharedObj = new StubRecordable();
            var value1 = new DictValueSharedRefTester
            {
                dict = new Dictionary<string, StubRecordable> { { "a", sharedObj }, { "b", sharedObj } },
            };
            var value2 = new DictValueSharedRefTester
            {
                dict = new Dictionary<string, StubRecordable> { { "a", new StubRecordable() }, { "b", new StubRecordable() } },
            };

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Shared vs non-shared dictionary values should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Cloned shared-ref dictionary should produce the same checksum");

            ChecksumDiffTests(value1, value2);
        }

        private class DictKeySharedRefTester : Dec.IRecordable
        {
            public Dictionary<StubRecordable, StubRecordable> dict;
            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref dict, nameof(dict));
            }
        }

        [Test]
        public void DictionaryKeySharedRef()
        {
            var keyA = new StubRecordable();
            var value1 = new DictKeySharedRefTester
            {
                dict = new Dictionary<StubRecordable, StubRecordable> { { keyA, keyA } },
            };
            var keyB = new StubRecordable();
            var keyC = new StubRecordable();
            var value2 = new DictKeySharedRefTester
            {
                dict = new Dictionary<StubRecordable, StubRecordable> { { keyB, keyB }, { keyC, keyC } },
            };

            ulong checksum1 = 0;
            ulong checksum2 = 0;

            ExpectErrors(() => checksum1 = Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), err => err.Contains("reference object first seen in an unordered context"));
            ExpectErrors(() => checksum2 = Dec.Recorder.Checksum(value2), err => err.Contains("reference object first seen in an unordered context"));

            ulong checksum3 = 0;

            ExpectErrors(() => checksum3 = Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), err => err.Contains("reference object first seen in an unordered context"));

            // these aren't really guaranteed due to how messy this is, but it will probably be true on a local machine, at least
            Assert.AreNotEqual(checksum1, checksum2, "Different dictionary-key-shared-ref objects should produce different checksums");
            Assert.AreEqual(checksum1, checksum3, "Identical dictionary-key-shared-ref objects should produce the same checksums");
        }

        private class DictCollidingKeysOrderTester : Dec.IRecordable
        {
            public Dictionary<StubRecordable, StubRecordableInt> dict;
            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref dict, nameof(dict));
            }
        }

        [Test]
        public void DictionaryCollidingKeysOrder()
        {
            // StubRecordable has an empty Record(), so all instances produce identical key checksums,
            // forcing the collision path in WriteDictionary. Two dictionaries with the same
            // (key_checksum, value_checksum) pairs but different insertion order should produce
            // the same checksum, since dictionary order is not semantically meaningful.
            var value1 = new DictCollidingKeysOrderTester
            {
                dict = new Dictionary<StubRecordable, StubRecordableInt>
                {
                    { new StubRecordable(), new StubRecordableInt { data = 1 } },
                    { new StubRecordable(), new StubRecordableInt { data = 2 } },
                },
            };
            DictCollidingKeysOrderTester value2;

            // keep resetting value2 until we get a different dictionary iteration order
            while (true)
            {
                value2 = new DictCollidingKeysOrderTester
                {
                    dict = new Dictionary<StubRecordable, StubRecordableInt>
                    {
                        { new StubRecordable(), new StubRecordableInt { data = 2 } },
                        { new StubRecordable(), new StubRecordableInt { data = 1 } },
                    },
                };

                if (value1.dict.First().Value != value2.dict.First().Value)
                {
                    break;
                }
            }

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreEqual(checksum1, checksum2, "Dictionaries with same content in different insertion order should have equal checksums");
        }

        class StubRecordableChildA : StubRecordable { }
        class StubRecordableChildB : StubRecordable { }

        [Test]
        public void RecordableHashset42()
        {
            // this tests for a problem that existed in initial implementations of HashSet's checksum
            var value1 = new HashSet<StubRecordable>() { new StubRecordableChildA(), new StubRecordableChildA(), new StubRecordableChildA(), new StubRecordableChildA(), new StubRecordableChildB(), new StubRecordableChildB() };
            var value2 = new HashSet<StubRecordable>() { new StubRecordableChildA(), new StubRecordableChildA(), new StubRecordableChildB(), new StubRecordableChildB(), new StubRecordableChildB(), new StubRecordableChildB() };

            ulong checksum1 = Dec.Recorder.Checksum(value1);
            ulong checksum2 = Dec.Recorder.Checksum(value2);

            Assert.AreNotEqual(checksum1, checksum2, "Different Hashset42's should produce different checksums");
            Assert.AreEqual(checksum1, Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)), "Identical Hashset42's should produce the same checksums");

            ChecksumDiffTests(value1, value2);
        }

        private class AsThisRecordable : Dec.IRecordable
        {
            public int data;

            public void Record(Dec.Recorder recorder)
            {
                recorder.RecordAsThis(ref data);
            }
        }

        [Test]
        public void MultidimArrayAsThisThenNull()
        {
            var value = new AsThisRecordable[,] { { new AsThisRecordable() { data = 1 }, null } };

            Dec.Recorder.Checksum(value);
        }

        private class SharedMultidimHolder : Dec.IRecordable
        {
            public int[,] grid;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref grid, nameof(grid));
            }
        }

        [Test]
        public void MultidimArraySharedValueTypes()
        {
            Dec.Recorder.Checksum(new SharedMultidimHolder { grid = new int[,] { { 1, 2 }, { 3, 4 } } });
        }

        [Test]
        public void MultidimArrayDiffLocation()
        {
            var baseline = new int[,] { { 1, 2 }, { 3, 4 } };
            var diffLate = new int[,] { { 1, 9 }, { 3, 4 } };
            var diffEarly = new int[,] { { 1, 2 }, { 9, 4 } };

            var reportsLate = new List<string>();
            var reportsEarly = new List<string>();
            Dec.Recorder.ChecksumDiff(baseline, diffLate, reportsLate.Add);
            Dec.Recorder.ChecksumDiff(baseline, diffEarly, reportsEarly.Add);

            Assert.That(reportsLate, Has.Count.EqualTo(1));
            Assert.That(reportsEarly, Has.Count.EqualTo(1));
            Assert.AreNotEqual(reportsLate[0], reportsEarly[0], "Differences at different multidim elements should report different locations");
        }

        private class PathProbe : Dec.IRecordable
        {
            public static List<string> Seen = new List<string>();

            public PathProbe child;

            public void Record(Dec.Recorder recorder)
            {
                Seen.Add(recorder.Context.PathString());
                recorder.Record(ref child, nameof(child));
            }
        }

        private class PathProbeHolder : Dec.IRecordable
        {
            public List<PathProbe> list;
            public Dictionary<string, PathProbe> dict;
            public Dictionary<PathProbe, int> keys;
            public HashSet<PathProbe> set;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref list, nameof(list));
                recorder.Record(ref dict, nameof(dict));
                recorder.Record(ref keys, nameof(keys));
                recorder.Record(ref set, nameof(set));
            }
        }

        [Test]
        public void ContextPaths()
        {
            var value = new PathProbeHolder
            {
                list = new List<PathProbe> { new PathProbe() { child = new PathProbe() }, new PathProbe() },
                dict = new Dictionary<string, PathProbe> { { "key", new PathProbe() } },
                keys = new Dictionary<PathProbe, int> { { new PathProbe(), 1 } },
                set = new HashSet<PathProbe> { new PathProbe() },
            };

            PathProbe.Seen.Clear();
            Dec.Recorder.Checksum(value);

            CollectionAssert.AreEqual(new List<string>
            {
                "ROOT.list[0]",
                "ROOT.list[0].child",
                "ROOT.list[1]",
                "ROOT.dict[key]",
                "ROOT.keys[KEY]",
                "ROOT.set[SETELEM]",
            }, PathProbe.Seen);
        }

        [Test]
        public void ReusedNodesForgetUnordered()
        {
            var shared = new StubRecordable();
            var value = new List<object> { new HashSet<StubRecordable> { new StubRecordable() }, new List<StubRecordable> { shared, shared } };

            Dec.Recorder.Checksum(value);
        }

        [Test]
        public void ReusedNodesForgetAsThis()
        {
            Dec.Recorder.Checksum(new List<AsThisRecordable> { new AsThisRecordable() { data = 1 }, null });
            Dec.Recorder.Checksum(new List<AsThisRecordable> { new AsThisRecordable() { data = 1 }, new AsThisRecordable() { data = 2 } });
        }

        private static long ChecksumAllocatedBytes<T>(T value)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            Dec.Recorder.Checksum(value);
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        private class ReferenceElement : Dec.IRecordable
        {
            public void Record(Dec.Recorder recorder)
            {
            }
        }

        [Test]
        public void AllocationPerReference()
        {
            // The elements have no fields, so only the reference table could grow with their count, and it's kept from the previous call on this thread.
            var small = Enumerable.Range(0, 1000).Select(i => new ReferenceElement()).ToList();
            var large = Enumerable.Range(0, 2000).Select(i => new ReferenceElement()).ToList();

            // Warm up caches and the JIT so that only the traversal itself is measured.
            ChecksumAllocatedBytes(large);
            ChecksumAllocatedBytes(small);

            AssertAllocatesLess(() => ChecksumAllocatedBytes(large) - ChecksumAllocatedBytes(small), 1000 * 8, "Checksum should not grow its reference table on every call");
        }

        [Test]
        public void AllocationPerElement()
        {
            var small = Enumerable.Range(0, 1000).Select(i => $"element{i}").ToList();
            var large = Enumerable.Range(0, 2000).Select(i => $"element{i}").ToList();

            // Warm up caches and the JIT so that only the traversal itself is measured.
            ChecksumAllocatedBytes(small);
            ChecksumAllocatedBytes(large);

            AssertAllocatesLess(() => ChecksumAllocatedBytes(large) - ChecksumAllocatedBytes(small), 1000, "Checksum should not allocate per list element");
        }

        private class StringFields : Dec.IRecordable
        {
            private static readonly string[] Labels = Enumerable.Range(0, 10).Select(i => $"field{i}").ToArray();

            public string[] values;

            public void Record(Dec.Recorder recorder)
            {
                for (int i = 0; i < values.Length; ++i)
                {
                    recorder.Record(ref values[i], Labels[i]);
                }
            }
        }

        [Test]
        public void AllocationPerField()
        {
            // The object count is the same on both sides, so the reference table is the same size on both; what's left is per-field cost, plus one more growth of the reused recorder's label set.
            var few = Enumerable.Range(0, 200).Select(i => new StringFields { values = Enumerable.Range(0, 5).Select(j => $"value{j}").ToArray() }).ToList();
            var many = Enumerable.Range(0, 200).Select(i => new StringFields { values = Enumerable.Range(0, 10).Select(j => $"value{j}").ToArray() }).ToList();

            // Warm up caches and the JIT so that only the traversal itself is measured.
            ChecksumAllocatedBytes(few);
            ChecksumAllocatedBytes(many);

            AssertAllocatesLess(() => ChecksumAllocatedBytes(many) - ChecksumAllocatedBytes(few), 1000, "Checksum should not allocate per recorded field");
        }

        private struct StringRecordable : Dec.IRecordable
        {
            public string value;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref value, nameof(value));
            }
        }

        [Test]
        public void AllocationPerRecord()
        {
            // Each element is boxed as the array hands it out, a struct holding one reference into the same 24 bytes as an int, and neither side touches the reference table; the recordable's string field boxes nothing on any build. So the difference is what a Record() call itself costs.
            var recordables = Enumerable.Range(0, 1000).Select(i => new StringRecordable { value = "value" }).ToArray();
            var ints = Enumerable.Range(0, 1000).ToArray();

            // Warm up caches and the JIT so that only the traversal itself is measured.
            ChecksumAllocatedBytes(recordables);
            ChecksumAllocatedBytes(ints);

            AssertAllocatesLess(() => ChecksumAllocatedBytes(recordables) - ChecksumAllocatedBytes(ints), 1000, "Checksum should not allocate per Record() call");
        }

        public class TypedFields : Dec.IRecordable
        {
            public int i;
            public uint ui;
            public long l;
            public ulong ul;
            public short s;
            public ushort us;
            public sbyte sb;
            public byte b;
            public float f;
            public double d;
            public bool flag;
            public char c;
            public GenericEnum e;
            public EnumByte eb;
            public EnumSByte esb;
            public EnumShort es;
            public EnumUShort eus;
            public EnumUInt eui;
            public EnumLong el;
            public EnumULong eul;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref i, nameof(i));
                recorder.Record(ref ui, nameof(ui));
                recorder.Record(ref l, nameof(l));
                recorder.Record(ref ul, nameof(ul));
                recorder.Record(ref s, nameof(s));
                recorder.Record(ref us, nameof(us));
                recorder.Record(ref sb, nameof(sb));
                recorder.Record(ref b, nameof(b));
                recorder.Record(ref f, nameof(f));
                recorder.Record(ref d, nameof(d));
                recorder.Record(ref flag, nameof(flag));
                recorder.Record(ref c, nameof(c));
                recorder.Record(ref e, nameof(e));
                recorder.Record(ref eb, nameof(eb));
                recorder.Record(ref esb, nameof(esb));
                recorder.Record(ref es, nameof(es));
                recorder.Record(ref eus, nameof(eus));
                recorder.Record(ref eui, nameof(eui));
                recorder.Record(ref el, nameof(el));
                recorder.Record(ref eul, nameof(eul));
            }
        }

        private static readonly TypedFields[] TypedFieldsCases =
        {
            new TypedFields(),
            new TypedFields { i = -1, ui = uint.MaxValue, l = -1, ul = ulong.MaxValue, s = -1, us = ushort.MaxValue, sb = -1, b = byte.MaxValue, f = -1f, d = -1.0, flag = true, c = char.MaxValue, e = GenericEnum.Beta, eb = EnumByte.High, esb = EnumSByte.Low, es = EnumShort.Low, eus = EnumUShort.High, eui = EnumUInt.High, el = EnumLong.Low, eul = EnumULong.High },
            new TypedFields { i = int.MinValue, ui = 0x80000000, l = long.MinValue, ul = 0x8000000000000000, s = short.MinValue, us = 0x8000, sb = sbyte.MinValue, b = 0x80, f = float.NegativeInfinity, d = double.NaN, c = 'x', el = EnumLong.High },
            new TypedFields { f = float.NaN, d = -0.0 },
        };

        private static ulong ChecksumWithFallback(object value, bool fallback)
        {
            Dec.Config.TestForceFallbackEmit = fallback;
            try
            {
                return Dec.Recorder.Checksum(value);
            }
            finally
            {
                Dec.Config.TestForceFallbackEmit = false;
            }
        }

        [Test]
        public void TypedFieldsMatchGeneral([ValueSource(nameof(TypedFieldsCases))] TypedFields value)
        {
            Assert.AreEqual(ChecksumWithFallback(value, true), ChecksumWithFallback(value, false));
        }

        [Test]
        public void TypedFieldsDiff([Values] bool fallback)
        {
            Dec.Config.TestForceFallbackEmit = fallback;

            // Each pair differs in that one field alone.
            foreach (var field in new[] { nameof(TypedFields.i), nameof(TypedFields.f), nameof(TypedFields.flag), nameof(TypedFields.c), nameof(TypedFields.eb), nameof(TypedFields.eul) })
            {
                var value1 = new TypedFields();
                var value2 = new TypedFields();
                var fieldInfo = typeof(TypedFields).GetField(field);
                fieldInfo.SetValue(value2, fieldInfo.GetValue(TypedFieldsCases[1]));

                var differences = new List<string>();
                Dec.Recorder.ChecksumDiff(value1, value2, diff => differences.Add(diff));

                Assert.AreEqual(1, differences.Count, field);
                Assert.That(differences[0], Does.Contain($".{field}`"), field);
            }
        }

        public class CountedValues<T> : Dec.IRecordable
        {
            private static readonly string[] Labels = Enumerable.Range(0, 10).Select(i => $"field{i}").ToArray();

            public int count;
            public T[] values = new T[10];

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref count, nameof(count));
                for (int i = 0; i < count; ++i)
                {
                    recorder.Record(ref values[i], Labels[i]);
                }
            }
        }

        private static void AssertNoAllocationPerValueField<T>(Func<int, T> make)
        {
            // As AllocationPerField, over value types: the object count is the same on both sides, so the difference is what the extra fields cost.
            List<CountedValues<T>> Make(int count)
            {
                return Enumerable.Range(0, 200).Select(i => new CountedValues<T> { count = count, values = Enumerable.Range(0, 10).Select(make).ToArray() }).ToList();
            }
            var few = Make(5);
            var many = Make(10);

            ChecksumAllocatedBytes(few);
            ChecksumAllocatedBytes(many);

            AssertAllocatesLess(() => ChecksumAllocatedBytes(many) - ChecksumAllocatedBytes(few), 1000, "Checksum should not allocate per recorded value-type field");
        }

        public enum ValueFieldKind
        {
            Int,
            Long,
            Double,
            Bool,
            Enum,
            RecordedStruct,
        }

        public struct StringStruct : Dec.IRecordable
        {
            public string text;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref text, nameof(text));
            }
        }

        [Test]
        public void AllocationPerValueField([Values] ValueFieldKind kind)
        {
            // The netstandard2.1 build, which the netcoreapp3.1 tests run against, boxes these. Applies() needs Dec's converters set up, which any Recorder call does.
            Dec.Recorder.Checksum(0);
            if (kind == ValueFieldKind.RecordedStruct ? !Dec.RecordableStruct<StringStruct>.Applies() : !Dec.ChecksumTyped<int>.Applies)
            {
                Assert.Ignore("Unboxed checksums are unavailable on this build");
            }

            switch (kind)
            {
                case ValueFieldKind.Int:
                    AssertNoAllocationPerValueField(i => i);
                    break;
                case ValueFieldKind.Long:
                    AssertNoAllocationPerValueField(i => (long)i << 33);
                    break;
                case ValueFieldKind.Double:
                    AssertNoAllocationPerValueField(i => i * 0.5);
                    break;
                case ValueFieldKind.Bool:
                    AssertNoAllocationPerValueField(i => i % 2 == 0);
                    break;
                case ValueFieldKind.Enum:
                    AssertNoAllocationPerValueField(i => (EnumByte)(i % 2 + 1));
                    break;
                case ValueFieldKind.RecordedStruct:
                    AssertNoAllocationPerValueField(i => new StringStruct { text = "text" });
                    break;
                default:
                    Assert.Fail($"No allocation case for {kind}");
                    break;
            }
        }

        public struct InnerStruct : Dec.IRecordable
        {
            public int number;
            public StubRecordable target;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref number, nameof(number));
                recorder.Shared().Record(ref target, nameof(target));
            }
        }

        public struct OuterStruct : Dec.IRecordable
        {
            public string text;
            public InnerStruct inner;
            public StubRecordable target;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref text, nameof(text));
                recorder.Record(ref inner, nameof(inner));
                recorder.Shared().Record(ref target, nameof(target));
            }
        }

        public class StructHolder : Dec.IRecordable
        {
            public OuterStruct a;
            public OuterStruct b;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref a, nameof(a));
                recorder.Record(ref b, nameof(b));
            }
        }

        private static StructHolder MakeStructHolder(int number)
        {
            var shared = new StubRecordable();
            return new StructHolder
            {
                a = new OuterStruct { text = "a", inner = new InnerStruct { number = number, target = shared }, target = shared },
                b = new OuterStruct { text = "b", inner = new InnerStruct { number = 2, target = new StubRecordable() }, target = shared },
            };
        }

        [Test]
        public void RecordedStructMatchesGeneral()
        {
            var value = MakeStructHolder(1);
            Assert.AreEqual(ChecksumWithFallback(value, true), ChecksumWithFallback(value, false));
        }

        [Test]
        public void RecordedStructDiff([Values] bool fallback)
        {
            Dec.Config.TestForceFallbackEmit = fallback;

            var differences = new List<string>();
            Dec.Recorder.ChecksumDiff(MakeStructHolder(1), MakeStructHolder(3), diff => differences.Add(diff));

            Assert.AreEqual(1, differences.Count);
            Assert.That(differences[0], Does.Contain(".a.inner.number`"));
        }

        [Test]
        public void TypedFieldsUnordered()
        {
            // Hashed in an unordered context, as set elements and dictionary keys are.
            var set = new HashSet<TypedFields>(TypedFieldsCases);
            var dict = TypedFieldsCases.ToDictionary(value => value, value => 1);

            Assert.AreEqual(ChecksumWithFallback(set, true), ChecksumWithFallback(set, false));
            Assert.AreEqual(ChecksumWithFallback(dict, true), ChecksumWithFallback(dict, false));
        }

        public class Converted
        {
            public int i;
            public float f;
            public EnumByte e;
            public StringStruct s;
        }

        public class ConvertedConverter : Dec.ConverterRecord<Converted>
        {
            public override void Record(ref Converted input, Dec.Recorder recorder)
            {
                recorder.Record(ref input.i, nameof(input.i));
                recorder.Record(ref input.f, nameof(input.f));
                recorder.Record(ref input.e, nameof(input.e));
                recorder.Record(ref input.s, nameof(input.s));
            }
        }

        [Test]
        public void ConverterRecordFieldsMatchGeneral()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitConverters = new Type[] { typeof(ConvertedConverter) } });

            var value = new Converted { i = -3, f = -1.5f, e = EnumByte.High, s = new StringStruct { text = "text" } };
            Assert.AreEqual(ChecksumWithFallback(value, true), ChecksumWithFallback(value, false));
        }

        public struct ConditionalStruct : Dec.IConditionalRecordable
        {
            public int number;

            public bool ShouldRecord(Dec.Recorder.IUserSettings userSettings)
            {
                return true;
            }

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref number, nameof(number));
            }
        }

        public class SharedValueFields : Dec.IRecordable
        {
            public int number;
            public StringStruct recorded;
            public ConditionalStruct conditional;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref number, nameof(number));
                recorder.Shared().Record(ref recorded, nameof(recorded));
                recorder.Record(ref conditional, nameof(conditional));
            }
        }

        [Test]
        public void SharedValueFieldsWarn()
        {
            var value = new SharedValueFields { number = 4, recorded = new StringStruct { text = "text" }, conditional = new ConditionalStruct { number = 5 } };

            var checksums = new List<ulong>();
            foreach (var fallback in new[] { true, false })
            {
                int warnings = 0;
                ExpectWarnings(() => checksums.Add(ChecksumWithFallback(value, fallback)), wrn => { ++warnings; return wrn.Contains("Shared"); });
                Assert.AreEqual(2, warnings);
            }

            Assert.AreEqual(checksums[0], checksums[1]);
        }
    }
}