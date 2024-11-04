using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace DecTest
{
    [TestFixture]
    public class Culture : Base
    {

        [Dec.StaticReferences]
        public static class StaticRefsDecs
        {
            static StaticRefsDecs() { Dec.StaticReferencesAttribute.Initialized(); }

            public static StubDec Brød;
            public static StubDec ขนมปัง;
            public static StubDec パン;
            public static StubDec 餧;
            public static StubDec 麵包;
            public static StubDec خبز;
            public static StubDec 빵;
            public static StubDec לחם;
            public static StubDec ပေါင်မုန့်;
            public static StubDec ബ്രെഡ്;
        }

        [Test]
        public void StaticRefs([Values] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StubDec) }, explicitStaticRefs = new Type[] { typeof(StaticRefsDecs) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, @"
                <Decs>
                    <StubDec decName=""Brød"" />
                    <StubDec decName=""ขนมปัง"" />
                    <StubDec decName=""パン"" />
                    <StubDec decName=""餧"" />
                    <StubDec decName=""麵包"" />
                    <StubDec decName=""خبز"" />
                    <StubDec decName=""빵"" />
                    <StubDec decName=""לחם"" />
                    <StubDec decName=""ပေါင်မုန့်"" />
                    <StubDec decName=""ബ്രെഡ്"" />
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            Assert.IsNotNull(Dec.Database<StubDec>.Get("Brød"));
            Assert.IsNotNull(Dec.Database<StubDec>.Get("ขนมปัง"));
            Assert.IsNotNull(Dec.Database<StubDec>.Get("パン"));
            Assert.IsNotNull(Dec.Database<StubDec>.Get("餧"));
            Assert.IsNotNull(Dec.Database<StubDec>.Get("麵包"));
            Assert.IsNotNull(Dec.Database<StubDec>.Get("خبز"));
            Assert.IsNotNull(Dec.Database<StubDec>.Get("빵"));
            Assert.IsNotNull(Dec.Database<StubDec>.Get("לחם"));
            Assert.IsNotNull(Dec.Database<StubDec>.Get("ပေါင်မုန့်"));
            Assert.IsNotNull(Dec.Database<StubDec>.Get("ബ്രെഡ്"));

            Assert.AreSame(Dec.Database<StubDec>.Get("Brød"), StaticRefsDecs.Brød);
            Assert.AreSame(Dec.Database<StubDec>.Get("ขนมปัง"), StaticRefsDecs.ขนมปัง);
            Assert.AreSame(Dec.Database<StubDec>.Get("パン"), StaticRefsDecs.パン);
            Assert.AreSame(Dec.Database<StubDec>.Get("餧"), StaticRefsDecs.餧);
            Assert.AreSame(Dec.Database<StubDec>.Get("麵包"), StaticRefsDecs.麵包);
            Assert.AreSame(Dec.Database<StubDec>.Get("خبز"), StaticRefsDecs.خبز);
            Assert.AreSame(Dec.Database<StubDec>.Get("빵"), StaticRefsDecs.빵);
            Assert.AreSame(Dec.Database<StubDec>.Get("לחם"), StaticRefsDecs.לחם);
            Assert.AreSame(Dec.Database<StubDec>.Get("ပေါင်မုန့်"), StaticRefsDecs.ပေါင်မုန့်);
            Assert.AreSame(Dec.Database<StubDec>.Get("ബ്രെഡ്"), StaticRefsDecs.ബ്രെഡ്);
        }

        [Test]
        public void Custom()
        {
            Dec.Config.CultureInfo = new System.Globalization.CultureInfo("hu-HU");

            var z = @"
                    <Record>
                        <recordFormatVersion>1</recordFormatVersion>
                        <data>1,5</data>
                    </Record>
            ";
            var result = Dec.Recorder.Read<float>(z, "data");
            Assert.AreEqual(1.5, result);
        }
    }
}
