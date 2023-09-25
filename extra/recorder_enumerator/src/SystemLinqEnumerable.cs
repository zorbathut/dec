namespace Dec.RecorderCoroutine
{
    using System;
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
}
