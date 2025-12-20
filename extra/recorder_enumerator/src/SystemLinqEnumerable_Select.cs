using System;
using System.Reflection;

namespace Dec.RecorderEnumerator
{
    public static class SystemLinqEnumerable_SelectEnumerable_Converter
    {
        // .NET 9 renamed SelectEnumerableIterator to IEnumerableSelectIterator
        internal static Type RelevantType = Util.GetLinqIteratorType("SelectEnumerableIterator`2", "IEnumerableSelectIterator`2");
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
        // .NET 9 renamed SelectArrayIterator to ArraySelectIterator
        internal static Type RelevantType = Util.GetLinqIteratorType("SelectArrayIterator`2", "ArraySelectIterator`2");
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
        // .NET 9 renamed SelectListIterator to ListSelectIterator
        internal static Type RelevantType = Util.GetLinqIteratorType("SelectListIterator`2", "ListSelectIterator`2");
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
        // .NET 9 renamed SelectRangeIterator to RangeSelectIterator
        internal static Type RelevantType = Util.GetLinqIteratorType("SelectRangeIterator`1", "RangeSelectIterator`1");
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

    public static class SystemLinqEnumerable_SelectIPartition_Converter
    {
        // .NET 9 replaced SelectIPartitionIterator with IListSkipTakeSelectIterator and IteratorSelectIterator
        internal static Type RelevantType = Util.GetLinqIteratorType("SelectIPartitionIterator`2", "IListSkipTakeSelectIterator`2");
    }

    public class SystemLinqEnumerable_SelectIPartition_Converter<Iterator, T> : ConverterFactoryDynamic
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

    // .NET 9 IteratorSelectIterator - used when selecting from another iterator (e.g., OrderBy().Select())
    public static class SystemLinqEnumerable_IteratorSelectIterator_Converter
    {
        internal static Type RelevantType = Util.GetLinqIteratorType("IteratorSelectIterator`2");
    }

    public class SystemLinqEnumerable_IteratorSelectIterator_Converter<Iterator, T> : ConverterFactoryDynamic
    {
        internal FieldInfo field_Source = typeof(Iterator).GetPrivateFieldInHierarchy("_source");
        internal FieldInfo field_Selector = typeof(Iterator).GetPrivateFieldInHierarchy("_selector");
        internal FieldInfo field_Enumerator = typeof(Iterator).GetPrivateFieldInHierarchy("_enumerator");
        internal FieldInfo field_State = typeof(Iterator).GetPrivateFieldInHierarchy("_state");
        internal FieldInfo field_Current = typeof(Iterator).GetPrivateFieldInHierarchy("_current");

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
            Write(input, recorder);
        }
    }
}
