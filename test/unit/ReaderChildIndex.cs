using System;
using System.Linq;
using System.Xml.Linq;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class ReaderChildIndex : Base
    {
        // Labels are built at runtime, so a lookup that compared references instead of contents would miss.
        private static string Label(int index)
        {
            return "f" + index;
        }

        public class Wide : Dec.IRecordable
        {
            public int count;
            public bool reverse;
            public bool repeatLast;
            public int[] values;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref count, nameof(count));
                recorder.Record(ref reverse, nameof(reverse));
                recorder.Record(ref repeatLast, nameof(repeatLast));

                // Resized rather than only created, since setting count by path replays this on an existing object.
                if (values == null || values.Length != count)
                {
                    Array.Resize(ref values, count);
                }

                for (int i = 0; i < count; ++i)
                {
                    int index = reverse ? count - 1 - i : i;
                    recorder.Record(ref values[index], Label(index));
                }

                if (repeatLast)
                {
                    recorder.Record(ref values[count - 1], Label(count - 1));
                }
            }

            public static Wide Create(int count)
            {
                return new Wide { count = count, values = Enumerable.Range(100, count).ToArray() };
            }
        }

        // Reads only the last field, ignoring every one before it.
        public class WideIgnoring : Dec.IRecordable
        {
            public int count;
            public int last;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref count, nameof(count));
                recorder.Ignore(nameof(Wide.reverse));
                recorder.Ignore(nameof(Wide.repeatLast));
                for (int i = 0; i < count - 1; ++i)
                {
                    recorder.Ignore(Label(i));
                }

                recorder.Record(ref last, Label(count - 1));
            }
        }

        // Wide writes three elements besides its values, so after a single field these put its element count just under, at, just over, and well over the threshold.
        private const int WideExtraElements = 3;
        private static readonly int[] Counts = { 1, Dec.ReaderNodeXml.ChildIndexThreshold - WideExtraElements - 1, Dec.ReaderNodeXml.ChildIndexThreshold - WideExtraElements, Dec.ReaderNodeXml.ChildIndexThreshold - WideExtraElements + 1, Dec.ReaderNodeXml.ChildIndexThreshold * 3 };

        private static XDocument Written(int count)
        {
            return XDocument.Parse(Dec.Recorder.Write(Wide.Create(count)));
        }

        private static XElement Field(XDocument doc, string name)
        {
            return doc.Root.Element("data").Element(name);
        }

        private static Wide Read(XDocument doc)
        {
            return Dec.Recorder.Read<Wide>(doc.ToString(SaveOptions.DisableFormatting));
        }

        [Test]
        public void InOrder([ValueSource(nameof(Counts))] int count)
        {
            var result = Read(Written(count));

            Assert.AreEqual(Enumerable.Range(100, count).ToArray(), result.values);
        }

        [Test]
        public void OutOfOrder([ValueSource(nameof(Counts))] int count)
        {
            var doc = Written(count);
            Field(doc, "reverse").Value = "true";

            var result = Read(doc);

            Assert.AreEqual(Enumerable.Range(100, count).ToArray(), result.values);
        }

        [Test]
        public void Missing([ValueSource(nameof(Counts))] int count, [Values] bool last)
        {
            int missing = last ? count - 1 : 0;
            var doc = Written(count);
            Field(doc, Label(missing)).Remove();

            var result = Read(doc);

            var expected = Enumerable.Range(100, count).ToArray();
            expected[missing] = 0;
            Assert.AreEqual(expected, result.values);
        }

        [Test]
        public void Duplicate([ValueSource(nameof(Counts))] int count, [Values] bool last)
        {
            int duplicated = last ? count - 1 : 0;
            var doc = Written(count);
            Field(doc, Label(duplicated)).AddAfterSelf(new XElement(Label(duplicated), "999"));

            int errors = 0;
            Wide result = null;
            ExpectErrors(() => result = Read(doc), err => { ++errors; return err.Contains($"`{Label(duplicated)}`"); });
            Assert.AreEqual(1, errors);

            Assert.AreEqual(Enumerable.Range(100, count).ToArray(), result.values);
        }

        [Test]
        public void ReadTwice([ValueSource(nameof(Counts))] int count)
        {
            var doc = Written(count);
            Field(doc, "repeatLast").Value = "true";

            int warnings = 0;
            Wide result = null;
            ExpectWarnings(() => result = Read(doc), warning => { ++warnings; return warning.Contains($"`{Label(count - 1)}`"); });
            Assert.AreEqual(1, warnings);

            Assert.AreEqual(Enumerable.Range(100, count).ToArray(), result.values);
        }

        [Test]
        public void Ignored([ValueSource(nameof(Counts))] int count)
        {
            var result = Dec.Recorder.Read<WideIgnoring>(Written(count).ToString(SaveOptions.DisableFormatting));

            Assert.AreEqual(count, result.count);
            Assert.AreEqual(100 + count - 1, result.last);
        }

        public class WideDec : Dec.Dec
        {
            public Wide wide;
        }

        // Not ReflectionSet: its sweep sets Wide's flags, which change what its Record() writes.
        [Test]
        public void Layers([ValueSource(nameof(Counts))] int count, [ValuesExcept(ParserMode.Validation, ParserMode.ReflectionSet)] ParserMode mode)
        {
            UpdateTestParameters(new Dec.Config.UnitTestParameters { explicitTypes = new Type[] { typeof(WideDec) } });

            // The parent sets every field; the child sets only the last, so every other field is missing from the child's layer.
            var parentFields = string.Concat(Enumerable.Range(0, count).Select(i => $"<{Label(i)}>{100 + i}</{Label(i)}>"));
            var parser = new Dec.Parser();
            parser.AddString(Dec.Parser.FileType.Xml, $@"
                <Decs>
                    <WideDec decName=""Parent"" abstract=""true""><wide><count>{count}</count>{parentFields}</wide></WideDec>
                    <WideDec decName=""Child"" parent=""Parent""><wide><{Label(count - 1)}>999</{Label(count - 1)}></wide></WideDec>
                </Decs>");
            parser.Finish();

            DoParserTests(mode);

            var expected = Enumerable.Range(100, count).ToArray();
            expected[count - 1] = 999;
            Assert.AreEqual(expected, Dec.Database<WideDec>.Get("Child").wide.values);
        }
    }
}
