namespace Dec
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    internal class WriterNull : Writer
    {
        private bool allowReflection;
        public override bool AllowReflection { get => allowReflection; }

        internal WriterNull(bool allowReflection)
        {
            this.allowReflection = allowReflection;
        }
    }

    internal sealed class WriterNodeNull : WriterNode
    {
        private WriterNull writer;

        public override bool AllowReflection { get => writer.AllowReflection; }

        private WriterNodeNull(WriterNull writer)
        {
            this.writer = writer;
        }

        public static WriterNodeNull Start(WriterNull writer)
        {
            return new WriterNodeNull(writer);
        }

        public override WriterNode CreateChild(string label)
        {
            return new WriterNodeNull(writer);
        }

        public override WriterNode CreateMember(System.Reflection.FieldInfo field)
        {
            return new WriterNodeNull(writer);
        }

        public override void WritePrimitive(object value)
        {
            value.ToString();
        }

        public override void WriteEnum(object value)
        {
            value.ToString();
        }

        public override void WriteString(string value)
        {
            
        }

        public override void WriteType(Type value)
        {
            value.ComposeDecFormatted();
        }

        public override void WriteDec(Dec value)
        {
            
        }

        public override void TagClass(Type type)
        {
            type.ComposeDecFormatted();
        }

        public override void WriteExplicitNull()
        {
            
        }

        public override bool WriteReference(object value)
        {
            return false;
        }

        public override void WriteArray(Array value)
        {
            Type referencedType = value.GetType().GetElementType();

            for (int i = 0; i < value.Length; ++i)
            {
                Serialization.ComposeElement(CreateChild("li"), value.GetValue(i), referencedType);
            }
        }

        public override void WriteList(IList value)
        {
            Type referencedType = value.GetType().GetGenericArguments()[0];

            for (int i = 0; i < value.Count; ++i)
            {
                Serialization.ComposeElement(CreateChild("li"), value[i], referencedType);
            }
        }

        public override void WriteDictionary(IDictionary value)
        {
            Type keyType = value.GetType().GetGenericArguments()[0];
            Type valueType = value.GetType().GetGenericArguments()[1];

            IDictionaryEnumerator iterator = value.GetEnumerator();
            while (iterator.MoveNext())
            {
                var li = CreateChild("li");

                Serialization.ComposeElement(li.CreateChild("key"), iterator.Key, keyType);
                Serialization.ComposeElement(li.CreateChild("value"), iterator.Value, valueType);
            }
        }

        public override void WriteHashSet(IEnumerable value)
        {
            Type keyType = value.GetType().GetGenericArguments()[0];

            // I really want some way to canonicalize this ordering
            IEnumerator iterator = value.GetEnumerator();
            while (iterator.MoveNext())
            {
                // In theory, some sets support inline format, not li format. Inline format is cleaner and smaller and we should be using it when possible.
                // In practice, it's hard and I'm lazy and this always works, and we're not providing any guarantees about cleanliness of serialized output.
                // Revisit this later when someone (possibly myself) really wants it improved.
                Serialization.ComposeElement(CreateChild("li"), iterator.Current, keyType);
            }
        }

        public override void WriteRecord(IRecordable value)
        {
            value.Record(new RecorderWriter(this));
        }

        public override void WriteConvertible(Converter converter, object value)
        {
            converter.Record(value, value.GetType(), new RecorderWriter(this));
        }
    }
}
