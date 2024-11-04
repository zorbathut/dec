using System;
using System.Reflection;

namespace Dec.RecorderEnumerator
{
    public static class SystemLinqEnumerable_SelectEnumerable_Converter
    {
        internal static Type RelevantType = typeof(System.Linq.Enumerable).GetNestedType("SelectEnumerableIterator`2", System.Reflection.BindingFlags.NonPublic);
    }

    public class SystemLinqEnumerable_SelectEnumerable_Converter<Iterator, T> : ConverterFactoryDynamic
    {
        internal FieldInfo field_Source = typeof(Iterator).GetField("_source", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Selector = typeof(Iterator).GetField("_selector", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Enumerator = typeof(Iterator).GetField("_enumerator", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_State = typeof(Iterator).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Current = typeof(Iterator).GetField("_current", BindingFlags.NonPublic | BindingFlags.Instance);

        public override void Write(object input, Recorder recorder)
        {
            recorder.Shared().RecordPrivate(input, field_Source, "source");
            recorder.RecordPrivate(input, field_Selector, "selector");
            recorder.Shared().RecordPrivate(input, field_Enumerator, "enumerator");
            recorder.RecordPrivate(input, field_State, "state");
            recorder.SharedIfPossible<T>().RecordPrivate(input, field_Current, "current");
        }

        public override object Create(Recorder recorder)
        {
            return Activator.CreateInstance(typeof(Iterator), new object[] { null, null });
        }

        public override void Read(ref object input, Recorder recorder)
        {
            // it's the same code, we only need this for the funky Create
            Write(input, recorder);
        }
    }

    public static class SystemLinqEnumerable_SelectArray_Converter
    {
        internal static Type RelevantType = typeof(System.Linq.Enumerable).GetNestedType("SelectArrayIterator`2", System.Reflection.BindingFlags.NonPublic);
    }

    public class SystemLinqEnumerable_SelectArray_Converter<Iterator, T> : ConverterFactoryDynamic
    {
        internal FieldInfo field_Source = typeof(Iterator).GetField("_source", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Selector = typeof(Iterator).GetField("_selector", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_State = typeof(Iterator).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Current = typeof(Iterator).GetField("_current", BindingFlags.NonPublic | BindingFlags.Instance);

        public override void Write(object input, Recorder recorder)
        {
            recorder.Shared().RecordPrivate(input, field_Source, "source");
            recorder.RecordPrivate(input, field_Selector, "selector");
            recorder.RecordPrivate(input, field_State, "state");
            recorder.SharedIfPossible<T>().RecordPrivate(input, field_Current, "current");
        }

        public override object Create(Recorder recorder)
        {
            return Activator.CreateInstance(typeof(Iterator), new object[] { null, null });
        }

        public override void Read(ref object input, Recorder recorder)
        {
            // it's the same code, we only need this for the funky Create
            Write(input, recorder);
        }
    }

    public static class SystemLinqEnumerable_SelectList_Converter
    {
        internal static Type RelevantType = typeof(System.Linq.Enumerable).GetNestedType("SelectListIterator`2", System.Reflection.BindingFlags.NonPublic);
    }

    public class SystemLinqEnumerable_SelectList_Converter<Iterator, T> : ConverterFactoryDynamic
    {
        internal FieldInfo field_Source = typeof(Iterator).GetField("_source", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Selector = typeof(Iterator).GetField("_selector", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Enumerator = typeof(Iterator).GetField("_enumerator", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_State = typeof(Iterator).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Current = typeof(Iterator).GetField("_current", BindingFlags.NonPublic | BindingFlags.Instance);

        public override void Write(object input, Recorder recorder)
        {
            recorder.Shared().RecordPrivate(input, field_Source, "source");
            recorder.RecordPrivate(input, field_Selector, "selector");
            recorder.RecordPrivate(input, field_Enumerator, "enumerator");
            recorder.RecordPrivate(input, field_State, "state");
            recorder.SharedIfPossible<T>().RecordPrivate(input, field_Current, "current");
        }

        public override object Create(Recorder recorder)
        {
            return Activator.CreateInstance(typeof(Iterator), new object[] { null, null });
        }

        public override void Read(ref object input, Recorder recorder)
        {
            // it's the same code, we only need this for the funky Create
            Write(input, recorder);
        }
    }

    public static class SystemLinqEnumerable_SelectRange_Converter
    {
        internal static Type RelevantType = typeof(System.Linq.Enumerable).GetNestedType("SelectRangeIterator`1", System.Reflection.BindingFlags.NonPublic);
    }

    public class SystemLinqEnumerable_SelectRange_Converter<Iterator, T> : ConverterFactoryDynamic
    {
        internal FieldInfo field_Start = typeof(Iterator).GetField("_start", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_End = typeof(Iterator).GetField("_end", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Selector = typeof(Iterator).GetField("_selector", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_State = typeof(Iterator).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Current = typeof(Iterator).GetField("_current", BindingFlags.NonPublic | BindingFlags.Instance);

        public override void Write(object input, Recorder recorder)
        {
            recorder.RecordPrivate(input, field_Start, "start");
            recorder.RecordPrivate(input, field_End, "end");
            recorder.RecordPrivate(input, field_Selector, "selector");
            recorder.RecordPrivate(input, field_State, "state");
            recorder.SharedIfPossible<T>().RecordPrivate(input, field_Current, "current");
        }

        public override object Create(Recorder recorder)
        {
            return Activator.CreateInstance(typeof(Iterator), new object[] { 0, 0, null });
        }

        public override void Read(ref object input, Recorder recorder)
        {
            // it's the same code, we only need this for the funky Create
            Write(input, recorder);
        }
    }

    public static class SystemLinqEnumerable_SelectMany_Converter
    {
        internal static Type RelevantType = typeof(System.Linq.Enumerable).GetNestedType("SelectManySingleSelectorIterator`2", System.Reflection.BindingFlags.NonPublic);
    }

    public class SystemLinqEnumerable_SelectMany_Converter<Iterator, T> : ConverterFactoryDynamic
    {
        internal FieldInfo field_Source = typeof(Iterator).GetField("_source", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Selector = typeof(Iterator).GetField("_selector", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_SourceEnumerator = typeof(Iterator).GetField("_sourceEnumerator", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_SubEnumerator = typeof(Iterator).GetField("_subEnumerator", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_State = typeof(Iterator).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Current = typeof(Iterator).GetField("_current", BindingFlags.NonPublic | BindingFlags.Instance);

        public override void Write(object input, Recorder recorder)
        {
            recorder.Shared().RecordPrivate(input, field_Source, "source");
            recorder.RecordPrivate(input, field_Selector, "selector");
            recorder.Shared().RecordPrivate(input, field_SourceEnumerator, "sourceEnumerator");
            recorder.Shared().RecordPrivate(input, field_SubEnumerator, "subEnumerator");
            recorder.RecordPrivate(input, field_State, "state");
            recorder.SharedIfPossible<T>().RecordPrivate(input, field_Current, "current");
        }

        public override object Create(Recorder recorder)
        {
            return Activator.CreateInstance(typeof(Iterator), BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { null, null }, null);
        }

        public override void Read(ref object input, Recorder recorder)
        {
            // it's the same code, we only need this for the funky Create
            Write(input, recorder);
        }
    }
}
