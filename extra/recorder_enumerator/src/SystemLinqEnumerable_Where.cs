using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Dec.RecorderEnumerator
{
    public static class SystemLinqEnumerable_WhereEnumerable_Converter
    {
        internal static Type RelevantType = typeof(System.Linq.Enumerable).GetNestedType("WhereEnumerableIterator`1", System.Reflection.BindingFlags.NonPublic);
    }

    public class SystemLinqEnumerable_WhereEnumerable_Converter<Iterator, T> : ConverterFactoryDynamic
    {
        internal FieldInfo field_Source = typeof(Iterator).GetField("_source", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Predicate = typeof(Iterator).GetField("_predicate", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Enumerator = typeof(Iterator).GetField("_enumerator", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_State = typeof(Iterator).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Current = typeof(Iterator).GetField("_current", BindingFlags.NonPublic | BindingFlags.Instance);

        public override void Write(object input, Recorder recorder)
        {
            recorder.Shared().RecordPrivate(input, field_Source, "source");
            recorder.RecordPrivate(input, field_Predicate, "predicate");
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

    public static class SystemLinqEnumerable_WhereArray_Converter
    {
        internal static Type RelevantType = typeof(System.Linq.Enumerable).GetNestedType("WhereArrayIterator`1", System.Reflection.BindingFlags.NonPublic);
    }

    public class SystemLinqEnumerable_WhereArray_Converter<Iterator, T> : ConverterFactoryDynamic
    {
        internal FieldInfo field_Source = typeof(Iterator).GetField("_source", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Predicate = typeof(Iterator).GetField("_predicate", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_State = typeof(Iterator).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Current = typeof(Iterator).GetField("_current", BindingFlags.NonPublic | BindingFlags.Instance);

        public override void Write(object input, Recorder recorder)
        {
            recorder.Shared().RecordPrivate(input, field_Source, "source");
            recorder.RecordPrivate(input, field_Predicate, "predicate");
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

    public static class SystemLinqEnumerable_WhereList_Converter
    {
        internal static Type RelevantType = typeof(System.Linq.Enumerable).GetNestedType("WhereListIterator`1", System.Reflection.BindingFlags.NonPublic);
    }

    public class SystemLinqEnumerable_WhereList_Converter<Iterator, T> : ConverterFactoryDynamic
    {
        internal FieldInfo field_Source = typeof(Iterator).GetField("_source", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Predicate = typeof(Iterator).GetField("_predicate", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Enumerator = typeof(Iterator).GetField("_enumerator", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_State = typeof(Iterator).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Current = typeof(Iterator).GetField("_current", BindingFlags.NonPublic | BindingFlags.Instance);

        public override void Write(object input, Recorder recorder)
        {
            recorder.Shared().RecordPrivate(input, field_Source, "source");
            recorder.RecordPrivate(input, field_Predicate, "predicate");
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

    public static class SystemLinqEnumerable_WhereSelectIterator_Converter
    {
        internal static Type RelevantType = typeof(System.Linq.Enumerable).GetNestedType("WhereSelectEnumerableIterator`2", System.Reflection.BindingFlags.NonPublic);
    }

    public class SystemLinqEnumerable_WhereSelectIterator_Converter<Iterator, T> : ConverterFactoryDynamic
    {
        internal FieldInfo field_Source = typeof(Iterator).GetField("_source", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Predicate = typeof(Iterator).GetField("_predicate", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Selector = typeof(Iterator).GetField("_selector", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Enumerator = typeof(Iterator).GetField("_enumerator", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_State = typeof(Iterator).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Current = typeof(Iterator).GetField("_current", BindingFlags.NonPublic | BindingFlags.Instance);

        public override void Write(object input, Recorder recorder)
        {
            recorder.Shared().RecordPrivate(input, field_Source, "source");
            recorder.RecordPrivate(input, field_Predicate, "predicate");
            recorder.RecordPrivate(input, field_Selector, "selector");
            recorder.Shared().RecordPrivate(input, field_Enumerator, "enumerator");
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

    public static class SystemLinqEnumerable_WhereSelectArray_Converter
    {
        internal static Type RelevantType = typeof(System.Linq.Enumerable).GetNestedType("WhereSelectArrayIterator`2", System.Reflection.BindingFlags.NonPublic);
    }

    public class SystemLinqEnumerable_WhereSelectArray_Converter<Iterator, T> : ConverterFactoryDynamic
    {
        internal FieldInfo field_Source = typeof(Iterator).GetField("_source", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Predicate = typeof(Iterator).GetField("_predicate", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Selector = typeof(Iterator).GetField("_selector", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_State = typeof(Iterator).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Current = typeof(Iterator).GetField("_current", BindingFlags.NonPublic | BindingFlags.Instance);

        public override void Write(object input, Recorder recorder)
        {
            recorder.Shared().RecordPrivate(input, field_Source, "source");
            recorder.RecordPrivate(input, field_Predicate, "predicate");
            recorder.RecordPrivate(input, field_Selector, "selector");
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

    public static class SystemLinqEnumerable_WhereSelectList_Converter
    {
        internal static Type RelevantType = typeof(System.Linq.Enumerable).GetNestedType("WhereSelectListIterator`2", System.Reflection.BindingFlags.NonPublic);
    }

    public class SystemLinqEnumerable_WhereSelectList_Converter<Iterator, T> : ConverterFactoryDynamic
    {
        internal FieldInfo field_Source = typeof(Iterator).GetField("_source", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Predicate = typeof(Iterator).GetField("_predicate", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Selector = typeof(Iterator).GetField("_selector", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Enumerator = typeof(Iterator).GetField("_enumerator", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_State = typeof(Iterator).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Current = typeof(Iterator).GetField("_current", BindingFlags.NonPublic | BindingFlags.Instance);

        public override void Write(object input, Recorder recorder)
        {
            recorder.Shared().RecordPrivate(input, field_Source, "source");
            recorder.RecordPrivate(input, field_Predicate, "predicate");
            recorder.RecordPrivate(input, field_Selector, "selector");
            recorder.RecordPrivate(input, field_Enumerator, "enumerator");
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
}
