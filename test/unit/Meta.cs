using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DecTest
{
    [TestFixture]
    public class Meta : Base
    {
        [Dec.StaticReferences]
        public static class StubDecs
        {
            static StubDecs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static StubDec TestDec;
        }

        [Test]
        public void Clear()
        {
            ExpectWarnings(() => Assert.IsNull(Dec.Database<StubDec>.Get("TestDec")));
            // we don't test StubDecs.TestDec here because if we do, we'll kick off the detection

            {
                UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDec) }, explicitStaticRefs = new Type[]{ typeof(StubDecs) } });

                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <StubDec decName=""TestDec"" />
                    </Decs>");
                parser.Finish();
            }

            Assert.IsNotNull(Dec.Database<StubDec>.Get("TestDec"));
            Assert.AreEqual(StubDecs.TestDec, Dec.Database<StubDec>.Get("TestDec"));

            Dec.Database.Clear();

            ExpectWarnings(() => Assert.IsNull(Dec.Database<StubDec>.Get("TestDec")));
            Assert.IsNull(StubDecs.TestDec);

            {
                UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(StubDec) }, explicitStaticRefs = new Type[]{ typeof(StubDecs) } });

                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <StubDec decName=""TestDec"" />
                    </Decs>");
                parser.Finish();
            }

            Assert.IsNotNull(Dec.Database<StubDec>.Get("TestDec"));
            Assert.AreEqual(StubDecs.TestDec, Dec.Database<StubDec>.Get("TestDec"));

            Dec.Database.Clear();

            ExpectWarnings(() => Assert.IsNull(Dec.Database<StubDec>.Get("TestDec")));
            Assert.IsNull(StubDecs.TestDec);
        }

        public class RefTargetDec : Dec.Dec
        {

        }

        public class RefSourceDec : Dec.Dec
        {
            public RefTargetDec target;
        }

        [Test]
        public void ClearRef()
        {
            // Had a bug where Dec.Database.Clear() wasn't properly clearing the lookup table used for references
            // This double-checks it

            {
                UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(RefTargetDec), typeof(RefSourceDec) } });

                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <RefTargetDec decName=""Target"" />
                        <RefSourceDec decName=""Source"">
                            <target>Target</target>
                        </RefSourceDec>
                    </Decs>");
                parser.Finish();
            }

            {
                var target = Dec.Database<RefTargetDec>.Get("Target");
                var source = Dec.Database<RefSourceDec>.Get("Source");
                Assert.IsNotNull(target);
                Assert.IsNotNull(source);

                Assert.AreEqual(source.target, target);
            }

            Dec.Database.Clear();

            {
                UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[]{ typeof(RefTargetDec), typeof(RefSourceDec) } });

                var parser = new Dec.Parser();
                parser.AddString(Dec.Parser.FileType.Xml, @"
                    <Decs>
                        <RefSourceDec decName=""Source"">
                            <target>Target</target>
                        </RefSourceDec>
                    </Decs>");
                ExpectErrors(() => parser.Finish());
            }

            {
                var target = Dec.Database<RefTargetDec>.Get("Target");
                var source = Dec.Database<RefSourceDec>.Get("Source");
                Assert.IsNull(target);
                Assert.IsNotNull(source);

                Assert.IsNull(source.target);
            }
        }

        [Test]
        public void ClassCacheReset()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { });

            var parseCache = (Dictionary<string, Type>)Assembly.GetAssembly(typeof(Dec.Dec)).GetType("Dec.UtilType").GetField("ParseCache", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);

            // This will be equal to our seeded intro values.
            int baseSize = parseCache.Count;
            Assert.AreEqual(baseSize, parseCache.Count);

            parseCache.Add("Meta", typeof(Meta));
            Assert.AreEqual(baseSize + 1, parseCache.Count);

            ExpectWarnings(() => DoParserTests(ParserMode.RewrittenBare));

            // Hopefully we reset after doing the DoParserTests()!
            Assert.AreEqual(baseSize, parseCache.Count);
        }
    }
}
