
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

            ExpectErrors(() => checksum1 = Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)));
            ExpectErrors(() => checksum2 = Dec.Recorder.Checksum(value2));

            ulong checksum3 = 0;

            ExpectErrors(() => checksum3 = Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)));

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

            ExpectErrors(() => checksum1 = Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)));
            ExpectErrors(() => checksum2 = Dec.Recorder.Checksum(value2));

            ulong checksum3 = 0;

            ExpectErrors(() => checksum3 = Dec.Recorder.Checksum(Dec.Recorder.Clone(value1)));

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
    }
}