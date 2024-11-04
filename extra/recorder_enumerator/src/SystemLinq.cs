using System;
using System.Linq;
using System.Reflection;

namespace Dec.RecorderEnumerator
{
    public static class SystemLinq_SingleLinkedNode_Converter
    {
        internal static Type RelevantType = typeof(System.Linq.Enumerable).Assembly.GetType("System.Linq.SingleLinkedNode`1");
    }

    public class SystemLinq_SingleLinkedNode_Converter<Node, T> : ConverterFactoryDynamic
    {
        internal FieldInfo field_Item = typeof(Node).GetPrivateFieldInHierarchy("<Item>k__BackingField");
        internal FieldInfo field_Linked = typeof(Node).GetPrivateFieldInHierarchy("<Linked>k__BackingField");

        public override void Write(object input, Recorder recorder)
        {
            recorder.Shared().RecordPrivate(input, field_Item, "item");
            recorder.Shared().RecordPrivate(input, field_Linked, "linked");
        }

        public override object Create(Recorder recorder)
        {
            return Activator.CreateInstance(typeof(Node), new object[] { default(T) });
        }

        public override void Read(ref object input, Recorder recorder)
        {
            // it's the same code, we only need this for the funky Create
            Write(input, recorder);
        }
    }

    public static class SystemLinq_Buffer_Converter
    {
        internal static Type RelevantType = typeof(System.Linq.Enumerable).Assembly.GetType("System.Linq.Buffer`1");
    }

    public class SystemLinq_Buffer_Converter<Node, T> : ConverterFactoryDynamic
    {
        internal FieldInfo field_Item = typeof(Node).GetPrivateFieldInHierarchy("_items");
        internal FieldInfo field_Count = typeof(Node).GetPrivateFieldInHierarchy("_count");

        public override void Write(object input, Recorder recorder)
        {
            recorder.Shared().RecordPrivate(input, field_Item, "item");
            recorder.RecordPrivate(input, field_Count, "count");
        }

        public override object Create(Recorder recorder)
        {
            // private constructor requires jumping through some hoops
            var constructor = typeof(Node).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(System.Collections.Generic.IEnumerable<T>) }, null);
            return constructor.Invoke(new object[] { Enumerable.Empty<T>() });
        }

        public override void Read(ref object input, Recorder recorder)
        {
            // it's the same code, we only need this for the funky Create
            Write(input, recorder);
        }
    }

    public static class SystemLinq_OrderedEnumerable_Converter
    {
        internal static Type RelevantType = typeof(System.Linq.Enumerable).Assembly.GetType("System.Linq.OrderedEnumerable`2");
    }

    public class SystemLinq_OrderedEnumerable_Converter<Iterator, T, K> : ConverterFactoryDynamic
    {
        internal FieldInfo field_Parent = typeof(Iterator).GetPrivateFieldInHierarchy("_parent");
        internal FieldInfo field_KeySelector = typeof(Iterator).GetPrivateFieldInHierarchy("_keySelector");
        internal FieldInfo field_Comparer = typeof(Iterator).GetPrivateFieldInHierarchy("_comparer");
        internal FieldInfo field_Descending = typeof(Iterator).GetPrivateFieldInHierarchy("_descending");
        internal FieldInfo field_Source = typeof(Iterator).GetPrivateFieldInHierarchy("_source");

        internal ConstructorInfo constructor = typeof(Iterator).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)[0];

        public override void Write(object input, Recorder recorder)
        {
            recorder.RecordPrivate(input, field_Parent, "_parent");
            recorder.RecordPrivate(input, field_KeySelector, "_keySelector");
            recorder.Shared().RecordPrivate(input, field_Comparer, "_comparer");
            recorder.RecordPrivate(input, field_Descending, "_descending");
            recorder.Shared().RecordPrivate(input, field_Source, "_source");
        }

        private static K DefaultKeySelector(T t) => default;

        public override object Create(Recorder recorder)
        {
            // "Converting method group 'DefaultKeySelector' to non-delegate type 'object'. Did you intend to invoke the method?" no, no I didn't, actually
            #pragma warning disable CS8974
            return constructor.Invoke(new object[] { Enumerable.Empty<T>(), DefaultKeySelector, null, false, null });
            #pragma warning restore CS8974
        }

        public override void Read(ref object input, Recorder recorder)
        {
            // it's the same code, we only need this for the funky Create
            Write(input, recorder);
        }
    }
}
