namespace Dec.RecorderEnumerator
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    public class SystemLinqEnumerable_RangeIterator_Converter : ConverterFactoryDynamic
    {
        internal static Type RelevantType = typeof(System.Linq.Enumerable).GetNestedType("RangeIterator", System.Reflection.BindingFlags.NonPublic);

        internal static FieldInfo Field_Current = RelevantType.GetField("_current", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static FieldInfo Field_State = RelevantType.GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static FieldInfo Field_Start = RelevantType.GetField("_start", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static FieldInfo Field_End = RelevantType.GetField("_end", BindingFlags.NonPublic | BindingFlags.Instance);

        public override void Write(object input, Recorder recorder)
        {
            recorder.RecordPrivate(input, Field_Current, "current");
            recorder.RecordPrivate(input, Field_State, "state");
            recorder.RecordPrivate(input, Field_Start, "start");
            recorder.RecordPrivate(input, Field_End, "end");
        }

        public override object Create(Recorder recorder)
        {
            int start = 0;
            int end = 0;
            recorder.Record(ref start, "start");
            recorder.Record(ref end, "end");

            // it stores "start" and "end", but takes "start" and "range" as parameters, so, okay, fine, sure
            return Activator.CreateInstance(RelevantType, start, end - start);
        }

        public override void Read(ref object input, Recorder recorder)
        {
            recorder.RecordPrivate(input, Field_Current, "current");
            recorder.RecordPrivate(input, Field_State, "state");
        }
    }

    public static class SystemLinqEnumerable_WhereIterator_Converter
    {
        internal static Type RelevantType = typeof(System.Linq.Enumerable).GetNestedType("WhereEnumerableIterator`1", System.Reflection.BindingFlags.NonPublic);
    }

    public class SystemLinqEnumerable_WhereIterator_Converter<Iterator, T> : ConverterFactoryDynamic
    {
        internal FieldInfo field_Source = typeof(Iterator).GetField("_source", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Predicate = typeof(Iterator).GetField("_predicate", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Enumerator = typeof(Iterator).GetField("_enumerator", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_State = typeof(Iterator).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Current = typeof(Iterator).GetField("_current", BindingFlags.NonPublic | BindingFlags.Instance);

        public override void Write(object input, Recorder recorder)
        {
            var fs = typeof(Iterator).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

            recorder.Shared().RecordPrivate(input, field_Source, "source");
            recorder.Shared().RecordPrivate(input, field_Predicate, "predicate");
            recorder.Shared().RecordPrivate(input, field_Enumerator, "enumerator");
            recorder.RecordPrivate(input, field_State, "state");
            recorder.RecordPrivate(input, field_Current, "current");
        }

        private static bool False(T input)
        {
            return false;
        }

        public override object Create(Recorder recorder)
        {
            return Activator.CreateInstance(typeof(Iterator), Enumerable.Empty<T>(), new Func<T, bool>(False));
        }

        public override void Read(ref object input, Recorder recorder)
        {
            // it's the same code, we only need this for the funky Create
            Write(input, recorder);
        }
    }
}
