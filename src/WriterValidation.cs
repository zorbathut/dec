namespace Dec
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml.Linq;

    internal abstract class WriterValidation : Writer
    {
        private StringBuilder sb = new StringBuilder();

        internal Dictionary<object, string> referenceLookup = new Dictionary<object, string>();
        private WriterUtil.PendingWriteCoordinator pendingWriteCoordinator = new WriterUtil.PendingWriteCoordinator();

        public void AppendLine(string line)
        {
            sb.AppendLine(line);
        }

        public void RegisterPendingWrite(Action action)
        {
            pendingWriteCoordinator.RegisterPendingWrite(action);
        }

        public string Finish()
        {
            pendingWriteCoordinator.DequeuePendingWrites();

            return sb.ToString();
        }
    }

    internal class WriterValidationCompose : WriterValidation
    {
        public override bool AllowReflection { get => true; }

        public WriterNode StartDec(Type type, string decName)
        {
            return new WriterNodeValidation(this, $"Dec.Database<{type.ComposeCSFormatted()}>.Get(\"{decName}\")");
        }
    }

    internal class WriterValidationRecord : WriterValidation
    {
        public override bool AllowReflection { get => false; }

        public WriterNode StartData()
        {
            return new WriterNodeValidation(this, $"input");
        }
    }

    // This is used for things that can be expressed as an easy inline string, which is used as part of the Dictionary-handling code.
    internal abstract class WriterNodeCS : WriterNode
    {
        public abstract void WriteToken(string token);

        public override void WritePrimitive(object value)
        {
            if (value.GetType() == typeof(bool))
            {
                WriteToken(value.ToString().ToLower());
            }
            else if (value.GetType() == typeof(float))
            {
                var val = (float)value;
                if (float.IsNaN(val))
                {
                    WriteToken("float.NaN");
                }
                else if (float.IsPositiveInfinity(val))
                {
                    WriteToken("float.PositiveInfinity");
                }
                else if (float.IsNegativeInfinity(val))
                {
                    WriteToken("float.NegativeInfinity");
                }
                else
                {
                    WriteToken(((float)value).ToString("G17") + 'f');
                }
            }
            else if (value.GetType() == typeof(double))
            {
                var val = (double)value;
                if (double.IsNaN(val))
                {
                    WriteToken("double.NaN");
                }
                else if (double.IsPositiveInfinity(val))
                {
                    WriteToken("double.PositiveInfinity");
                }
                else if (double.IsNegativeInfinity(val))
                {
                    WriteToken("double.NegativeInfinity");
                }
                else
                {
                    WriteToken(((double)value).ToString("G17") + 'd');
                }
            }
            else
            {
                WriteToken(value.ToString());
            }
        }

        public override void WriteEnum(object value)
        {
            WriteToken($"{value.GetType().ComposeCSFormatted()}.{value}");
        }

        public override void WriteString(string value)
        {
            WriteToken($"\"{value}\"");
        }

        public override void WriteType(Type value)
        {
            WriteToken($"typeof({value.ComposeCSFormatted()})");
        }

        public override void WriteDec(Dec value)
        {
            if (value != null)
            {
                WriteToken($"Dec.Database<{value.GetType().ComposeCSFormatted()}>.Get(\"{value.DecName}\")");
            }
            else
            {
                WriteExplicitNull();
            }
        }

        public override XElement GetXElement()
        {
            // we don't have one
            return null;
        }
    }

    internal sealed class WriterNodeValidation : WriterNodeCS
    {
        private WriterValidation writer;
        private string accessor;

        public override bool AllowReflection { get => writer.AllowReflection; }

        public WriterNodeValidation(WriterValidation writer, string accessor)
        {
            this.writer = writer;
            this.accessor = accessor;
        }

        public override WriterNode CreateChild(string label)
        {
            return new WriterNodeValidation(writer, $"{accessor}.{label}");
        }

        public override WriterNode CreateMember(System.Reflection.FieldInfo field)
        {
            if (field.IsPublic)
            {
                return new WriterNodeValidation(writer, $"{accessor}.{field.Name}");
            }
            else
            {
                return new WriterNodeValidation(writer, $"(({field.FieldType.ComposeCSFormatted()})typeof({field.DeclaringType.ComposeCSFormatted()}).GetField(\"{field.Name}\", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue({accessor}))");
            }
        }

        private void WriteIsEqual(string value)
        {
            writer.AppendLine($"Assert.AreEqual({value}, {accessor});");
        }

        public override void WriteToken(string token)
        {
            WriteIsEqual(token);
        }

        public override void TagClass(Type type)
        {
            if (type == typeof(Type))
            {
                // Special case: Sometimes we convert System.RuntimeType to System.Type, because System.RuntimeType is a compatible C# implementation detail that we don't want to preserve.
                // We handle that conversion here as well.
                writer.AppendLine($"Assert.IsTrue({accessor} is System.Type);");
            }
            else
            {
                writer.AppendLine($"Assert.AreEqual(typeof({type.ComposeCSFormatted()}), {accessor}.GetType());");
            }

            accessor = $"(({type.ComposeCSFormatted()}){accessor})";
        }

        public override void WriteExplicitNull()
        {
            writer.AppendLine($"Assert.IsNull({accessor});");
        }

        public override bool WriteReference(object value)
        {
            if (writer.referenceLookup.ContainsKey(value))
            {
                writer.AppendLine($"Assert.AreSame({writer.referenceLookup[value]}, {accessor});");
                return true;
            }
            else
            {
                writer.referenceLookup[value] = accessor;
                return false;
            }
        }

        public override void WriteRecord(IRecordable value)
        {
            // For performance's sake we shouldn't always be doing this.
            // But the validation layer is already incredibly slow and none of this is relevant in terms of performance anyway.
            // So, whatever.
            writer.RegisterPendingWrite(() => value.Record(new RecorderWriter(this)));
        }

        public override void WriteArray(Array value)
        {
            Type referencedType = value.GetType().GetElementType();

            // Maybe this should just be a giant AreEqual with a dynamically allocated array?
            writer.AppendLine($"Assert.AreEqual({accessor}.Length, {value.Length});");
            writer.AppendLine($"if ({accessor}.Length == {value.Length}) {{");

            for (int i = 0; i < value.Length; ++i)
            {
                Serialization.ComposeElement(new WriterNodeValidation(writer, $"{accessor}[{i}]"), value.GetValue(i), referencedType);
            }

            writer.AppendLine($"}}");
        }

        public override void WriteList(IList value)
        {
            Type referencedType = value.GetType().GetGenericArguments()[0];

            // Maybe this should just be a giant AreEqual with a dynamically allocated list?
            writer.AppendLine($"Assert.AreEqual({accessor}.Count, {value.Count});");
            writer.AppendLine($"if ({accessor}.Count == {value.Count}) {{");

            for (int i = 0; i < value.Count; ++i)
            {
                Serialization.ComposeElement(new WriterNodeValidation(writer, $"{accessor}[{i}]"), value[i], referencedType);
            }

            writer.AppendLine($"}}");
        }

        public override void WriteDictionary(IDictionary value)
        {
            Type keyType = value.GetType().GetGenericArguments()[0];
            Type valueType = value.GetType().GetGenericArguments()[1];

            writer.AppendLine($"Assert.AreEqual({accessor}.Count, {value.Count});");

            IDictionaryEnumerator iterator = value.GetEnumerator();
            while (iterator.MoveNext())
            {
                var keyNode = new WriterNodeStringize();
                Serialization.ComposeElement(keyNode, iterator.Key, keyType);

                writer.AppendLine($"if ({accessor}.ContainsKey({keyNode.SerializedString})) {{");
                Serialization.ComposeElement(new WriterNodeValidation(writer, $"{accessor}[{keyNode.SerializedString}]"), iterator.Value, valueType);
                writer.AppendLine($"}} else {{");
                writer.AppendLine($"Assert.IsTrue({accessor}.ContainsKey({keyNode.SerializedString}));");   // this is unnecessary - it could just be .Fail() - but this gives you a *much* better error message
                writer.AppendLine($"}}");
            }
        }

        public override void WriteHashSet(IEnumerable value)
        {
            Type keyType = value.GetType().GetGenericArguments()[0];

            int count = 0;
            IEnumerator iterator = value.GetEnumerator();
            while (iterator.MoveNext())
            {
                var keyNode = new WriterNodeStringize();
                Serialization.ComposeElement(keyNode, iterator.Current, keyType);

                // You might think "Assert.Contains" would do what we want, but it doesn't - it requires an ICollection and HashSet isn't an ICollection.
                writer.AppendLine($"Assert.IsTrue({accessor}.Contains({keyNode.SerializedString}));");

                ++count;
            }

            writer.AppendLine($"Assert.AreEqual({accessor}.Count, {count});");
        }

        public override void WriteConvertible(Converter converter, object value, Type fieldType)
        {
            // this isn't really a thing I can implement because the entire point of this is to compare the output to known values
            // and if we're going through Converter, we don't know what the underlying known values will be
            throw new NotImplementedException();
        }
    }

    // This is used solely for dict keys, because we want to get a reasonably-stringized version of this without having to jump through hideous hoops.
    internal sealed class WriterNodeStringize : WriterNodeCS
    {
        public override bool AllowReflection { get => false; }

        public string SerializedString { get; private set; }

        public override void WriteToken(string token)
        {
            if (SerializedString != null)
            {
                Dbg.Err("String is already set!");
            }

            SerializedString = token;
        }

        public override WriterNode CreateChild(string label)
        {
            throw new NotImplementedException();
        }

        public override WriterNode CreateMember(System.Reflection.FieldInfo field)
        {
            throw new NotImplementedException();
        }

        public override void TagClass(Type type)
        {
            throw new NotImplementedException();
        }

        public override void WriteExplicitNull()
        {
            throw new NotImplementedException();
        }

        public override bool WriteReference(object value)
        {
            throw new NotImplementedException();
        }

        public override void WriteRecord(IRecordable value)
        {
            throw new NotImplementedException();
        }

        public override void WriteArray(Array value)
        {
            throw new NotImplementedException();
        }

        public override void WriteList(IList value)
        {
            throw new NotImplementedException();
        }

        public override void WriteDictionary(IDictionary value)
        {
            throw new NotImplementedException();
        }

        public override void WriteHashSet(IEnumerable value)
        {
            throw new NotImplementedException();
        }

        public override void WriteConvertible(Converter converter, object value, Type fieldType)
        {
            throw new NotImplementedException();
        }
    }
}

