namespace DefTest
{
    using NUnit.Framework;
    using NUnit.Framework.Internal;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [TestFixture]
    public class Compat : Base
    {
        public class DoubleRec : Def.IRecordable
        {
            public double a;
            public double b;

            public void Record(Def.Recorder record)
            {
                record.Record(ref a, "a");
                record.Record(ref b, "b");
            }
        }

        [Test]
        public void DotNet21FloatIssue([Values] bool pretty)
        {
            // Intended to verify that this test doesn't stop working somehow, so we actually test both codepaths . . .
            bool floatSuccess = -8.22272715124268E-63 == double.Parse("-8.22272715124268E-63");
            int[] assemblyVersion = typeof(float)
                .Assembly
                .CustomAttributes
                .Where(ca => ca.AttributeType == typeof(System.Reflection.AssemblyFileVersionAttribute))
                .Single()
                .ConstructorArguments[0]
                .ToString()
                .Trim('"')
                .Split('.')
                .Select(n => int.Parse(n))
                .ToArray();
            bool bugShouldBeFixed =
                assemblyVersion[0] > 4 ||
                (assemblyVersion[0] == 4 && assemblyVersion[1] > 7);

            Assert.IsTrue(floatSuccess == bugShouldBeFixed);

            var mr = new DoubleRec();
            mr.a = -8.22272715124268E-63;
            mr.b = -2.30119041724042E-247;

            string serialized = Def.Recorder.Write(mr, pretty: pretty);
            var deserialized = Def.Recorder.Read<DoubleRec>(serialized);

            Assert.AreEqual(mr.a, deserialized.a);
            Assert.AreEqual(mr.b, deserialized.b);
        }
    }
}
