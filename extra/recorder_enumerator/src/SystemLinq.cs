namespace Dec.RecorderEnumerator
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;

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
}
