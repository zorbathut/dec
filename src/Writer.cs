namespace Def
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    internal abstract class Writer
    {
        public abstract bool AllowReflection { get; }
    }

    internal abstract class WriterNode
    {
        public abstract Writer Writer { get; }

        // this needs to be more abstract
        public abstract WriterNode CreateChild(string label);

        public abstract void WritePrimitive(object value);
        public abstract void WriteString(string value);
        public abstract void WriteType(Type value);
        public abstract void WriteDef(Def value);
        public abstract void WriteExplicitNull();
        public abstract bool WriteReference(object value);
        public abstract void WriteRecord(IRecordable value);
        public abstract void WriteConvertable(Converter converter, object value, Type fieldType);

        public abstract void TagClass(Type type);

        // get rid of me
        public abstract XElement GetXElement();
    }
}
