using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        public abstract bool AllowDecPath { get; }
        public abstract Recorder.IUserSettings UserSettings { get; }

        public abstract bool RegisterReference(object referenced, XElement element, Recorder.Settings recSettings, Path path);

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
        public override bool AllowDecPath { get => writer.AllowDecPath; }
        public override Recorder.Purpose Intent { get => Recorder.Purpose.Serialization; }
        public override Recorder.IUserSettings UserSettings { get => writer.UserSettings; }

        private WriterNodeXml(WriterXml writer, XContainer parent, string label, int depth, Recorder.Settings settings, Path path) : base(settings, path)
        {
            this.writer = writer;
            this.depth = depth;

            node = new XElement(label);
            parent.Add(node);
        }

        public static WriterNodeXml StartDec(WriterXmlCompose writer, XContainer decRoot, string type, string decName)
        {
            var node = new WriterNodeXml(writer, decRoot, type, 0, new Recorder.Settings(), new PathDec(type, decName));
            node.GetXElement().Add(new XAttribute("decName", decName));
            return node;
        }

        public static WriterNodeXml StartRecord(WriterXml writer, XContainer decRoot, string name, Type type, string pathId)
        {
            return new WriterNodeXml(writer, decRoot, name, 0, new Recorder.Settings() { shared = Recorder.Settings.Shared.Flexible }, new PathRoot(pathId));
        }

        internal WriterNodeXml CreateNamedChild(string label, Recorder.Settings settings, Path newPath)
        {
            return new WriterNodeXml(writer, node, label, depth + 1, settings, newPath);
        }

        // this should be WriterNodeXml but this C# doesn't support that
        public override WriterNode CreateRecorderChild(string label, Recorder.Settings settings)
        {
            return new WriterNodeXml(writer, node, label, depth + 1, settings, new PathMember(Path, label));
        }

        // this should be WriterNodeXml but this C# doesn't support that
        public override WriterNode CreateReflectionChild(System.Reflection.FieldInfo field, Recorder.Settings settings)
        {
            return new WriterNodeXml(writer, node, field.Name, depth + 1, settings, new PathMember(Path, field.Name));
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
                else
                {
                    // .NET Core 3.0+ default ToString() is shortest-round-trippable, which is what we need.
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

        public override void WriteDecPathRef(object value)
        {
            node.Add(new XAttribute("ref", Database.GetDecPathFromObj(value)));
        }

        public override void TagClass(Type type)
        {
            // I guess we just keep going? what's likely to be less damaging here? this may at least be manually reconstructible I suppose?
            FlagAsClass();

            node.Add(new XAttribute("class", type.ComposeDecFormatted()));
        }

        public override void WriteExplicitNull()
        {
            if (!FlagAsNull())
            {
                // this can result in some weirdly broken XML so let's just not do that
                return;
            }

            node.SetAttributeValue("null", "true");
        }

        public override bool WriteReference(object value, Path path)
        {
            return writer.RegisterReference(value, node, RecorderSettings, path);
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
                    indices[rank] = i;
                    var child = node.CreateNamedChild("li", RecorderSettings.CreateChild(), new PathIndexMultidim(Path, indices.ToArray()));

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
                    Serialization.ComposeElement(CreateNamedChild("li", RecorderSettings.CreateChild(), new PathIndex(Path, i)), value.GetValue(i), referencedType);
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

        public override void WriteByteArray(byte[] value)
        {
            // about as good as we're gonna get
            WriteString(Convert.ToBase64String(value));
        }

        public override void WriteList(IList value)
        {
            Type referencedType = value.GetType().GetGenericInterfaceArguments(typeof(IList<>))[0];

            for (int i = 0; i < value.Count; ++i)
            {
                Serialization.ComposeElement(CreateNamedChild("li", RecorderSettings.CreateChild(), new PathIndex(Path, i)), value[i], referencedType);
            }
        }

        public override void WriteDictionary(IDictionary value)
        {
            var dictArgs = value.GetType().GetGenericInterfaceArguments(typeof(IDictionary<,>));
            Type keyType = dictArgs[0];
            Type valueType = dictArgs[1];

            // I really want some way to canonicalize this ordering
            IDictionaryEnumerator iterator = value.GetEnumerator();
            while (iterator.MoveNext())
            {
                // In theory, some dicts support inline format, not li format. Inline format is cleaner and smaller and we should be using it when possible.
                // In practice, it's hard and I'm lazy and this always works, and we're not providing any guarantees about cleanliness of serialized output.
                // Revisit this later when someone (possibly myself) really wants it improved.
                var li = CreateNamedChild("li", RecorderSettings, Path);

                Serialization.ComposeElement(li.CreateNamedChild("key", RecorderSettings.CreateChildKey(iterator.Key), new PathDictionaryKey(Path)), iterator.Key, keyType);

                // unfortunate consequence: we can't generate sensible Paths when doing this
                Serialization.ComposeElement(li.CreateNamedChild("value", RecorderSettings.CreateChild(), new PathDictionaryValueUnpathable(Path)), iterator.Value, valueType);
            }
        }

        public override void WriteHashSet(IEnumerable value)
        {
            Type keyType = value.GetType().GetGenericInterfaceArguments(typeof(ISet<>))[0];

            // I really want some way to canonicalize this ordering
            IEnumerator iterator = value.GetEnumerator();
            while (iterator.MoveNext())
            {
                // In theory, some sets support inline format, not li format. Inline format is cleaner and smaller and we should be using it when possible.
                // In practice, it's hard and I'm lazy and this always works, and we're not providing any guarantees about cleanliness of serialized output.
                // Revisit this later when someone (possibly myself) really wants it improved.
                Serialization.ComposeElement(CreateNamedChild("li", RecorderSettings.CreateChildKey(iterator.Current), new PathHashSetElement(Path)), iterator.Current, keyType);
            }
        }

        public override void WriteQueue(IEnumerable value)
        {
            // We actually just treat this like an array right now; it's the same behavior and it's easier
            Type keyType = value.GetType().GetGenericArguments()[0];
            var array = UtilCollectionReflect.QueueToArray(keyType)(value);

            for (int i = 0; i < array.Length; ++i)
            {
                Serialization.ComposeElement(CreateNamedChild("li", RecorderSettings.CreateChild(), new PathQueueElement(Path, i)), array.GetValue(i), keyType);
            }
        }

        public override void WriteStack(IEnumerable value)
        {
            // We actually just treat this like an array right now; it's the same behavior and it's easier
            Type keyType = value.GetType().GetGenericArguments()[0];
            var array = UtilCollectionReflect.StackToArray(keyType)(value);

            // For some reason this writes it out to an array in the reverse order than I'd expect
            // (and also the reverse order it inputs in!)
            // so, uh, time to munge
            Array.Reverse(array);

            for (int i = 0; i < array.Length; ++i)
            {
                Serialization.ComposeElement(CreateNamedChild("li", RecorderSettings.CreateChild(), new PathStackElement(Path, i)), array.GetValue(i), keyType);
            }
        }

        public override void WriteTuple(object value, System.Runtime.CompilerServices.TupleElementNamesAttribute names)
        {
            var args = value.GetType().GenericTypeArguments;
            var length = args.Length;

            var nameArray = names?.TransformNames;

            for (int i = 0; i < length; ++i)
            {
                Serialization.ComposeElement(CreateNamedChild(nameArray != null ? nameArray[i] : "li", RecorderSettings.CreateChild(), new PathTupleItem(Path, i)), value.GetType().GetProperty(UtilMisc.DefaultTupleNames[i]).GetValue(value), args[i]);
            }
        }

        public override void WriteValueTuple(object value, System.Runtime.CompilerServices.TupleElementNamesAttribute names)
        {
            var args = value.GetType().GenericTypeArguments;
            var length = args.Length;

            var nameArray = names?.TransformNames;

            for (int i = 0; i < length; ++i)
            {
                Serialization.ComposeElement(CreateNamedChild(nameArray != null ? nameArray[i] : "li", RecorderSettings.CreateChild(), new PathTupleItem(Path, i)), value.GetType().GetField(UtilMisc.DefaultTupleNames[i]).GetValue(value), args[i]);
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
