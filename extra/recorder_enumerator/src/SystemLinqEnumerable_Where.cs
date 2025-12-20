using System;
using System.Reflection;

namespace Dec.RecorderEnumerator
{
    public static class SystemLinqEnumerable_WhereEnumerable_Converter
    {
        // .NET 9 renamed WhereEnumerableIterator to IEnumerableWhereIterator
        internal static Type RelevantType = Util.GetLinqIteratorType("WhereEnumerableIterator`1", "IEnumerableWhereIterator`1");
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
        // .NET 9 renamed WhereArrayIterator to ArrayWhereIterator
        internal static Type RelevantType = Util.GetLinqIteratorType("WhereArrayIterator`1", "ArrayWhereIterator`1");
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
        // .NET 9 renamed WhereListIterator to ListWhereIterator
        internal static Type RelevantType = Util.GetLinqIteratorType("WhereListIterator`1", "ListWhereIterator`1");
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
        // .NET 9 renamed WhereSelectEnumerableIterator to IEnumerableWhereSelectIterator
        internal static Type RelevantType = Util.GetLinqIteratorType("WhereSelectEnumerableIterator`2", "IEnumerableWhereSelectIterator`2");
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
        // .NET 9 renamed WhereSelectArrayIterator to ArrayWhereSelectIterator
        internal static Type RelevantType = Util.GetLinqIteratorType("WhereSelectArrayIterator`2", "ArrayWhereSelectIterator`2");
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
        // .NET 9 renamed WhereSelectListIterator to ListWhereSelectIterator
        internal static Type RelevantType = Util.GetLinqIteratorType("WhereSelectListIterator`2", "ListWhereSelectIterator`2");
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
