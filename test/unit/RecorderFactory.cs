using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace DecTest
{
    [TestFixture]
    public class RecorderFactory : Base
    {
        public class Factoried : Dec.IRecordable
        {
            // this is treated like a default value that's specified in the constructor
            public int value;

            public void Record(Dec.Recorder recorder)
            {
            }
        }

        public class FactoriedDerived : Factoried
        {
        }

        public class FactoriedDerivedSibling : Factoried
        {
        }

        public class FactoriedDerivedDouble : FactoriedDerived
        {
        }

        public class BasicCore : Dec.IRecordable
        {
            public Factoried one;
            public Factoried two;

            public void Record(Dec.Recorder recorder)
            {
                recorder
                    .WithFactory(new Dictionary<Type, Func<Type, object>>() { { typeof(Factoried), _ => new Factoried() { value = 1 } } })
                    .Record(ref one, "one");

                recorder
                    .WithFactory(new Dictionary<Type, Func<Type, object>>() { { typeof(Factoried), _ => new Factoried() { value = 2 } } })
                    .Record(ref two, "two");
            }
        }

        [Test]
        public void Basic([Values] RecorderMode mode)
        {
            var element = new BasicCore();

            // Need to create these otherwise the factory won't even be triggered
            element.one = new Factoried();
            element.two = new Factoried();

            var deserialized = DoRecorderRoundTrip(element, mode);

            Assert.AreEqual(1, deserialized.one.value);
            Assert.AreEqual(2, deserialized.two.value);
        }

        public class ParameterReuseCore : Dec.IRecordable
        {
            public Factoried one;
            public Factoried two;

            public void Record(Dec.Recorder recorder)
            {
                var parameters = recorder
                    .WithFactory(new Dictionary<Type, Func<Type, object>>() { { typeof(Factoried), _ => new Factoried() { value = 42 } } });

                parameters.Record(ref one, "one");
                parameters.Record(ref two, "two");
            }
        }

        [Test]
        public void ParameterReuse([Values] RecorderMode mode)
        {
            var element = new ParameterReuseCore();

            // Need to create these dynamically otherwise the factory won't even be triggered
            element.one = new Factoried();
            element.two = new Factoried();

            var deserialized = DoRecorderRoundTrip(element, mode);

            Assert.AreEqual(42, deserialized.one.value);
            Assert.AreEqual(42, deserialized.two.value);
        }

        public class ParameterOverrideCore : Dec.IRecordable
        {
            public Factoried one;
            public Factoried two;

            public void Record(Dec.Recorder recorder)
            {
                recorder
                    .WithFactory(new Dictionary<Type, Func<Type, object>>() { { typeof(Factoried), _ => new Factoried() { value = 1 } } })
                    .WithFactory(new Dictionary<Type, Func<Type, object>>() { { typeof(Factoried), _ => new Factoried() { value = -1 } } })
                    .Record(ref one, "one");

                recorder
                    .WithFactory(new Dictionary<Type, Func<Type, object>>() { { typeof(Factoried), _ => new Factoried() { value = 2 } } })
                    .WithFactory(new Dictionary<Type, Func<Type, object>>() { { typeof(Factoried), _ => new Factoried() { value = -2 } } })
                    .Record(ref two, "two");
            }
        }

        [Test]
        public void ParameterOverride([Values] RecorderMode mode)
        {
            var element = new ParameterOverrideCore();

            // Need to create these dynamically otherwise the factory won't even be triggered
            element.one = new Factoried();
            element.two = new Factoried();

            var deserialized = DoRecorderRoundTrip(element, mode, expectWriteErrors: true, expectReadErrors: true);

            Assert.AreEqual(-1, deserialized.one.value);
            Assert.AreEqual(-2, deserialized.two.value);
        }

        public enum InheritanceTestFactoryProvided
        {
            Exact,
            Base,
        }

        public enum InheritanceTestResult
        {
            Exact,
            Derived,
            ParentError, // returns something that conforms to the factory id, but that is a parent of the thing requested
            SiblingError, // returns something that conforms to the factory id, but that is a sibling of the thing requested
            InvalidError, // returns something that doesn't conform to the factory id
        }

        public class InheritanceCore : Dec.IRecordable
        {
            public FactoriedDerived element;

            public static InheritanceTestFactoryProvided provided_setting;
            public static InheritanceTestResult result_setting;

            public void Record(Dec.Recorder recorder)
            {
                Type proposed = provided_setting == InheritanceTestFactoryProvided.Exact ? typeof(FactoriedDerived) : typeof(Factoried);

                recorder
                    .WithFactory(new Dictionary<Type, Func<Type, object>>() { { proposed, _ => {
                        if (result_setting == InheritanceTestResult.Exact)
                        {
                            return new FactoriedDerived() { value = 42 };
                        }
                        else if (result_setting == InheritanceTestResult.Derived)
                        {
                            return new FactoriedDerivedDouble() { value = 42 };
                        }
                        else if (result_setting == InheritanceTestResult.ParentError)
                        {
                            return new Factoried() { value = 42 };
                        }
                        else if (result_setting == InheritanceTestResult.SiblingError)
                        {
                            return new FactoriedDerivedSibling() { value = 42 };
                        }
                        else if (result_setting == InheritanceTestResult.InvalidError)
                        {
                            return "42";
                        }

                        return null;
                    } } })
                    .Record(ref element, "element");
            }
        }

        [Test]
        public void InheritanceOptions([Values] RecorderMode mode, [Values] InheritanceTestFactoryProvided provided, [Values] InheritanceTestResult result)
        {
            if (provided == InheritanceTestFactoryProvided.Exact)
            {
                // These don't even make sense
                if (result == InheritanceTestResult.ParentError || result == InheritanceTestResult.SiblingError)
                {
                    return;
                }
            }

            InheritanceCore.provided_setting = provided;
            InheritanceCore.result_setting = result;

            var element = new InheritanceCore();

            // Need to create these dynamically otherwise the factory won't even be triggered
            element.element = new FactoriedDerived();

            bool expectErrors =
                (result == InheritanceTestResult.ParentError) ||
                (result == InheritanceTestResult.SiblingError) ||
                (result == InheritanceTestResult.InvalidError);

            var deserialized = DoRecorderRoundTrip(element, mode, expectReadErrors: expectErrors);

            if (result == InheritanceTestResult.Derived)
            {
                Assert.AreEqual(typeof(FactoriedDerivedDouble), deserialized.element.GetType());
            }
            else
            {
                // either because it's correct, or because it defaulted to that as a fallback
                Assert.AreEqual(typeof(FactoriedDerived), deserialized.element.GetType());
            }

            if (!expectErrors)
            {
                Assert.AreEqual(42, deserialized.element.value);
            }
            else
            {
                Assert.AreEqual(0, deserialized.element.value);
            }
        }

        public class NullCore : Dec.IRecordable
        {
            public Factoried element;

            public void Record(Dec.Recorder recorder)
            {
                recorder
                    .WithFactory(new Dictionary<Type, Func<Type, object>>() { { typeof(Factoried), _ => null } })
                    .Record(ref element, "element");
            }
        }

        [Test]
        public void Null([Values] RecorderMode mode)
        {
            var element = new NullCore();

            // Need to create these otherwise the factory won't even be triggered
            element.element = new Factoried();

            var deserialized = DoRecorderRoundTrip(element, mode);

            Assert.IsNotNull(deserialized.element);
        }

        public class TraversalCore : Dec.IRecordable
        {
            public Factoried norm;
            public FactoriedDerived derived;
            public FactoriedDerivedDouble deriveddouble;

            public void Record(Dec.Recorder recorder)
            {
                var factory = recorder.WithFactory(new Dictionary<Type, Func<Type, object>>()
                {
                    { typeof(Factoried), _ => new Factoried() { value = 5 } },
                    { typeof(FactoriedDerived), _ => new FactoriedDerived() { value = 10 } },
                    { typeof(FactoriedDerivedDouble), _ => new FactoriedDerivedDouble() { value = 15 } },
                    { typeof(string), _ => "" },
                });

                factory.Record(ref norm, "norm");
                factory.Record(ref derived, "derived");
                factory.Record(ref deriveddouble, "deriveddouble");
            }
        }

        [Test]
        public void Traversal([Values] RecorderMode mode)
        {
            var element = new TraversalCore();

            // Need to create these otherwise the factory won't even be triggered
            element.norm = new Factoried();
            element.derived = new FactoriedDerived();
            element.deriveddouble = new FactoriedDerivedDouble();

            var deserialized = DoRecorderRoundTrip(element, mode);

            Assert.AreEqual(typeof(Factoried), deserialized.norm.GetType());
            Assert.AreEqual(5, deserialized.norm.value);
            Assert.AreEqual(typeof(FactoriedDerived), deserialized.derived.GetType());
            Assert.AreEqual(10, deserialized.derived.value);
            Assert.AreEqual(typeof(FactoriedDerivedDouble), deserialized.deriveddouble.GetType());
            Assert.AreEqual(15, deserialized.deriveddouble.value);
        }

        [Test]
        public void PreSpecialized([Values] RecorderMode mode)
        {
            var element = new TraversalCore();

            // Need to create these otherwise the factory won't even be triggered
            element.norm = new FactoriedDerivedDouble();
            element.derived = new FactoriedDerivedDouble();
            element.deriveddouble = new FactoriedDerivedDouble();

            var deserialized = DoRecorderRoundTrip(element, mode);

            Assert.AreEqual(typeof(FactoriedDerivedDouble), deserialized.norm.GetType());
            Assert.AreEqual(15, deserialized.norm.value);
            Assert.AreEqual(typeof(FactoriedDerivedDouble), deserialized.derived.GetType());
            Assert.AreEqual(15, deserialized.derived.value);
            Assert.AreEqual(typeof(FactoriedDerivedDouble), deserialized.deriveddouble.GetType());
            Assert.AreEqual(15, deserialized.deriveddouble.value);
        }

        public class Recursive : Dec.IRecordable
        {
            public Recursive element;
            public int value = 4;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref element, "element");
            }
        }

        public class RecursiveRoot : Dec.IRecordable
        {
            public Recursive element;

            public void Record(Dec.Recorder recorder)
            {
                recorder
                    .WithFactory(new Dictionary<Type, Func<Type, object>>() { { typeof(Recursive), _ => new Recursive() { value = 42 } } })
                    .Record(ref element, "element");
            }
        }

        [Test]
        public void RecursiveRemoval([Values] RecorderMode mode)
        {
            var element = new RecursiveRoot();

            element.element = new Recursive();
            element.element.element = new Recursive();

            var deserialized = DoRecorderRoundTrip(element, mode);

            Assert.AreEqual(42, deserialized.element.value);
            Assert.AreEqual(4, deserialized.element.element.value);
        }

        public class SelectiveLeaf : Dec.IRecordable
        {
            public int neither = 1;
            public int factory = 2;
            public int record = 3;
            public int factoryrecord = 4;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref record, "record");
                recorder.Record(ref factoryrecord, "factoryrecord");
            }
        }

        public class SelectiveRoot : Dec.IRecordable
        {
            public SelectiveLeaf element;

            public void Record(Dec.Recorder recorder)
            {
                recorder
                    .WithFactory(new Dictionary<Type, Func<Type, object>>() {
                        { typeof(SelectiveLeaf), _ => new SelectiveLeaf() { factory = 12, factoryrecord = 14 } }
                    })
                    .Record(ref element, "element");
            }
        }

        [Test]
        public void Selective([Values] RecorderMode mode)
        {
            var element = new SelectiveRoot();

            element.element = new SelectiveLeaf();
            element.element.neither = 21;
            element.element.factory = 22;
            element.element.record = 23;
            element.element.factoryrecord = 24;

            var deserialized = DoRecorderRoundTrip(element, mode);

            Assert.AreEqual(1, deserialized.element.neither);   // reset to default
            Assert.AreEqual(12, deserialized.element.factory);  // initialized by factory
            Assert.AreEqual(23, deserialized.element.record);  // initialized by record
            Assert.AreEqual(24, deserialized.element.factoryrecord);  // initialized by factory, overwritten by record
        }

        public class RecorderHybrid : Dec.IRecordable
        {
            public int nonrecorded;
            public int recorded;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref recorded, "recorded");
            }
        }

        public class SharedWriteCode : Dec.IRecordable
        {
            public RecorderHybrid one;
            public RecorderHybrid two;

            public void Record(Dec.Recorder recorder)
            {
                var parameters = recorder.WithFactory(new Dictionary<Type, Func<Type, object>>()
                {
                    { typeof(RecorderHybrid), _ => new RecorderHybrid() { nonrecorded = 100, recorded = 200 } },
                });

                parameters.Record(ref one, "one");
                parameters.Record(ref two, "two");
            }
        }

        [Test]
        public void SharedWrite([Values] RecorderMode mode)
        {
            var element = new SharedWriteCode();

            element.one = new RecorderHybrid();
            element.two = element.one;

            element.one.nonrecorded = 11;
            element.one.recorded = 12;

            var deserialized = DoRecorderRoundTrip(element, mode, expectWriteErrors: mode != RecorderMode.Clone);

            if (mode != RecorderMode.Clone)
            {
                Assert.IsNotNull(deserialized.one);
                Assert.AreEqual(100, deserialized.one.nonrecorded);
                Assert.AreEqual(12, deserialized.one.recorded);
                Assert.IsNotNull(deserialized.two);
                Assert.AreEqual(100, deserialized.two.nonrecorded);
                Assert.AreEqual(200, deserialized.two.recorded);
            }
            else
            {
                Assert.AreSame(deserialized.one, deserialized.two);
                Assert.AreEqual(100, deserialized.one.nonrecorded);
                Assert.AreEqual(12, deserialized.one.recorded);
            }
        }

        [Test]
        public void FactoryRefRead()
        {
            // This is an error because a .WithFactory() method is being called, which precludes references.

            string serialized = @"
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <refs>
                    <Ref id=""ref00000"" class=""DecTest.RecorderFactory.RecorderHybrid"">
                        <recorded>99</recorded>
                    </Ref>
                  </refs>
                  <data>
                    <one ref=""ref00000"" />
                    <two>
                        <recorded>42</recorded>
                    </two>
                  </data>
                </Record>";

            SharedWriteCode deserialized = null;
            ExpectErrors(() => deserialized = Dec.Recorder.Read<SharedWriteCode>(serialized));

            Assert.IsNotNull(deserialized.one);
            Assert.AreEqual(0, deserialized.one.nonrecorded);
            Assert.AreEqual(99, deserialized.one.recorded);
            Assert.IsNotNull(deserialized.two);
            Assert.AreEqual(100, deserialized.two.nonrecorded);
            Assert.AreEqual(42, deserialized.two.recorded);
        }

        public class RecursiveKillerItem : Dec.IRecordable
        {
            public bool makeFactory = false;
            public bool usedFactory = false;

            public RecursiveKillerItem element;

            public RecursiveKillerItem() { }
            public RecursiveKillerItem(int depthToFactory, bool doFactory)
            {
                makeFactory = doFactory && depthToFactory == 0;
                if (depthToFactory > -10)
                {
                    element = new RecursiveKillerItem(depthToFactory - 1, doFactory);
                }
            }

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref makeFactory, "makeFactory");

                if (makeFactory)
                {
                    recorder.WithFactory(new Dictionary<Type, Func<Type, object>>() {
                        { typeof(RecursiveKillerItem), _ => new RecursiveKillerItem() { usedFactory = true } },
                    }).Record(ref element, "element");
                }
                else
                {
                    recorder.Record(ref element, "element");
                }
            }
        }

        [Test]
        public void RecursiveKiller([Values] RecorderMode mode)
        {
            bool gotRef = false;
            for (int depth = 15; depth < 25; ++depth)
            {
                // Make sure we get a ref mode eventually
                {
                    var testbed = new RecursiveKillerItem(depth, false);

                    var result = DoRecorderRoundTrip(testbed, mode, testSerializedResult: serialized => gotRef |= serialized.Contains("<refs>"));

                    int detectedDepth = 0;
                    while (result != null)
                    {
                        ++detectedDepth;
                        result = result.element;
                    }

                    Assert.AreEqual(depth + 11, detectedDepth);
                }

                // Make sure we deserialize without error
                {
                    var testbed = new RecursiveKillerItem(depth, true);

                    var result = DoRecorderRoundTrip(testbed, mode);

                    int detectedDepth = 0;
                    int foundFactory = 0;
                    while (result != null)
                    {
                        ++detectedDepth;
                        if (result.makeFactory)
                        {
                            Assert.IsTrue(result.element.usedFactory);
                            foundFactory++;
                        }
                        else
                        {
                            Assert.IsTrue(result.element == null || !result.element.usedFactory);
                        }
                        result = result.element;
                    }

                    Assert.AreEqual(depth + 11, detectedDepth);
                    Assert.AreEqual(1, foundFactory);
                }
            }
        }

        public class RecursiveListKillerItem : Dec.IRecordable
        {
            public bool makeFactory = false;
            public bool usedFactory = false;

            public List<List<RecursiveListKillerItem>> element;

            public RecursiveListKillerItem() { }
            public RecursiveListKillerItem(int depthToFactory, bool doFactory)
            {
                makeFactory = doFactory && depthToFactory == 0;
                if (depthToFactory > -3)
                {
                    element = new List<List<RecursiveListKillerItem>>();
                    element.Add(new List<RecursiveListKillerItem>());
                    element[0].Add(new RecursiveListKillerItem(depthToFactory - 1, doFactory));
                }
            }

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref makeFactory, "makeFactory");

                if (makeFactory)
                {
                    recorder.WithFactory(new Dictionary<Type, Func<Type, object>>() {
                        { typeof(RecursiveListKillerItem), _ => new RecursiveListKillerItem() { usedFactory = true } },
                    }).Record(ref element, "element");
                }
                else
                {
                    recorder.Record(ref element, "element");
                }
            }
        }

        [Test]
        public void RecursiveListKiller([Values] RecorderMode mode)
        {
            bool gotRef = false;
            for (int depth = 4; depth < 8; ++depth)
            {
                // Make sure we get a ref mode eventually
                {
                    var testbed = new RecursiveListKillerItem(depth, false);

                    var result = DoRecorderRoundTrip(testbed, mode, testSerializedResult: serialized => gotRef |= serialized.Contains("<refs>"));

                    int detectedDepth = 0;
                    while (result != null)
                    {
                        ++detectedDepth;

                        if (result.element != null)
                        {
                            result = result.element[0][0];
                        }
                        else
                        {
                            result = null;
                        }
                    }

                    Assert.AreEqual(depth + 4, detectedDepth);
                }

                // Make sure we deserialize without error
                {
                    var testbed = new RecursiveListKillerItem(depth, true);

                    var result = DoRecorderRoundTrip(testbed, mode, testSerializedResult: serialized => {
                        System.Console.WriteLine(serialized);
                    });

                    int detectedDepth = 0;
                    int foundFactory = 0;
                    while (result != null)
                    {
                        ++detectedDepth;

                        if (result.makeFactory)
                        {
                            Assert.IsTrue(result.element[0][0].usedFactory);
                            foundFactory++;
                        }
                        else
                        {
                            Assert.IsTrue(result.element == null || !result.element[0][0].usedFactory);
                        }

                        if (result.element != null)
                        {
                            result = result.element[0][0];
                        }
                        else
                        {
                            result = null;
                        }
                    }

                    Assert.AreEqual(depth + 4, detectedDepth);
                    Assert.AreEqual(1, foundFactory);
                }
            }
        }

        public class SidestepCore : Dec.IRecordable
        {
            public Factoried one;

            public void Record(Dec.Recorder recorder)
            {
                recorder
                    .WithFactory(new Dictionary<Type, Func<Type, object>>() { { typeof(FactoriedDerived), _ => new FactoriedDerived() { value = 1 } } })
                    .Record(ref one, "one");
            }
        }

        [Test]
        public void Sidestep([Values] RecorderMode mode)
        {
            var element = new SidestepCore();

            element.one = new FactoriedDerivedSibling();

            var deserialized = DoRecorderRoundTrip(element, mode);

            Assert.IsInstanceOf(typeof(FactoriedDerivedSibling), element.one);
            Assert.AreEqual(0, deserialized.one.value);
        }
    }
}
