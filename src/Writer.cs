namespace Dec
{
    using System;
    using System.Collections;
    using System.Xml.Linq;

    internal abstract class Writer
    {
        public abstract bool AllowReflection { get; }
    }

    internal abstract class WriterNode
    {
        public abstract bool AllowReflection { get; }

        // this needs to be more abstract
        public abstract WriterNode CreateChild(string label);

        public abstract WriterNode CreateMember(System.Reflection.FieldInfo field);

        public abstract void WritePrimitive(object value);
        public abstract void WriteEnum(object value);
        public abstract void WriteString(string value);
        public abstract void WriteType(Type value);
        public abstract void WriteDec(Dec value);
        public abstract void WriteExplicitNull();
        public abstract bool WriteReference(object value);
        public abstract void WriteArray(Array value);
        public abstract void WriteList(IList value);
        public abstract void WriteDictionary(IDictionary value);
        public abstract void WriteHashSet(IEnumerable value);
        public abstract void WriteRecord(IRecordable value);
        public abstract void WriteConvertible(Converter converter, object value);

        public abstract void TagClass(Type type);
    }
}
