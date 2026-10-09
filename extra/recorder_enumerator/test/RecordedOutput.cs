using System;
using System.Collections.Generic;
using DecTest;
using NUnit.Framework;

namespace RecorderEnumeratorTest
{
    // Pins the exact XML recorded for compiler-generated iterators and closures. Saved files depend on it, so a change here is a format change.
    //
    // The compiler numbers its generated types by their method's position in this class, and those names are part of the pinned output; add new members at the end.
    [TestFixture]
    [Dec.RecorderEnumerator.RecordableClosures]
    public class RecordedOutput : Base
    {
        public class Payload : Dec.IRecordable
        {
            public int value;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref value, nameof(value));
            }
        }

        [Dec.RecorderEnumerator.RecordableEnumerable]
        private static IEnumerable<int> MixedLocals(int start)
        {
            float scale = 1.5f;
            string tag = "tag";
            var seen = new List<int>();
            for (int i = start; ; ++i)
            {
                seen.Add(i);
                yield return (int)(i * scale) + tag.Length + seen.Count;
            }
        }

        private static IEnumerator<int> BuildMixedLocals()
        {
            var iterator = MixedLocals(3).GetEnumerator();
            iterator.MoveNext();
            iterator.MoveNext();
            return iterator;
        }

        public class Owner : Dec.IRecordable
        {
            public int data;
            public IEnumerator<int> counter;

            [Dec.RecorderEnumerator.RecordableEnumerable]
            public IEnumerable<int> Count()
            {
                while (true)
                {
                    data += 2;
                    yield return data;
                }
            }

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref data, nameof(data));
                recorder.Record(ref counter, nameof(counter));
            }
        }

        private static Owner BuildOwner()
        {
            var owner = new Owner { data = 5 };
            owner.counter = owner.Count().GetEnumerator();
            owner.counter.MoveNext();
            return owner;
        }

        [Dec.RecorderEnumerator.RecordableEnumerable]
        private static IEnumerable<T> Alternate<T>(T first, T second)
        {
            while (true)
            {
                yield return first;
                yield return second;
            }
        }

        private static IEnumerator<string> BuildGeneric()
        {
            var iterator = Alternate("left", "right").GetEnumerator();
            iterator.MoveNext();
            return iterator;
        }

        [Dec.RecorderEnumerator.RecordableEnumerable]
        private static IEnumerable<int> WatchPayload(Payload payload)
        {
            var history = new List<int>();
            while (true)
            {
                history.Add(payload.value);
                yield return history.Count * 1000 + payload.value;
            }
        }

        public class PayloadWatchers : Dec.IRecordable
        {
            public Payload payload;
            public IEnumerator<int> first;
            public IEnumerator<int> second;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Shared().Record(ref payload, nameof(payload));
                recorder.Record(ref first, nameof(first));
                recorder.Record(ref second, nameof(second));
            }
        }

        private static PayloadWatchers BuildPayloadWatchers()
        {
            var watchers = new PayloadWatchers { payload = new Payload { value = 7 } };
            watchers.first = WatchPayload(watchers.payload).GetEnumerator();
            watchers.second = WatchPayload(watchers.payload).GetEnumerator();
            watchers.first.MoveNext();
            watchers.first.MoveNext();
            watchers.second.MoveNext();
            return watchers;
        }

        [Dec.RecorderEnumerator.RecordableEnumerable]
        private static IEnumerable<int> LocalFunctionCounter(int step)
        {
            int total = 0;
            void Bump()
            {
                total += step;
            }

            while (true)
            {
                Bump();
                yield return total;
            }
        }

        private static IEnumerator<int> BuildLocalFunctionCounter()
        {
            var iterator = LocalFunctionCounter(4).GetEnumerator();
            iterator.MoveNext();
            iterator.MoveNext();
            return iterator;
        }

        private static Func<int> MakeClosure(int seed)
        {
            string name = "closure";
            return () => seed + name.Length;
        }

        private static Func<int> MakeNestedClosure(int outer)
        {
            int doubled = outer * 2;
            Func<int> result = null;
            for (int i = 0; i < 1; ++i)
            {
                int inner = i + 7;
                result = () => outer + doubled + inner;
            }
            return result;
        }

        private static Func<int> MakeGenericClosure<T>(T value)
        {
            return () => value.GetHashCode();
        }

        private static void AssertResumesAlike<T>(IEnumerator<T> expected, IEnumerator<T> actual)
        {
            Assert.AreNotSame(expected, actual);
            Assert.AreEqual(expected.Current, actual.Current);
            for (int i = 0; i < 5; ++i)
            {
                Assert.AreEqual(expected.MoveNext(), actual.MoveNext());
                Assert.AreEqual(expected.Current, actual.Current);
            }
        }

        private static void AssertPinned(object input, string expectedXml)
        {
            Assert.AreEqual(expectedXml, Dec.Recorder.Write(input));
        }

        [Test]
        public void MixedLocalsPinned()
        {
            AssertPinned(BuildMixedLocals(), MixedLocalsXml);
        }

        [Test]
        public void MixedLocalsRoundTrip([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            var original = BuildMixedLocals();
            AssertResumesAlike(original, DoRecorderRoundTrip(original, mode));
        }

        [Test]
        public void OwnerPinned()
        {
            AssertPinned(BuildOwner(), OwnerXml);
        }

        [Test]
        public void OwnerRoundTrip([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode mode)
        {
            var original = BuildOwner();
            var result = DoRecorderRoundTrip(original, mode);
            Assert.AreEqual(original.data, result.data);
            AssertResumesAlike(original.counter, result.counter);
            Assert.AreEqual(original.data, result.data);
        }

        [Test]
        public void GenericPinned()
        {
            AssertPinned(BuildGeneric(), GenericXml);
        }

        [Test]
        public void GenericRoundTrip([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            var original = BuildGeneric();
            AssertResumesAlike(original, DoRecorderRoundTrip(original, mode));
        }

        [Test]
        public void PayloadWatchersPinned()
        {
            AssertPinned(BuildPayloadWatchers(), PayloadWatchersXml);
        }

        [Test]
        public void PayloadWatchersRoundTrip([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode mode)
        {
            var original = BuildPayloadWatchers();
            var result = DoRecorderRoundTrip(original, mode);

            // Both iterators must still watch the one payload their holder has.
            result.payload.value = 11;
            original.payload.value = 11;
            AssertResumesAlike(original.first, result.first);
            AssertResumesAlike(original.second, result.second);
        }

        [Test]
        public void LocalFunctionCounterPinned()
        {
            AssertPinned(BuildLocalFunctionCounter(), LocalFunctionCounterXml);
        }

        [Test]
        public void LocalFunctionCounterRoundTrip([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            var original = BuildLocalFunctionCounter();
            AssertResumesAlike(original, DoRecorderRoundTrip(original, mode));
        }

        [Test]
        public void ClosurePinned()
        {
            AssertPinned(MakeClosure(5), ClosureXml);
        }

        [Test]
        public void NestedClosurePinned()
        {
            AssertPinned(MakeNestedClosure(3), NestedClosureXml);
        }

        [Test]
        public void GenericClosurePinned()
        {
            AssertPinned(MakeGenericClosure(42), GenericClosureXml);
        }

        [Test]
        public void ClosuresRoundTrip([ValuesExcept(RecorderMode.Validation)] RecorderMode mode)
        {
            foreach (var original in new[] { MakeClosure(5), MakeNestedClosure(3), MakeGenericClosure(42) })
            {
                var result = DoRecorderRoundTrip(original, mode);
                Assert.AreNotSame(original, result);
                Assert.AreEqual(original(), result());
            }
        }

        [Test]
        public void LegacyThreadIdRead()
        {
            // Files written before the thread id was skipped still carry it; reading one must neither warn nor restore it.
            string legacy = OwnerXml.Replace("<LABRAB2__current>7</LABRAB2__current>", "<LABRAB2__current>7</LABRAB2__current><LABRABl__initialThreadId>5</LABRABl__initialThreadId>");
            Assert.AreNotEqual(OwnerXml, legacy);

            var result = Dec.Recorder.Read<Owner>(legacy);

            var field = result.counter.GetType().GetField("<>l__initialThreadId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.AreEqual(-1, field.GetValue(result.counter));
            AssertResumesAlike(BuildOwner().counter, result.counter);
        }

        public class OwnerCreatesIterator : Dec.IRecordable
        {
            public int data;
            public IEnumerator<int> counter;

            public OwnerCreatesIterator()
            {
                counter = Count().GetEnumerator();
            }

            [Dec.RecorderEnumerator.RecordableEnumerable]
            public IEnumerable<int> Count()
            {
                while (true)
                {
                    data += 3;
                    yield return data;
                }
            }

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref data, nameof(data));
                recorder.Record(ref counter, nameof(counter));
            }
        }

        [Test]
        public void OwnerCreatesIteratorRoundTrip([ValuesExcept(RecorderMode.Validation, RecorderMode.Simple)] RecorderMode mode)
        {
            // Reading builds the owner first, so its constructor's iterator already exists when the recorded one is read into it.
            var original = new OwnerCreatesIterator { data = 4 };
            original.counter.MoveNext();

            var result = DoRecorderRoundTrip(original, mode);
            Assert.AreEqual(original.data, result.data);
            AssertResumesAlike(original.counter, result.counter);
            Assert.AreEqual(original.data, result.data);
        }

        private const string MixedLocalsXml = @"<!--This file was written by Dec, a serialization library designed for game development. (https://github.com/zorbathut/dec)--><!--Pretty-print can be enabled as a parameter of the Recorder.Write() call.--><Record><recordFormatVersion>1</recordFormatVersion><data class=""RecorderEnumeratorTest.RecordedOutput.&lt;MixedLocals&gt;d__1""><LABRAB1__state>1</LABRAB1__state><LABRAB2__current>11</LABRAB2__current><start>3</start><LABRAB3__start>3</LABRAB3__start><LABscaleRAB5__1>1.5</LABscaleRAB5__1><LABtagRAB5__2>tag</LABtagRAB5__2><LABseenRAB5__3><li>3</li><li>4</li></LABseenRAB5__3><LABiRAB5__4>4</LABiRAB5__4></data></Record>";
        private const string OwnerXml = @"<!--This file was written by Dec, a serialization library designed for game development. (https://github.com/zorbathut/dec)--><!--Pretty-print can be enabled as a parameter of the Recorder.Write() call.--><Record><recordFormatVersion>1</recordFormatVersion><refs><Ref id=""ref00000"" class=""RecorderEnumeratorTest.RecordedOutput.Owner""><data>7</data><counter class=""RecorderEnumeratorTest.RecordedOutput.Owner.&lt;Count&gt;d__2""><LABRAB1__state>1</LABRAB1__state><LABRAB2__current>7</LABRAB2__current><LABRAB4__this ref=""ref00000"" /></counter></Ref></refs><data ref=""ref00000"" /></Record>";
        private const string GenericXml = @"<!--This file was written by Dec, a serialization library designed for game development. (https://github.com/zorbathut/dec)--><!--Pretty-print can be enabled as a parameter of the Recorder.Write() call.--><Record><recordFormatVersion>1</recordFormatVersion><data class=""RecorderEnumeratorTest.RecordedOutput.&lt;Alternate&gt;d__5&lt;string&gt;""><LABRAB1__state>1</LABRAB1__state><LABRAB2__current>left</LABRAB2__current><first>left</first><LABRAB3__first>left</LABRAB3__first><second>right</second><LABRAB3__second>right</LABRAB3__second></data></Record>";
        private const string PayloadWatchersXml = @"<!--This file was written by Dec, a serialization library designed for game development. (https://github.com/zorbathut/dec)--><!--Pretty-print can be enabled as a parameter of the Recorder.Write() call.--><Record><recordFormatVersion>1</recordFormatVersion><refs><Ref id=""ref00000"" class=""RecorderEnumeratorTest.RecordedOutput.Payload""><value>7</value></Ref></refs><data class=""RecorderEnumeratorTest.RecordedOutput.PayloadWatchers""><payload ref=""ref00000"" /><first class=""RecorderEnumeratorTest.RecordedOutput.&lt;WatchPayload&gt;d__7""><LABRAB1__state>1</LABRAB1__state><LABRAB2__current>2007</LABRAB2__current><payload ref=""ref00000"" /><LABRAB3__payload ref=""ref00000"" /><LABhistoryRAB5__1><li>7</li><li>7</li></LABhistoryRAB5__1></first><second class=""RecorderEnumeratorTest.RecordedOutput.&lt;WatchPayload&gt;d__7""><LABRAB1__state>1</LABRAB1__state><LABRAB2__current>1007</LABRAB2__current><payload ref=""ref00000"" /><LABRAB3__payload ref=""ref00000"" /><LABhistoryRAB5__1><li>7</li></LABhistoryRAB5__1></second></data></Record>";
        private const string LocalFunctionCounterXml = @"<!--This file was written by Dec, a serialization library designed for game development. (https://github.com/zorbathut/dec)--><!--Pretty-print can be enabled as a parameter of the Recorder.Write() call.--><Record><recordFormatVersion>1</recordFormatVersion><data class=""RecorderEnumeratorTest.RecordedOutput.&lt;LocalFunctionCounter&gt;d__10""><LABRAB1__state>1</LABRAB1__state><LABRAB2__current>8</LABRAB2__current><step>4</step><LABRAB3__step>4</LABRAB3__step><LABRAB8__1><total>8</total><step>4</step></LABRAB8__1></data></Record>";
        private const string ClosureXml = @"<!--This file was written by Dec, a serialization library designed for game development. (https://github.com/zorbathut/dec)--><!--Pretty-print can be enabled as a parameter of the Recorder.Write() call.--><Record><recordFormatVersion>1</recordFormatVersion><data class=""System.Func&lt;int&gt;""><method class=""System.Reflection.RuntimeMethodInfo""><type>RecorderEnumeratorTest.RecordedOutput.&lt;&gt;c__DisplayClass12_0</type><name>&lt;MakeClosure&gt;b__0</name><params /></method><target class=""RecorderEnumeratorTest.RecordedOutput.&lt;&gt;c__DisplayClass12_0""><seed>5</seed><name>closure</name></target></data></Record>";
        private const string NestedClosureXml = @"<!--This file was written by Dec, a serialization library designed for game development. (https://github.com/zorbathut/dec)--><!--Pretty-print can be enabled as a parameter of the Recorder.Write() call.--><Record><recordFormatVersion>1</recordFormatVersion><data class=""System.Func&lt;int&gt;""><method class=""System.Reflection.RuntimeMethodInfo""><type>RecorderEnumeratorTest.RecordedOutput.&lt;&gt;c__DisplayClass13_1</type><name>&lt;MakeNestedClosure&gt;b__0</name><params /></method><target class=""RecorderEnumeratorTest.RecordedOutput.&lt;&gt;c__DisplayClass13_1""><inner>7</inner><CSSTRLABRAB8__locals1><outer>3</outer><doubled>6</doubled></CSSTRLABRAB8__locals1></target></data></Record>";
        private const string GenericClosureXml = @"<!--This file was written by Dec, a serialization library designed for game development. (https://github.com/zorbathut/dec)--><!--Pretty-print can be enabled as a parameter of the Recorder.Write() call.--><Record><recordFormatVersion>1</recordFormatVersion><data class=""System.Func&lt;int&gt;""><method class=""System.Reflection.RuntimeMethodInfo""><type>RecorderEnumeratorTest.RecordedOutput.&lt;&gt;c__DisplayClass14_0&lt;int&gt;</type><name>&lt;MakeGenericClosure&gt;b__0</name><params /></method><target class=""RecorderEnumeratorTest.RecordedOutput.&lt;&gt;c__DisplayClass14_0&lt;int&gt;""><value>42</value></target></data></Record>";
    }
}
