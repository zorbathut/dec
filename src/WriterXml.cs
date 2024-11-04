using System;
using System.Collections;
using System.Xml.Linq;

namespace Dec
{
    internal abstract class WriterXml
    {
        // A list of writes that still have to happen. This is used so we don't have to do deep recursive dives and potentially blow our stack.
        // I think this is only used for WriterXmlRecord, but right now this all goes through WriterNodeXml which is meant to work with both of these.
        // The inheritance tree is kind of messed up right now and should be fixed.
        private WriterUtil.PendingWriteCoordinator pendingWriteCoordinator = new WriterUtil.PendingWriteCoordinator();

        public abstract bool AllowReflection { get; }
        public abstract Recorder.IUserSettings UserSettings { get; }

        public abstract bool RegisterReference(object referenced, XElement element, Recorder.Context recContext);

        public void RegisterPendingWrite(Action action)
        {
            pendingWriteCoordinator.RegisterPendingWrite(action);
        }

        public void DequeuePendingWrites()
        {
            pendingWriteCoordinator.DequeuePendingWrites();
        }
    }

    internal sealed class WriterNodeXml : WriterNode
    {
        private WriterXml writer;
        private XElement node;

        // Represents only the *active* depth in the program stack.
        // This is kind of painfully hacky, because when it's created, we don't know if it's going to represent a new stack start.
        // So we just kinda adjust it as we go.
        private int depth;
        private const int MaxRecursionDepth = 100;

        public override bool AllowReflection { get => writer.AllowReflection; }
        public override Recorder.IUserSettings UserSettings { get => writer.UserSettings; }

        private WriterNodeXml(WriterXml writer, XContainer parent, string label, int depth, Recorder.Context context) : base(context)
        {
            this.writer = writer;
            this.depth = depth;

            node = new XElement(label);
            parent.Add(node);
        }

        public static WriterNodeXml StartDec(WriterXmlCompose writer, XContainer decRoot, string type, string decName)
        {
            var node = new WriterNodeXml(writer, decRoot, type, 0, new Recorder.Context());
            node.GetXElement().Add(new XAttribute("decName", decName));
            return node;
        }

        public static WriterNodeXml StartData(WriterXml writer, XContainer decRoot, string name, Type type)
        {
            return new WriterNodeXml(writer, decRoot, name, 0, new Recorder.Context() { shared = Recorder.Context.Shared.Flexible });
        }

        internal WriterNodeXml CreateNamedChild(string label, Recorder.Context context)
        {
            return new WriterNodeXml(writer, node, label, depth + 1, context);
        }

        // this should be WriterNodeXml but this C# doesn't support that
        public override WriterNode CreateRecorderChild(string label, Recorder.Context context)
        {
            return new WriterNodeXml(writer, node, label, depth + 1, context);
        }

        // this should be WriterNodeXml but this C# doesn't support that
        public override WriterNode CreateReflectionChild(System.Reflection.FieldInfo field, Recorder.Context context)
        {
            return new WriterNodeXml(writer, node, field.Name, depth + 1, context);
        }

        public override void WritePrimitive(object value)
        {
            if (value.GetType() == typeof(double))
            {
                double val = (double)value;
                if (double.IsNaN(val) && BitConverter.DoubleToInt64Bits(val) != BitConverter.DoubleToInt64Bits(double.NaN))
                {
                    // oops, all nan boxing!
                    node.Add(new XText("NaNbox" + BitConverter.DoubleToInt64Bits(val).ToString("X16")));
                }
                else if (Compat.FloatRoundtripBroken)
                {
                    node.Add(new XText(val.ToString("G17")));
                }
                else
                {
                    node.Add(new XText(val.ToString()));
                }
            }
            else if (value.GetType() == typeof(float))
            {
                float val = (float)value;
                if (float.IsNaN(val) && BitConverter.SingleToInt32Bits(val) != BitConverter.SingleToInt32Bits(float.NaN))
                {
                    // oops, all nan boxing!
                    node.Add(new XText("NaNbox" + BitConverter.SingleToInt32Bits(val).ToString("X8")));
                }
                else if (Compat.FloatRoundtripBroken)
                {
                    node.Add(new XText(val.ToString("G9")));
                }
                else
                {
                    node.Add(new XText(val.ToString()));
                }
            }
            else
            {
                node.Add(new XText(value.ToString()));
            }
        }

        public override void WriteEnum(object value)
        {
            node.Add(new XText(value.ToString()));
        }

        public override void WriteString(string value)
        {
            node.Add(new XText(value));
        }

        public override void WriteType(Type value)
        {
            node.Add(new XText(value.ComposeDecFormatted()));
        }

        public override void WriteDec(Dec value)
        {
            // Get the dec name and be done with it.
            if (value == null)
            {
                // "No data" is defined as null for decs, so we just do that
            }
            else if (value.DecName == "" || value.DecName == null)
            {
                Dbg.Err($"Attempted to write a Dec that was dynamically created but never registered; this will be left as a null reference. In most cases you shouldn't be dynamically creating Decs anyway, this is likely a malfunctioning deep copy such as a misbehaving ICloneable");
            }
            else if (value != Database.Get(value.GetType(), value.DecName))
            {
                Dbg.Err($"Referenced dec `{value}` does not exist in the database; serializing an error value instead");
                node.Add(new XText($"{value.DecName}_DELETED"));

                // if you actually have a dec named SomePreviouslyExistingDec_DELETED then you need to sort out what you're doing with your life
            }
            else
            {
                node.Add(new XText(value.DecName));
            }
        }

        public override void TagClass(Type type)
        {
            // I guess we just keep going? what's likely to be less damaging here? this may at least be manually reconstructible I suppose?
            FlagAsClass();

            node.Add(new XAttribute("class", type.ComposeDecFormatted()));
        }

        public override void WriteExplicitNull()
        {
            node.SetAttributeValue("null", "true");
        }

        public override bool WriteReference(object value)
        {
            return writer.RegisterReference(value, node, RecorderContext);
        }

        private void WriteArrayRank(WriterNodeXml node, Array value, Type referencedType, int rank, int[] indices)
        {
            if (rank == value.Rank)
            {
                Serialization.ComposeElement(node, value.GetValue(indices), referencedType);
            }
            else
            {
                for (int i = 0; i < value.GetLength(rank); ++i)
                {
                    var child = node.CreateNamedChild("li", RecorderContext.CreateChild());

                    indices[rank] = i;
                    WriteArrayRank(child, value, referencedType, rank + 1, indices);
                }
            }
        }

        public override void WriteArray(Array value)
        {
            Type referencedType = value.GetType().GetElementType();

            if (value.Rank == 1)
            {
                // fast path
                for (int i = 0; i < value.Length; ++i)
                {
                    Serialization.ComposeElement(CreateNamedChild("li", RecorderContext.CreateChild()), value.GetValue(i), referencedType);
                }

                return;
            }
            else
            {
                // slow path
                int[] indices = new int[value.Rank];
                WriteArrayRank(this, value, referencedType, 0, indices);
            }
        }

        public override void WriteList(IList value)
        {
            Type referencedType = value.GetType().GetGenericArguments()[0];

            for (int i = 0; i < value.Count; ++i)
            {
                Serialization.ComposeElement(CreateNamedChild("li", RecorderContext.CreateChild()), value[i], referencedType);
            }
        }

        public override void WriteDictionary(IDictionary value)
        {
            Type keyType = value.GetType().GetGenericArguments()[0];
            Type valueType = value.GetType().GetGenericArguments()[1];

            // I really want some way to canonicalize this ordering
            IDictionaryEnumerator iterator = value.GetEnumerator();
            while (iterator.MoveNext())
            {
                // In theory, some dicts support inline format, not li format. Inline format is cleaner and smaller and we should be using it when possible.
                // In practice, it's hard and I'm lazy and this always works, and we're not providing any guarantees about cleanliness of serialized output.
                // Revisit this later when someone (possibly myself) really wants it improved.
                var li = CreateNamedChild("li", RecorderContext);

                Serialization.ComposeElement(li.CreateNamedChild("key", RecorderContext.CreateChild()), iterator.Key, keyType);
                Serialization.ComposeElement(li.CreateNamedChild("value", RecorderContext.CreateChild()), iterator.Value, valueType);
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
                Serialization.ComposeElement(CreateNamedChild("li", RecorderContext.CreateChild()), iterator.Current, keyType);
            }
        }

        public override void WriteQueue(IEnumerable value)
        {
            // We actually just treat this like an array right now; it's the same behavior and it's easier
            Type keyType = value.GetType().GetGenericArguments()[0];
            var array = value.GetType().GetMethod("ToArray").Invoke(value, new object[] { }) as Array;

            WriteArray(array);
        }

        public override void WriteStack(IEnumerable value)
        {
            // We actually just treat this like an array right now; it's the same behavior and it's easier
            Type keyType = value.GetType().GetGenericArguments()[0];
            var array = value.GetType().GetMethod("ToArray").Invoke(value, new object[] { }) as Array;

            // For some reason this writes it out to an array in the reverse order than I'd expect
            // (and also the reverse order it inputs in!)
            // so, uh, time to munge
            Array.Reverse(array);

            WriteArray(array);
        }

        public override void WriteTuple(object value, System.Runtime.CompilerServices.TupleElementNamesAttribute names)
        {
            var args = value.GetType().GenericTypeArguments;
            var length = args.Length;

            var nameArray = names?.TransformNames;

            for (int i = 0; i < length; ++i)
            {
                Serialization.ComposeElement(CreateNamedChild(nameArray != null ? nameArray[i] : "li", RecorderContext.CreateChild()), value.GetType().GetProperty(UtilMisc.DefaultTupleNames[i]).GetValue(value), args[i]);
            }
        }

        public override void WriteValueTuple(object value, System.Runtime.CompilerServices.TupleElementNamesAttribute names)
        {
            var args = value.GetType().GenericTypeArguments;
            var length = args.Length;

            var nameArray = names?.TransformNames;

            for (int i = 0; i < length; ++i)
            {
                Serialization.ComposeElement(CreateNamedChild(nameArray != null ? nameArray[i] : "li", RecorderContext.CreateChild()), value.GetType().GetField(UtilMisc.DefaultTupleNames[i]).GetValue(value), args[i]);
            }
        }

        public override void WriteRecord(IRecordable value)
        {
            if (depth < MaxRecursionDepth)
            {
                // This is somewhat faster than a full pending write (5-10% faster in one test case, though with a lot of noise), so we do it whenever we can.
                value.Record(new RecorderWriter(this));
            }
            else
            {
                // Reset depth because this will be run only when the pending writes are ready.
                depth = 0;
                writer.RegisterPendingWrite(() => WriteRecord(value));
            }
        }

        public override void WriteConvertible(Converter converter, object value)
        {
            // Convertibles are kind of a wildcard, so right now we're just changing this to Flexible mode
            MakeRecorderContextChild();

            if (depth < MaxRecursionDepth)
            {
                try
                {
                    if (converter is ConverterString converterString)
                    {
                        WriteString(converterString.WriteObj(value));
                    }
                    else if (converter is ConverterRecord converterRecord)
                    {
                        converterRecord.RecordObj(value, new RecorderWriter(this));
                    }
                    else if (converter is ConverterFactory converterFactory)
                    {
                        converterFactory.WriteObj(value, new RecorderWriter(this));
                    }
                    else
                    {
                        Dbg.Err($"Somehow ended up with an unsupported converter {converter.GetType()}");
                    }
                }
                catch (Exception e)
                {
                    Dbg.Ex(e);
                }
            }
            else
            {
                // Reset depth because this will be run only when the pending writes are ready.
                depth = 0;
                writer.RegisterPendingWrite(() => WriteConvertible(converter, value));
            }
        }

        internal XElement GetXElement()
        {
            return node;
        }
    }
}
