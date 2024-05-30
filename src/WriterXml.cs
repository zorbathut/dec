namespace Dec
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading;
    using System.Xml;
    using System.Xml.Linq;

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

    internal class WriterXmlCompose : WriterXml
    {
        public override bool AllowReflection { get => true; }
        public override Recorder.IUserSettings UserSettings { get; }

        private XDocument doc;
        private XElement decs;

        public WriterXmlCompose(Recorder.IUserSettings userSettings)
        {
            this.UserSettings = userSettings;

            doc = new XDocument();

            decs = new XElement("Decs");
            doc.Add(decs);
        }

        public override bool RegisterReference(object referenced, XElement element, Recorder.Context recContext)
        {
            // We never register references in Compose mode.
            return false;
        }

        public WriterNode StartDec(Type type, string decName)
        {
            string typeName;
            Type overrideClass = null;
            if (type.IsGenericType)
            {
                // okay, this just got more complicated
                // we need to find a dec subclass that doesn't have any generics, then attach an attribute
                // this is . . . not great design, honestly, I'm not sure I like this format right now
                // but, fine, it's ugly but it will work
                Type baseType = type.BaseType;
                while (baseType.IsGenericType)
                {
                    baseType = baseType.BaseType;
                }

                typeName = baseType.ComposeDecFormatted();
                overrideClass = type;
            }
            else
            {
                typeName = type.ComposeDecFormatted();
            }

            var nodeXml = WriterNodeXml.StartDec(this, decs, typeName, decName);

            if (overrideClass != null)
            {
                nodeXml.TagClass(overrideClass);
            }

            return nodeXml;
        }

        public string Finish(bool pretty)
        {
            DequeuePendingWrites();

            return doc.ToString(pretty ? SaveOptions.None : SaveOptions.DisableFormatting);
        }
    }

    internal class WriterXmlRecord : WriterXml
    {
        public override bool AllowReflection { get => false; }
        public override Recorder.IUserSettings UserSettings { get; }

        // Maps between object and the in-place element. This does *not* yet have the ref ID tagged, and will have to be extracted into a new Element later.
        private Dictionary<object, XElement> refToElement = new Dictionary<object, XElement>();
        private Dictionary<XElement, object> elementToRef = new Dictionary<XElement, object>();

        // A map from object to the string intended as a reference. This will be filled in only once a second reference to something is created.
        // This is cleared after we resolve references, then re-used for the depth capping code.
        private Dictionary<object, string> refToString = new Dictionary<object, string>();

        // Current reference ID that we're on.
        private int referenceId = 0;

        private XDocument doc;
        private XElement record;
        private XElement refs;
        private XElement rootElement;

        public WriterXmlRecord(Recorder.IUserSettings userSettings)
        {
            this.UserSettings = userSettings;

            doc = new XDocument();

            record = new XElement("Record");
            doc.Add(record);

            record.Add(new XElement("recordFormatVersion", 1));

            refs = new XElement("refs");
            record.Add(refs);
        }

        public override bool RegisterReference(object referenced, XElement element, Recorder.Context recContext)
        {
            bool forceProcess = false;

            if (!refToElement.TryGetValue(referenced, out var xelement))
            {
                if (recContext.shared != Recorder.Context.Shared.Deny)
                {
                    // Insert it into our refToElement mapping
                    refToElement[referenced] = element;
                    elementToRef[element] = referenced;
                }
                else
                {
                    // Cannot be referenced, so we insert a fake null entry
                    refToElement[referenced] = null;

                    // Note: It is important not to add an elementToRef entry because this is later used to split long hierarchies
                    // and if you split a long hierarchy around a non-referencable barrier, everything breaks!
                }

                if (Config.TestRefEverything && recContext.shared != Recorder.Context.Shared.Deny)
                {
                    // Test pathway that should only occur during testing.
                    xelement = element;
                    forceProcess = true;
                }
                else
                {
                    return false;
                }
            }

            if (xelement == null)
            {
                // This is an unreferencable object! We are in trouble.
                Dbg.Err("Attempted to create a new reference to an unshared object. This may result in an invalid serialization. If this is coming from a Recorder setup, perhaps you need a .Shared() decorator.");
                return true;
            }

            // We have a referenceable target, but do *we* allow a reference?
            if (recContext.shared == Recorder.Context.Shared.Deny)
            {
                Dbg.Err("Attempted to create a new unshared reference to a previously-seen object. This may result in an invalid serialization. If this is coming from a Recorder setup, it's likely you either need a .Shared() decorator, or you need to ensure that this object is not serialized elsewhere.");
                return true;
            }

            var refId = refToString.TryGetValue(referenced);
            if (refId == null)
            {
                // We already had a reference, but we don't have a string ID for it. We need one now though!
                refId = $"ref{referenceId++:D5}";
                refToString[referenced] = refId;
            }

            // Tag the XML element properly
            element.SetAttributeValue("ref", refId);

            // And we're done!
            // If we're forcing auto-ref'ing, then we allow processing this; otherwise, we tell it to skip because it's already done.
            return !forceProcess;
        }

        public IEnumerable<KeyValuePair<string, XElement>> StripAndOutputReferences()
        {
            // It is *vitally* important that we do this step *after* all references are generated, not inline as we add references.
            // This is because we have to move all the contents of the XML element, but if we do it during generation, a recursive-reference situation could result in us trying to move the XML element before its contents are fully generated.
            // So we do it now, when we know that everything is finished.
            foreach (var refblock in refToString)
            {
                var result = new XElement("Ref");
                result.SetAttributeValue("id", refblock.Value);

                var src = refToElement[refblock.Key];

                // gotta ToArray() because it does not like mutating things while iterating
                // And yes, you have to .Remove() also, otherwise you get copies in both places.
                foreach (var attribute in src.Attributes().ToArray())
                {
                    attribute.Remove();

                    // We will normally not have a ref attribute here, but if we're doing the ref-everything mode, we might.
                    if (attribute.Name != "ref")
                    {
                        result.Add(attribute);
                    }
                }

                foreach (var node in src.Nodes().ToArray())
                {
                    node.Remove();
                    result.Add(node);
                }

                // Patch in the ref link
                src.SetAttributeValue("ref", refblock.Value);

                // We may not have had a class to begin with, but we sure need one now!
                result.SetAttributeValue("class", refblock.Key.GetType().ComposeDecFormatted());

                yield return new KeyValuePair<string, XElement>(refblock.Value, result);
            }

            // We're now done processing this segment and can erase it; we don't want to try doing this a second time!
            refToString.Clear();
        }

        public bool ProcessDepthLimitedReferences(XElement node, int depthRemaining)
        {
            if (depthRemaining <= 0 && elementToRef.ContainsKey(node))
            {
                refToString[elementToRef[node]] = $"ref{referenceId++:D5}";
                // We don't continue recursively because then we're threatening a stack overflow; we'll get it on the next pass

                return true;
            }
            else if (depthRemaining <= -100)
            {
                Dbg.Err("Depth limiter ran into an unshareable node stack that's too deep. Recommend using more `.Shared()` calls to allow for stack splitting. Generated file may not be readable (ask on Discord if you need this) and is likely to be very inefficient.");
                return false;
            }
            else
            {
                bool found = false;
                foreach (var child in node.Elements())
                {
                    found |= ProcessDepthLimitedReferences(child, depthRemaining - 1);
                }

                return found;
            }
        }

        public WriterNodeXml StartData(Type type)
        {
            var node = WriterNodeXml.StartData(this, record, "data", type);
            rootElement = node.GetXElement();
            return node;
        }

        public string Finish(bool pretty)
        {
            // Handle all our pending writes
            DequeuePendingWrites();

            // We now have a giant XML tree, potentially many thousands of nodes deep, where some nodes are references and some *should* be in the reference bank but aren't.
            // We need to do two things:
            // * Make all of our tagged references into actual references in the Refs section
            // * Tag anything deeper than a certain depth as a reference, then move it into the Refs section
            var depthTestsPending = new List<XElement>();
            depthTestsPending.Add(rootElement);

            // This is a loop between "write references" and "tag everything below a certain depth as needing to be turned into a reference".
            // We do this in a loop so we don't have to worry about ironically blowing our stack while making a change required to not blow our stack.
            while (true)
            {
                // Canonical ordering to provide some stability and ease-of-reading.
                foreach (var reference in StripAndOutputReferences().OrderBy(kvp => kvp.Key))
                {
                    refs.Add(reference.Value);
                    depthTestsPending.Add(reference.Value);
                }

                bool found = false;
                for (int i = 0; i < depthTestsPending.Count; ++i)
                {
                    // Magic number should probably be configurable at some point
                    found |= ProcessDepthLimitedReferences(depthTestsPending[i], 20);
                }
                depthTestsPending.Clear();

                if (!found)
                {
                    // No new depth-clobbering references found, just move on
                    break;
                }
            }

            if (refs.IsEmpty)
            {
                // strip out the refs 'cause it looks better that way :V
                refs.Remove();
            }

            if (!pretty)
            {
                doc.AddFirst(new XComment("Pretty-print can be enabled as a parameter of the Recorder.Write() call."));
            }
            doc.AddFirst(new XComment("This file was written by Dec, a serialization library designed for game development. (https://github.com/zorbathut/dec)"));

            return doc.ToString(pretty ? SaveOptions.None : SaveOptions.DisableFormatting);
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

        private WriterNodeXml(WriterXml writer, XElement parent, string label, int depth, Recorder.Context context) : base(context)
        {
            this.writer = writer;
            this.depth = depth;

            node = new XElement(label);
            parent.Add(node);
        }

        public static WriterNodeXml StartDec(WriterXmlCompose writer, XElement decRoot, string type, string decName)
        {
            var node = new WriterNodeXml(writer, decRoot, type, 0, new Recorder.Context());
            node.GetXElement().Add(new XAttribute("decName", decName));
            return node;
        }

        public static WriterNodeXml StartData(WriterXmlRecord writer, XElement decRoot, string name, Type type)
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
