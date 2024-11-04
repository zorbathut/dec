using System;
using System.Reflection;

namespace Dec.RecorderEnumerator
{
    public class SystemLinqEnumerable_RangeIterator_Converter : ConverterFactoryDynamic
    {
        internal static Type RelevantType = typeof(System.Linq.Enumerable).GetNestedType("RangeIterator", System.Reflection.BindingFlags.NonPublic);

        internal static FieldInfo Field_Current = RelevantType.GetField("_current", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static FieldInfo Field_State = RelevantType.GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static FieldInfo Field_Start = RelevantType.GetField("_start", BindingFlags.NonPublic | BindingFlags.Instance);
        internal static FieldInfo Field_End = RelevantType.GetField("_end", BindingFlags.NonPublic | BindingFlags.Instance);

        public override void Write(object input, Recorder recorder)
        {
            recorder.SharedIfPossible<int>().RecordPrivate(input, Field_Current, "current");
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
            recorder.SharedIfPossible<int>().RecordPrivate(input, Field_Current, "current");
            recorder.RecordPrivate(input, Field_State, "state");
        }
    }

    public static class SystemLinqEnumerable_DistinctIterator_Converter
    {
        internal static Type RelevantType = typeof(System.Linq.Enumerable).GetNestedType("DistinctIterator`1", System.Reflection.BindingFlags.NonPublic);
    }

    public class SystemLinqEnumerable_DistinctIterator_Converter<Iterator, T> : ConverterFactoryDynamic
    {
        internal FieldInfo field_Source = typeof(Iterator).GetField("_source", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Comparer = typeof(Iterator).GetField("_comparer", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Set = typeof(Iterator).GetField("_set", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Enumerator = typeof(Iterator).GetField("_enumerator", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_State = typeof(Iterator).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Current = typeof(Iterator).GetField("_current", BindingFlags.NonPublic | BindingFlags.Instance);

        public override void Write(object input, Recorder recorder)
        {
            recorder.Shared().RecordPrivate(input, field_Source, "source");
            recorder.Shared().RecordPrivate(input, field_Comparer, "comparer");
            recorder.RecordPrivate(input, field_Set, "selector");
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

    public static class SystemLinqEnumerable_UnionIterator2_Converter
    {
        internal static Type RelevantType = typeof(System.Linq.Enumerable).GetNestedType("UnionIterator2`1", System.Reflection.BindingFlags.NonPublic);
    }

    public class SystemLinqEnumerable_UnionIterator2_Converter<Iterator, T> : ConverterFactoryDynamic
    {
        internal FieldInfo field_First = typeof(Iterator).GetPrivateFieldInHierarchy("_first");
        internal FieldInfo field_Second = typeof(Iterator).GetPrivateFieldInHierarchy("_second");
        internal FieldInfo field_Comparer = typeof(Iterator).GetPrivateFieldInHierarchy("_comparer");
        internal FieldInfo field_Enumerator = typeof(Iterator).GetPrivateFieldInHierarchy("_enumerator");
        internal FieldInfo field_Set = typeof(Iterator).GetPrivateFieldInHierarchy("_set");
        internal FieldInfo field_State = typeof(Iterator).GetPrivateFieldInHierarchy("_state");
        internal FieldInfo field_Current = typeof(Iterator).GetPrivateFieldInHierarchy("_current");

        public override void Write(object input, Recorder recorder)
        {
            recorder.Shared().RecordPrivate(input, field_First, "first");
            recorder.Shared().RecordPrivate(input, field_Second, "second");
            recorder.Shared().RecordPrivate(input, field_Comparer, "comparer");
            recorder.Shared().RecordPrivate(input, field_Enumerator, "enumerator");
            recorder.RecordPrivate(input, field_Set, "selector");
            recorder.RecordPrivate(input, field_State, "state");
            recorder.SharedIfPossible<T>().RecordPrivate(input, field_Current, "current");
        }

        public override object Create(Recorder recorder)
        {
            return Activator.CreateInstance(typeof(Iterator), new object[] { null, null, null });
        }

        public override void Read(ref object input, Recorder recorder)
        {
            // it's the same code, we only need this for the funky Create
            Write(input, recorder);
        }
    }

    public static class SystemLinqEnumerable_UnionIteratorN_Converter
    {
        internal static Type RelevantType = typeof(System.Linq.Enumerable).GetNestedType("UnionIteratorN`1", System.Reflection.BindingFlags.NonPublic);
    }

    public class SystemLinqEnumerable_UnionIteratorN_Converter<Iterator, T> : ConverterFactoryDynamic
    {
        internal FieldInfo field_Sources = typeof(Iterator).GetPrivateFieldInHierarchy("_sources");
        internal FieldInfo field_HeadIndex = typeof(Iterator).GetPrivateFieldInHierarchy("_headIndex");
        internal FieldInfo field_Comparer = typeof(Iterator).GetPrivateFieldInHierarchy("_comparer");
        internal FieldInfo field_Enumerator = typeof(Iterator).GetPrivateFieldInHierarchy("_enumerator");
        internal FieldInfo field_Set = typeof(Iterator).GetPrivateFieldInHierarchy("_set");
        internal FieldInfo field_State = typeof(Iterator).GetPrivateFieldInHierarchy("_state");
        internal FieldInfo field_Current = typeof(Iterator).GetPrivateFieldInHierarchy("_current");

        public override void Write(object input, Recorder recorder)
        {
            recorder.Shared().RecordPrivate(input, field_Sources, "sources");
            recorder.RecordPrivate(input, field_HeadIndex, "headIndex");
            recorder.Shared().RecordPrivate(input, field_Comparer, "comparer");
            recorder.Shared().RecordPrivate(input, field_Enumerator, "enumerator");
            recorder.RecordPrivate(input, field_Set, "selector");
            recorder.RecordPrivate(input, field_State, "state");
            recorder.SharedIfPossible<T>().RecordPrivate(input, field_Current, "current");
        }

        public override object Create(Recorder recorder)
        {
            return Activator.CreateInstance(typeof(Iterator), new object[] { null, 0, null });
        }

        public override void Read(ref object input, Recorder recorder)
        {
            // it's the same code, we only need this for the funky Create
            Write(input, recorder);
        }
    }

    public static class SystemLinqEnumerable_ReverseIterator_Converter
    {
        internal static Type RelevantType = typeof(System.Linq.Enumerable).GetNestedType("ReverseIterator`1", System.Reflection.BindingFlags.NonPublic);
    }

    public class SystemLinqEnumerable_ReverseIterator_Converter<Iterator, T> : ConverterFactoryDynamic
    {
        internal FieldInfo field_Source = typeof(Iterator).GetPrivateFieldInHierarchy("_source");
        internal FieldInfo field_Buffer = typeof(Iterator).GetPrivateFieldInHierarchy("_buffer");
        internal FieldInfo field_State = typeof(Iterator).GetPrivateFieldInHierarchy("_state");
        internal FieldInfo field_Current = typeof(Iterator).GetPrivateFieldInHierarchy("_current");

        public override void Write(object input, Recorder recorder)
        {
            recorder.Shared().RecordPrivate(input, field_Source, "source");
            recorder.Shared().RecordPrivate(input, field_Buffer, "buffer");
            recorder.RecordPrivate(input, field_State, "state");
            recorder.SharedIfPossible<T>().RecordPrivate(input, field_Current, "current");
        }

        public override object Create(Recorder recorder)
        {
            return Activator.CreateInstance(typeof(Iterator), new object[] { null });
        }

        public override void Read(ref object input, Recorder recorder)
        {
            // it's the same code, we only need this for the funky Create
            Write(input, recorder);
        }
    }

    public static class SystemLinqEnumerable_Concat2Iterator_Converter
    {
        internal static Type RelevantType = typeof(System.Linq.Enumerable).GetNestedType("Concat2Iterator`1", System.Reflection.BindingFlags.NonPublic);
    }

    public class SystemLinqEnumerable_Concat2Iterator_Converter<Iterator, T> : ConverterFactoryDynamic
    {
        internal FieldInfo field_First = typeof(Iterator).GetPrivateFieldInHierarchy("_first");
        internal FieldInfo field_Second = typeof(Iterator).GetPrivateFieldInHierarchy("_second");
        internal FieldInfo field_Enumerator = typeof(Iterator).GetPrivateFieldInHierarchy("_enumerator");
        internal FieldInfo field_State = typeof(Iterator).GetPrivateFieldInHierarchy("_state");
        internal FieldInfo field_Current = typeof(Iterator).GetPrivateFieldInHierarchy("_current");

        internal ConstructorInfo constructor = typeof(Iterator).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)[0];

        public override void Write(object input, Recorder recorder)
        {
            recorder.Shared().RecordPrivate(input, field_First, "first");
            recorder.Shared().RecordPrivate(input, field_Second, "second");
            recorder.Shared().RecordPrivate(input, field_Enumerator, "enumerator");
            recorder.RecordPrivate(input, field_State, "state");
            recorder.SharedIfPossible<T>().RecordPrivate(input, field_Current, "current");
        }

        public override object Create(Recorder recorder)
        {
            return constructor.Invoke(new object[] { null, null });
        }

        public override void Read(ref object input, Recorder recorder)
        {
            // it's the same code, we only need this for the funky Create
            Write(input, recorder);
        }
    }

    public static class SystemLinqEnumerable_ConcatNIterator_Converter
    {
        internal static Type RelevantType = typeof(System.Linq.Enumerable).GetNestedType("ConcatNIterator`1", System.Reflection.BindingFlags.NonPublic);
    }

    public class SystemLinqEnumerable_ConcatNIterator_Converter<Iterator, T> : ConverterFactoryDynamic
    {
        internal FieldInfo field_Tail = typeof(Iterator).GetPrivateFieldInHierarchy("_tail");
        internal FieldInfo field_Head = typeof(Iterator).GetPrivateFieldInHierarchy("_head");
        internal FieldInfo field_HeadIndex = typeof(Iterator).GetPrivateFieldInHierarchy("_headIndex");
        internal FieldInfo field_HasOnlyCollections = typeof(Iterator).GetPrivateFieldInHierarchy("_hasOnlyCollections");
        internal FieldInfo field_Enumerator = typeof(Iterator).GetPrivateFieldInHierarchy("_enumerator");
        internal FieldInfo field_State = typeof(Iterator).GetPrivateFieldInHierarchy("_state");
        internal FieldInfo field_Current = typeof(Iterator).GetPrivateFieldInHierarchy("_current");

        internal ConstructorInfo constructor = typeof(Iterator).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)[0];

        public override void Write(object input, Recorder recorder)
        {
            recorder.Shared().RecordPrivate(input, field_Tail, "tail");
            recorder.Shared().RecordPrivate(input, field_Head, "head");
            recorder.RecordPrivate(input, field_HeadIndex, "headIndex");
            recorder.RecordPrivate(input, field_HasOnlyCollections, "hasOnlyCollections");
            recorder.Shared().RecordPrivate(input, field_Enumerator, "enumerator");
            recorder.RecordPrivate(input, field_State, "state");
            recorder.SharedIfPossible<T>().RecordPrivate(input, field_Current, "current");
        }

        public override object Create(Recorder recorder)
        {
            return constructor.Invoke(new object[] { null, null, 0, false });
        }

        public override void Read(ref object input, Recorder recorder)
        {
            // it's the same code, we only need this for the funky Create
            Write(input, recorder);
        }
    }
}
