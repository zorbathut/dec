using System;
using NUnit.Framework;

namespace DecTest
{
    // How an element's text is read when it's split across several text nodes, by comments, CDATA, or child elements.
    [TestFixture]
    public class XmlText : Base
    {
        public class TextObj
        {
            public int x;
        }

        public class TextRec : Dec.IRecordable
        {
            public int value;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref value, nameof(value));
            }
        }

        public class TextDec : Dec.Dec
        {
            public int hp;
            public string name;
            public byte[] bytes;
            public TextObj obj;
            public TextRec rec;
        }

        private Dec.Parser TextParser(string body)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(TextDec) } });

            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <TextDec decName=""TestDec"">
                        {body}
                    </TextDec>
                </Decs>");
            return parser;
        }

        private static TextDec Result()
        {
            return Dec.Database<TextDec>.Get("TestDec");
        }

        [Test]
        public void StrayTextInRecord([ValuesExcept(ParserMode.Validation)] ParserMode mode)
        {
            var parser = TextParser(@"
                <hp>3</hp>
                <rec>
                    <value>5</value>
                    stray
                </rec>");
            int errors = 0;
            ExpectErrors(() => parser.Finish(), err => { ++errors; return true; });
            Assert.AreEqual(1, errors);

            DoParserTests(mode);

            Assert.AreEqual(3, Result().hp);
            Assert.AreEqual(5, Result().rec.value);
        }

        [Test]
        public void StrayTextInReflected([Values] ParserMode mode)
        {
            // The object both gets the text-and-children error and, like any object with text, has its layer discarded.
            var parser = TextParser(@"
                <hp>3</hp>
                <obj>
                    <x>5</x>
                    stray
                </obj>");
            int errors = 0;
            ExpectErrors(() => parser.Finish(), err => { ++errors; return true; });
            Assert.AreEqual(2, errors);

            DoParserTests(mode);

            Assert.AreEqual(3, Result().hp);
            Assert.IsNull(Result().obj);
        }

        [Test]
        public void StrayTextAfterComment([Values] ParserMode mode)
        {
            // No child elements at all; the text only hides behind the comment's indentation.
            var parser = TextParser(@"
                <hp>3</hp>
                <obj>
                    <!-- a note -->
                    stray
                </obj>");
            int errors = 0;
            ExpectErrors(() => parser.Finish(), err => { ++errors; return true; });
            Assert.AreEqual(1, errors);

            DoParserTests(mode);

            Assert.AreEqual(3, Result().hp);
            Assert.IsNull(Result().obj);
        }
    }
}
