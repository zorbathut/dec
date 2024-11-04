using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DecTest
{
    [TestFixture]
    public class Attribute : Base
    {
        public class BaseString : Dec.IRecordable
        {
            public string value = "default";

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref value, "value");
            }
        }

        public class DerivedString : BaseString
        {

        }

        public class StringMemberRecordable : Dec.IRecordable
        {
            public BaseString member;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref member, "member");
            }
        }

        public class StringMemberDec : Dec.Dec
        {
            public BaseString member;
        }

        [Test]
        public void Record([Values] bool classTag, [Values] bool nullTag, [Values] bool refTag, [Values] bool modeTag)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(BaseString), typeof(DerivedString) } });

            string serialized = $@"
                <Record>
                  <recordFormatVersion>1</recordFormatVersion>
                  <refs>
                    <Ref id=""ref00000"" class=""BaseString""><value>reffed</value></Ref>
                  </refs>
                  <data>
                    <member {(classTag ? "class='DerivedString'" : "")} {(nullTag ? "null='true'" : "")} {(refTag ? "ref='ref00000'" : "")} {(modeTag ? "mode='patch'" : "")}><value>data</value></member>
                  </data>
                </Record>";

            StringMemberRecordable deserialized = null;

            int tags = ((classTag || modeTag) ? 1 : 0) + (nullTag ? 1 : 0) + (refTag ? 1 : 0);
            if (tags <= 1)
            {
                deserialized = Dec.Recorder.Read<StringMemberRecordable>(serialized);
            }
            else
            {
                ExpectErrors(() => deserialized = Dec.Recorder.Read<StringMemberRecordable>(serialized));
            }

            if (refTag && classTag)
            {
                // these conflict and end up returning null
                Assert.IsNull(deserialized.member);
            }
            else if (refTag)
            {
                Assert.IsInstanceOf<BaseString>(deserialized.member);
                Assert.AreEqual("reffed", deserialized.member.value);
            }
            else if (nullTag)
            {
                Assert.IsNull(deserialized.member);
            }
            else if (classTag)
            {
                Assert.IsInstanceOf<DerivedString>(deserialized.member);
                Assert.AreEqual("data", deserialized.member.value);
            }
            else
            {
                Assert.IsInstanceOf<BaseString>(deserialized.member);
                Assert.AreEqual("data", deserialized.member.value);
            }
        }

        [Test]
        public void Parser([Values] ParserMode mode, [Values] bool classTag, [Values] bool nullTag, [Values] bool refTag, [Values] bool modeTag)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(StringMemberDec), typeof(DerivedString) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <StringMemberDec decName=""TestDec"">
                        <member {(classTag ? "class='DerivedString'" : "")} {(nullTag ? "null='true'" : "")} {(refTag ? "ref='ref00000'" : "")} {(modeTag ? "mode='patch'" : "")}><value>data</value></member>
                    </StringMemberDec>
                </Decs>");

            int tags = ((classTag || modeTag) ? 1 : 0) + (nullTag ? 1 : 0) + (refTag ? 1 : 0);
            if (tags <= 1 && !refTag)
            {
                parser.Finish();
            }
            else
            {
                ExpectErrors(() => parser.Finish());
            }

            DoParserTests(mode);

            if (nullTag)
            {
                Assert.IsNull(Dec.Database<StringMemberDec>.Get("TestDec").member);
            }
            else if (classTag)
            {
                Assert.IsInstanceOf<DerivedString>(Dec.Database<StringMemberDec>.Get("TestDec").member);
                Assert.AreEqual("data", Dec.Database<StringMemberDec>.Get("TestDec").member.value);
            }
            else
            {
                Assert.IsInstanceOf<BaseString>(Dec.Database<StringMemberDec>.Get("TestDec").member);
                Assert.AreEqual("data", Dec.Database<StringMemberDec>.Get("TestDec").member.value);
            }
        }
    }
}
