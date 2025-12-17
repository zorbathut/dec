using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class CompatDecLookup : Base
    {
        private class SimpleDec : Dec.Dec
        {
        }

        [Test]
        public void BasicRemappingGeneric()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });
            Dec.Config.CompatDecLookup = new Dictionary<Type, Dictionary<string, string>>()
            {
                { typeof(SimpleDec), new Dictionary<string, string>() { { "OldName", "NewName" } } }
            };

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""NewName"" />
                </Decs>");
            parser.Finish();

            // OldName should resolve to NewName
            Assert.IsNotNull(Dec.Database<SimpleDec>.Get("OldName"));
            Assert.AreEqual(Dec.Database<SimpleDec>.Get("NewName"), Dec.Database<SimpleDec>.Get("OldName"));
        }

        [Test]
        public void BasicRemappingNonGeneric()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });
            Dec.Config.CompatDecLookup = new Dictionary<Type, Dictionary<string, string>>()
            {
                { typeof(SimpleDec), new Dictionary<string, string>() { { "OldName", "NewName" } } }
            };

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""NewName"" />
                </Decs>");
            parser.Finish();

            // OldName should resolve to NewName
            Assert.IsNotNull(Dec.Database.Get(typeof(SimpleDec), "OldName"));
            Assert.AreEqual(Dec.Database.Get(typeof(SimpleDec), "NewName"), Dec.Database.Get(typeof(SimpleDec), "OldName"));
        }

        private class RootDec : Dec.Dec
        {
        }

        private class ChildDec : RootDec
        {
        }

        [Test]
        public void InheritanceWalksUp()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(RootDec), typeof(ChildDec) } });
            Dec.Config.CompatDecLookup = new Dictionary<Type, Dictionary<string, string>>()
            {
                { typeof(RootDec), new Dictionary<string, string>() { { "OldName", "TargetDec" } } }
            };

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ChildDec decName=""TargetDec"" />
                </Decs>");
            parser.Finish();

            // Child type lookup should walk up and find the parent's remapping
            Assert.AreEqual(Dec.Database<ChildDec>.Get("TargetDec"), Dec.Database<ChildDec>.Get("OldName"));
        }

        [Test]
        public void ExistingDecNotOverwritten()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });
            Dec.Config.CompatDecLookup = new Dictionary<Type, Dictionary<string, string>>()
            {
                { typeof(SimpleDec), new Dictionary<string, string>() { { "OldName", "NewName" } } }
            };

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""OldName"" />
                    <SimpleDec decName=""NewName"" />
                </Decs>");

            // Should warn because OldName already exists (shadowed mapping)
            ExpectWarnings(() => parser.Finish(), warningValidator: str => str.Contains("already exists"));

            var oldDec = Dec.Database<SimpleDec>.Get("OldName");
            var newDec = Dec.Database<SimpleDec>.Get("NewName");

            // OldName should return the actual OldName dec, not the remapped one
            Assert.AreNotEqual(oldDec, newDec);
        }

        [Test]
        public void NullLookupWorks()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });
            Dec.Config.CompatDecLookup = null;

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""TestDec"" />
                </Decs>");
            parser.Finish();

            // Should work normally
            Assert.IsNotNull(Dec.Database<SimpleDec>.Get("TestDec"));
            Assert.IsNull(Dec.Database<SimpleDec>.Get("MissingDec"));
        }

        [Test]
        public void EmptyLookupWorks()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });
            Dec.Config.CompatDecLookup = new Dictionary<Type, Dictionary<string, string>>();

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""TestDec"" />
                </Decs>");
            parser.Finish();

            // Should work normally
            Assert.IsNotNull(Dec.Database<SimpleDec>.Get("TestDec"));
            Assert.IsNull(Dec.Database<SimpleDec>.Get("MissingDec"));
        }

        private class DecRefHolder : Dec.IRecordable
        {
            public SimpleDec decRef;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref decRef, "decRef");
            }
        }

        [Test]
        public void SerializationRespectsMappingOnRead([Values] RecorderMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });
            Dec.Config.CompatDecLookup = new Dictionary<Type, Dictionary<string, string>>()
            {
                { typeof(SimpleDec), new Dictionary<string, string>() { { "OldName", "NewName" } } }
            };

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""NewName"" />
                </Decs>");
            parser.Finish();

            // Serialized data with old name
            string serialized = @"<Record>
  <recordFormatVersion>1</recordFormatVersion>
  <refs />
  <data>
    <decRef>OldName</decRef>
  </data>
</Record>";

            // Reading should succeed - OldName maps to NewName
            var result = Dec.Recorder.Read<DecRefHolder>(serialized);
            Assert.IsNotNull(result.decRef);
            Assert.AreEqual("NewName", result.decRef.DecName);
        }

        // Warning tests - these validate CompatDecLookup at Parser.Finish() time

        [Test]
        public void WarnOnShadowedMapping()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });
            Dec.Config.CompatDecLookup = new Dictionary<Type, Dictionary<string, string>>()
            {
                { typeof(SimpleDec), new Dictionary<string, string>() { { "OldName", "NewName" } } }
            };

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""OldName"" />
                    <SimpleDec decName=""NewName"" />
                </Decs>");

            // Should warn because OldName already exists
            ExpectWarnings(() => parser.Finish(), warningValidator: str => str.Contains("already exists"));

            // Lookup should still work - returns actual OldName dec
            var oldDec = Dec.Database<SimpleDec>.Get("OldName");
            var newDec = Dec.Database<SimpleDec>.Get("NewName");
            Assert.AreNotEqual(oldDec, newDec);
        }

        [Test]
        public void WarnOnMissingTarget()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });
            Dec.Config.CompatDecLookup = new Dictionary<Type, Dictionary<string, string>>()
            {
                { typeof(SimpleDec), new Dictionary<string, string>() { { "OldName", "NonExistent" } } }
            };

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""TestDec"" />
                </Decs>");

            // Should warn because NonExistent doesn't exist
            ExpectWarnings(() => parser.Finish(), warningValidator: str => str.Contains("does not exist"));

            // Lookup for OldName should return null (target doesn't exist)
            Assert.IsNull(Dec.Database<SimpleDec>.Get("OldName"));
        }

        private class NotADec
        {
        }

        [Test]
        public void WarnOnNonDecType()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(SimpleDec) } });
            Dec.Config.CompatDecLookup = new Dictionary<Type, Dictionary<string, string>>()
            {
                { typeof(NotADec), new Dictionary<string, string>() { { "OldName", "NewName" } } }
            };

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <SimpleDec decName=""TestDec"" />
                </Decs>");

            // Should warn because NotADec is not a Dec type
            ExpectWarnings(() => parser.Finish(), warningValidator: str => str.Contains("not a Dec type"));
        }

        [Test]
        public void WarnOnHierarchyConflict()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(RootDec), typeof(ChildDec) } });
            Dec.Config.CompatDecLookup = new Dictionary<Type, Dictionary<string, string>>()
            {
                { typeof(RootDec), new Dictionary<string, string>() { { "OldName", "TargetA" } } },
                { typeof(ChildDec), new Dictionary<string, string>() { { "OldName", "TargetB" } } }
            };

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <RootDec decName=""TargetA"" />
                    <ChildDec decName=""TargetB"" />
                </Decs>");

            // Should warn about conflicting mappings within the same hierarchy
            ExpectWarnings(() => parser.Finish(), warningValidator: str => str.Contains("conflicting"));
        }

        [Test]
        public void NoWarnOnSameMapping()
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(RootDec), typeof(ChildDec) } });
            Dec.Config.CompatDecLookup = new Dictionary<Type, Dictionary<string, string>>()
            {
                { typeof(RootDec), new Dictionary<string, string>() { { "OldName", "TargetDec" } } },
                { typeof(ChildDec), new Dictionary<string, string>() { { "OldName", "TargetDec" } } }
            };

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <ChildDec decName=""TargetDec"" />
                </Decs>");

            // Should NOT warn - both map to the same target
            parser.Finish();

            // Lookup should work
            Assert.IsNotNull(Dec.Database<RootDec>.Get("OldName"));
            Assert.IsNotNull(Dec.Database<ChildDec>.Get("OldName"));
        }
    }
}
