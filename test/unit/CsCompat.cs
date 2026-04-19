using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class CsCompat : Base
    {
        public class DoubleRec : Dec.IRecordable
        {
            public double a;
            public double b;
            public float c;

            public void Record(Dec.Recorder record)
            {
                record.Record(ref a, "a");
                record.Record(ref b, "b");
                record.Record(ref c, "c");
            }
        }

        // Regression corpus for the .NET Core 2.1 float-roundtrip bug (dotnet/runtime#12035).
        // The bug is fixed in every runtime Dec still targets, but the specific values that broke it
        // remain useful as round-trip regression data.
        [Test]
        public void DotNet21FloatIssue([Values] RecorderMode mode)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

            var mr = new DoubleRec();
            mr.a = -8.22272715124268E-63;
            mr.b = -2.30119041724042E-247;
            mr.c = -30984198100f;

            var deserialized = DoRecorderRoundTrip(mr, mode);

            Assert.AreEqual(mr.a, deserialized.a);
            Assert.AreEqual(mr.b, deserialized.b);
            Assert.AreEqual(mr.c, deserialized.c);
        }
    }
}
