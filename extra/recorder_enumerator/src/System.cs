using System;
using System.Reflection;

namespace Dec.RecorderEnumerator
{
    public class System_ArrayEnumerator_Converter : ConverterFactoryDynamic
    {
        internal static Type RelevantType = typeof(System.Array).Assembly.GetType("System.ArrayEnumerator");

        internal FieldInfo field_Array = RelevantType.GetPrivateFieldInHierarchy("_array");
        internal FieldInfo field_Index = RelevantType.GetPrivateFieldInHierarchy("_index");

        internal ConstructorInfo constructor = RelevantType.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)[0];

        public override void Write(object input, Recorder recorder)
        {
            recorder.Shared().RecordPrivate(input, field_Array, "array");

            // I assume this is an IntPtr just to avoid taking up more space on 32-bit systems
            // but, seriously guys, wat
            IntPtr intPtr = (IntPtr)field_Index.GetValue(input);
            long intPtrValue = intPtr.ToInt64();
            recorder.Record(ref intPtrValue, "index");
        }

        public override object Create(Recorder recorder)
        {
            return constructor.Invoke(new object[] { null });
        }

        public override void Read(ref object input, Recorder recorder)
        {
            recorder.Shared().RecordPrivate(input, field_Array, "array");

            long intPtrValue = 0;
            recorder.Record(ref intPtrValue, "index");
            IntPtr intPtr = new IntPtr(intPtrValue);
            field_Index.SetValue(input, intPtr);
        }
    }

    public static class System_SZGenericArrayEnumerator_Converter
    {
        internal static Type RelevantType = typeof(System.Array).Assembly.GetType("System.SZGenericArrayEnumerator`1");
    }

    public class System_SZGenericArrayEnumerator_Converter<Iterator> : ConverterFactoryDynamic
    {
        internal FieldInfo field_Array = typeof(Iterator).GetPrivateFieldInHierarchy("_array");
        internal FieldInfo field_Index = typeof(Iterator).GetPrivateFieldInHierarchy("_index");

        public override void Write(object input, Recorder recorder)
        {
            recorder.Shared().RecordPrivate(input, field_Array, "array");
            recorder.RecordPrivate(input, field_Index, "index");
        }

        public override object Create(Recorder recorder)
        {
            // I am frankly bewildered as to why Activator.CreateInstance() doesn't work here.
            var cs = typeof(Iterator).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);
            return cs[0].Invoke(new object[] { null });
        }

        public override void Read(ref object input, Recorder recorder)
        {
            Write(input, recorder);
        }
    }
}
