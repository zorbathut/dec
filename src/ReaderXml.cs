using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Dec
{
    internal class ReaderNodeXml : ReaderNodeParseable
    {
        public override Recorder.IUserSettings UserSettings { get; }
        public override Recorder.Purpose Intent { get => Recorder.Purpose.Serialization; }

        private XElement xml;
        private string fileIdentifier;
        private Path path;

        // An element with more children than this indexes them by name after its first lookup, instead of scanning them for every field.
        internal const int ChildIndexThreshold = 16;

        // Each child name's first element, or null for a name that appears more than once.
        private Dictionary<string, XElement> childIndex;

        public ReaderNodeXml(XElement xml, string fileIdentifier, Path path, Recorder.IUserSettings userSettings)
        {
            this.UserSettings = userSettings;
            this.xml = xml;
            this.fileIdentifier = fileIdentifier;
            this.path = path;
        }

        public override Context GetContext()
        {
            return new Context(fileIdentifier, xml, path);
        }

        public override ReaderNode GetChildNamed(string name)
        {
            var child = ChildNamed(name);
            return child == null ? null : new ReaderNodeXml(child, fileIdentifier, new PathMember(path, name), UserSettings);
        }

        private XElement ChildNamed(string name)
        {
            if (childIndex == null)
            {
                var found = xml.ElementNamed(name, GetContext(), out int elementsScanned);
                if (elementsScanned > ChildIndexThreshold)
                {
                    childIndex = new Dictionary<string, XElement>(elementsScanned);
                    foreach (var element in xml.Elements())
                    {
                        if (!childIndex.TryAdd(element.Name.LocalName, element))
                        {
                            childIndex[element.Name.LocalName] = null;
                        }
                    }
                }

                return found;
            }

            if (!childIndex.TryGetValue(name, out var child))
            {
                return null;
            }

            // A repeated name goes back through ElementNamed, which reports it and hands back its first element.
            return child ?? xml.ElementNamed(name, GetContext(), out _);
        }

        public override string[] GetAllChildren()
        {
            return xml.Elements().Select(e => e.Name.LocalName).ToArray();
        }

        public override string GetText()
        {
            return xml.GetText();
        }

        public override bool HasText()
        {
            return xml.HasNonWhitespaceText();
        }

        public override string GetMetadata(Metadata metadata)
        {
            return xml.Attribute(metadata.ToLowerString())?.Value;
        }

        private static readonly HashSet<string> MetadataNames = UtilMisc.GetEnumValues<Metadata>().Select(metadata => metadata.ToLowerString()).ToHashSet();
        public override string GetMetadataUnrecognized()
        {
            if (!xml.HasAttributes)
            {
                return null;
            }

            var unrecognized = string.Join(", ", xml.Attributes().Select(attr => attr.Name.LocalName).Where(name => !MetadataNames.Contains(name)));
            return unrecognized == string.Empty ? null : unrecognized;
        }

        public override bool HasChildren()
        {
            return xml.HasElements;
        }

        public override int GetChildCount()
        {
            // Walked by hand rather than through Elements(), since this runs for every Record() object a read parses.
            int count = 0;
            for (var child = xml.FirstNode; child != null; child = child.NextNode)
            {
                if (child is XElement)
                {
                    ++count;
                }
            }

            return count;
        }

        public override int[] GetArrayDimensions(int rank)
        {
            // Special case: if this is a 1D array with text content and no children, it's likely a base64-encoded byte array
            string base64Text = rank == 1 && !xml.HasElements ? xml.GetText() : null;
            if (base64Text != null)
            {
                // Calculate byte array length from base64 string length without full parsing, not counting the whitespace decoding skips, so an element holding only whitespace is an empty array
                // Base64 length formula: (base64Length / 4) * 3, minus padding
                int base64Length = 0;
                foreach (char c in base64Text)
                {
                    if (!char.IsWhiteSpace(c))
                    {
                        ++base64Length;
                    }
                }

                int padding = 0;
                for (int i = base64Text.Length - 1; i >= 0 && padding < 2; --i)
                {
                    if (base64Text[i] == '=')
                    {
                        ++padding;
                    }
                    else if (!char.IsWhiteSpace(base64Text[i]))
                    {
                        break;
                    }
                }

                int estimatedByteLength = (base64Length / 4) * 3 - padding;
                return new int[] { estimatedByteLength };
            }

            // The actual processing will be handled by ParseArray, so we're not doing much validation here right now
            int[] results = new int[rank];
            var tier = xml;
            for (int i = 0; i < rank; ++i)
            {
                results[i] = tier.Elements().Count();

                tier = tier.Elements().FirstOrDefault();
                if (tier == null)
                {
                    // ran out of elements; stop now, we'll leave them full of 0's
                    break;
                }
            }

            return results;
        }

        public override void ParseList(IList list, Type referencedType, ReaderGlobals readerGlobals, Recorder.Settings recorderSettings)
        {
            var recorderChildContext = recorderSettings.CreateChild();

            foreach (var fieldElement in xml.Elements())
            {
                if (fieldElement.Name.LocalName != "li")
                {
                    var elementContext = new Context(fileIdentifier, fieldElement, path);
                    Dbg.Err($"{elementContext}: Tag should be <li>, is <{fieldElement.Name.LocalName}>");
                }

                list.Add(Serialization.ParseElement(new List<ReaderNodeParseable>() { new ReaderNodeXml(fieldElement, fileIdentifier, new PathIndex(path, list.Count), UserSettings) }, referencedType, null, readerGlobals, recorderChildContext));
            }

            var versionField = list.GetType().GetField("_version", BindingFlags.Instance | BindingFlags.NonPublic);
            if (versionField != null)
            {
                // possible that a collection won't have this, I suppose
                versionField.SetValue(list, Util.CollectionDeserializationVersionBoxed);
            }
        }

        private void ParseArrayRank(ReaderNodeXml node, ReaderGlobals readerGlobals, Recorder.Settings recorderSettings, Array value, Type referencedType, int rank, int[] indices, int startAt)
        {
            if (rank == indices.Length)
            {
                value.SetValue(Serialization.ParseElement(new List<ReaderNodeParseable>() { node }, referencedType, null, readerGlobals, recorderSettings), indices);
            }
            else
            {
                // this is kind of unnecessary but it's also an irrelevant perf hit
                var recorderChildContext = recorderSettings.CreateChild();

                int elementCount = node.xml.Elements().Count();
                int rankLength = value.GetLength(rank);
                if (elementCount > rankLength)
                {
                    Dbg.Err($"{node.GetContext()}: Array dimension {rank} expects {rankLength} elements but got {elementCount}; truncating");
                }
                else if (elementCount < rankLength)
                {
                    Dbg.Err($"{node.GetContext()}: Array dimension {rank} expects {rankLength} elements but got {elementCount}; padding with default values");
                }

                int i = 0;
                foreach (var fieldElement in node.xml.Elements())
                {
                    if (i >= rankLength)
                    {
                        // truncate, we're done here
                        break;
                    }

                    indices[rank] = startAt + i++;
                    var newPath = new PathIndexMultidim(path, indices.ToArray());

                    if (fieldElement.Name.LocalName != "li")
                    {
                        var elementContext = new Context(fileIdentifier, fieldElement, newPath);
                        Dbg.Err($"{elementContext}: Tag should be <li>, is <{fieldElement.Name.LocalName}>");
                    }

                    // the pathIndexMultidim is kind of slow and I should solve this at some point
                    ParseArrayRank(new ReaderNodeXml(fieldElement, fileIdentifier, newPath, UserSettings), readerGlobals, recorderChildContext, value, referencedType, rank + 1, indices, 0);
                }
            }
        }

        public override void ParseArray(Array array, Type referencedType, ReaderGlobals readerGlobals, Recorder.Settings recorderSettings, int startOffset)
        {
            var recorderChildContext = recorderSettings.CreateChild();

            if (array.Rank == 1)
            {
                // fast path
                int index = 0;
                foreach (var fieldElement in xml.Elements())
                {
                    var newPath = new PathIndex(path, startOffset + index);

                    if (fieldElement.Name.LocalName != "li")
                    {
                        var elementContext = new Context(fileIdentifier, fieldElement, newPath);
                        Dbg.Err($"{elementContext}: Tag should be <li>, is <{fieldElement.Name.LocalName}>");
                    }

                    array.SetValue(Serialization.ParseElement(new List<ReaderNodeParseable>() { new ReaderNodeXml(fieldElement, fileIdentifier, newPath, UserSettings) }, referencedType, null, readerGlobals, recorderChildContext), startOffset + index);
                    ++index;
                }
            }
            else
            {
                // slow path
                var indices = new int[array.Rank];
                ParseArrayRank(this, readerGlobals, recorderChildContext, array, referencedType, 0, indices, startOffset);
            }
        }

        public override IEnumerable<ReaderDictionaryEntry> GetDictionaryEntries(Type referencedKeyType, Type referencedValueType, ReaderGlobals readerGlobals, Recorder.Settings recorderSettings)
        {
            var recorderChildContext = recorderSettings.CreateChild();

            foreach (var fieldElement in xml.Elements())
            {
                var elementContext = new Context(fileIdentifier, fieldElement);

                if (fieldElement.Name.LocalName == "li")
                {
                    // Treat this like a key/value pair
                    var keyNode = fieldElement.ElementNamedWithFallback("key", elementContext, "Dictionary includes li tag without a `key`");
                    var valueNode = fieldElement.ElementNamedWithFallback("value", elementContext, "Dictionary includes li tag without a `value`");

                    if (keyNode == null)
                    {
                        // error has already been generated
                        continue;
                    }

                    if (valueNode == null)
                    {
                        // error has already been generated
                        continue;
                    }

                    var keyPath = new PathDictionaryKey(path);
                    var key = Serialization.ParseElement(new List<ReaderNodeParseable>() { new ReaderNodeXml(keyNode, fileIdentifier, keyPath, UserSettings) }, referencedKeyType, null, readerGlobals, recorderChildContext);

                    if (key == null)
                    {
                        Dbg.Err($"{new Context(fileIdentifier, keyNode, keyPath)}: Dictionary includes null key, skipping pair");
                        continue;
                    }

                    var valuePath = new PathDictionaryValueUnpathable(path);
                    yield return new ReaderDictionaryEntry { key = key, value = new ReaderNodeXml(valueNode, fileIdentifier, valuePath, UserSettings), valueType = referencedValueType, context = elementContext };
                }
                else
                {
                    var key = Serialization.ParseString(fieldElement.Name.LocalName, referencedKeyType, null, elementContext);

                    if (key == null)
                    {
                        // it's really rare for this to happen, I think you could do it with a converter but that's it
                        Dbg.Err($"{elementContext}: Dictionary includes null key, skipping pair");

                        // just in case . . .
                        if (string.Compare(fieldElement.Name.LocalName, "li", true) == 0)
                        {
                            Dbg.Err($"{elementContext}: Did you mean to write `li`? This field is case-sensitive.");
                        }

                        continue;
                    }

                    Type valueType = referencedValueType;
                    if (recorderSettings.bespoke_keytypedict)
                    {
                        if (referencedKeyType != typeof(Type))
                        {
                            Dbg.Err($"{elementContext}: Bespoke_KeyTypeDict() requires a dictionary with a Type key, but this dictionary's key type is {referencedKeyType}");
                        }

                        // make sure the value of `key` can be implicitly converted to `referencedValueType`
                        if (!referencedValueType.IsAssignableFrom((Type)key))
                        {
                            Dbg.Err($"{elementContext}: Key `{key}` cannot be implicitly converted to {referencedValueType}");
                        }

                        valueType = (Type)key;
                    }

                    Path valuePath;
                    if (fieldElement.Name.LocalName.Contains('[') || fieldElement.Name.LocalName.Contains(']'))
                    {
                        // I'm not actually sure this is possible
                        valuePath = new PathDictionaryValueUnpathable(path);
                    }
                    else
                    {
                        valuePath = new PathDictionaryValue(path, fieldElement.Name.LocalName);
                    }

                    yield return new ReaderDictionaryEntry { key = key, value = new ReaderNodeXml(fieldElement, fileIdentifier, valuePath, UserSettings), valueType = valueType, context = elementContext };
                }
            }
        }

        public override void ParseHashset(object hashset, Type referencedType, ReaderGlobals readerGlobals, Recorder.Settings recorderSettings, bool permitPatch)
        {
            // ISet<> doesn't inherit from a non-generic interface exposing Contains/Add, so we route through per-element-type cached delegates.
            var containsFunction = UtilCollectionReflect.SetContains(referencedType);
            var addFunction = UtilCollectionReflect.SetAdd(referencedType);

            var recorderChildContext = recorderSettings.CreateChild();

            // avoid the heap allocation if we can
            var writtenFields = permitPatch ? new HashSet<object>() : null;

            foreach (var fieldElement in xml.Elements())
            {
                var keyPath = new PathHashSetElement(path);
                var elementContext = new Context(fileIdentifier, fieldElement, keyPath);

                // There's a potential bit of ambiguity here if someone does <li /> and expects that to be an actual string named "li".
                // Practically, I think this is less likely than someone doing <li></li> and expecting that to be the empty string.
                // And there's no other way to express the empty string.
                // So . . . we treat that like the empty string.
                if (fieldElement.Name.LocalName == "li")
                {
                    // Treat this like a full node
                    var key = Serialization.ParseElement(new List<ReaderNodeParseable>() { new ReaderNodeXml(fieldElement, fileIdentifier, keyPath, UserSettings) }, referencedType, null, readerGlobals, recorderChildContext);

                    if (key == null)
                    {
                        Dbg.Err($"{elementContext}: HashSet includes null key, skipping");
                        continue;
                    }

                    if (containsFunction(hashset, key) && (writtenFields == null || writtenFields.Contains(key)))
                    {
                        Dbg.Err($"{elementContext}: HashSet includes duplicate key `{key.ToString()}`");
                    }
                    writtenFields?.Add(key);

                    addFunction(hashset, key);
                }
                else
                {
                    if (fieldElement.HasElements || !fieldElement.GetText().IsNullOrEmpty())
                    {
                        Dbg.Err($"{elementContext}: HashSet non-li member includes data, ignoring");
                    }

                    var key = Serialization.ParseString(fieldElement.Name.LocalName, referencedType, null, elementContext);

                    if (key == null)
                    {
                        // it's really rare for this to happen, I think you could do it with a converter but that's it
                        Dbg.Err($"{elementContext}: HashSet includes null key, skipping pair");
                        continue;
                    }

                    if (containsFunction(hashset, key) && (writtenFields == null || writtenFields.Contains(key)))
                    {
                        Dbg.Err($"{elementContext}: HashSet includes duplicate key `{key.ToString()}`");
                    }
                    writtenFields?.Add(key);

                    addFunction(hashset, key);
                }
            }
        }

        public override void ParseStack(object stack, Type referencedType, ReaderGlobals readerGlobals, Recorder.Settings recorderSettings)
        {
            var pushFunction = UtilCollectionReflect.StackPush(referencedType);

            var recorderChildContext = recorderSettings.CreateChild();

            int index = UtilCollectionReflect.StackCount(referencedType)(stack);
            foreach (var fieldElement in xml.Elements())
            {
                var newPath = new PathStackElement(path, index);

                if (fieldElement.Name.LocalName != "li")
                {
                    var elementContext = new Context(fileIdentifier, fieldElement, newPath);
                    Dbg.Err($"{elementContext}: Tag should be <li>, is <{fieldElement.Name.LocalName}>");
                }

                pushFunction(stack, Serialization.ParseElement(new List<ReaderNodeParseable>() { new ReaderNodeXml(fieldElement, fileIdentifier, newPath, UserSettings) }, referencedType, null, readerGlobals, recorderChildContext));
                ++index;
            }
        }

        public override void ParseQueue(object queue, Type referencedType, ReaderGlobals readerGlobals, Recorder.Settings recorderSettings)
        {
            var enqueueFunction = UtilCollectionReflect.QueueEnqueue(referencedType);

            var recorderChildContext = recorderSettings.CreateChild();

            int index = UtilCollectionReflect.QueueCount(referencedType)(queue);
            foreach (var fieldElement in xml.Elements())
            {
                var newPath = new PathQueueElement(path, index);

                if (fieldElement.Name.LocalName != "li")
                {
                    var elementContext = new Context(fileIdentifier, fieldElement, newPath);
                    Dbg.Err($"{elementContext}: Tag should be <li>, is <{fieldElement.Name.LocalName}>");
                }

                enqueueFunction(queue, Serialization.ParseElement(new List<ReaderNodeParseable>() { new ReaderNodeXml(fieldElement, fileIdentifier, newPath, UserSettings) }, referencedType, null, readerGlobals, recorderChildContext));
                ++index;
            }
        }

        public override void ParseTuple(object[] parameters, Type referencedType, IList<string> parameterNames, ReaderGlobals readerGlobals, Recorder.Settings recorderSettings)
        {
            int expectedCount = referencedType.GenericTypeArguments.Length;
            var recorderChildContext = recorderSettings.CreateChild();

            var elements = xml.Elements().ToList();

            bool hasNonLi = false;
            foreach (var elementField in elements)
            {
                if (elementField.Name.LocalName != "li")
                {
                    hasNonLi = true;
                }
            }

            if (!hasNonLi)
            {
                // Treat it like an indexed array

                if (elements.Count != parameters.Length)
                {
                    Dbg.Err($"{GetContext()}: Tuple expects {expectedCount} parameters but got {elements.Count}");
                }

                for (int i = 0; i < Math.Min(parameters.Length, elements.Count); ++i)
                {
                    parameters[i] = Serialization.ParseElement(new List<ReaderNodeParseable>() { new ReaderNodeXml(elements[i], fileIdentifier, new PathTupleItem(path, i), UserSettings) }, referencedType.GenericTypeArguments[i], null, readerGlobals, recorderChildContext);
                }

                // fill in anything missing
                for (int i = Math.Min(parameters.Length, elements.Count); i < parameters.Length; ++i)
                {
                    parameters[i] = Serialization.GenerateResultFallback(null, referencedType.GenericTypeArguments[i]);
                }
            }
            else
            {
                // We're doing named lookups instead
                if (parameterNames == null)
                {
                    parameterNames = UtilMisc.DefaultTupleNames;
                }

                if (parameterNames.Count < expectedCount)
                {
                    Dbg.Err($"{GetContext()}: Not enough tuple names (this honestly shouldn't even be possible)");

                    // TODO: handle it
                }

                bool[] seen = new bool[expectedCount];
                foreach (var elementItem in elements)
                {
                    var newPath = new PathMember(path, elementItem.Name.LocalName); // yeah okay
                    var elementContext = new Context(fileIdentifier, elementItem, newPath);

                    int index = parameterNames.FirstIndexOf(n => n == elementItem.Name.LocalName);

                    if (index == -1)
                    {
                        Dbg.Err($"{elementContext}: Found field with unexpected name `{elementItem.Name.LocalName}`");
                        continue;
                    }

                    if (seen[index])
                    {
                        Dbg.Err($"{elementContext}: Found duplicate of field `{elementItem.Name.LocalName}`");
                    }

                    seen[index] = true;
                    parameters[index] = Serialization.ParseElement(new List<ReaderNodeParseable>() { new ReaderNodeXml(elementItem, fileIdentifier, newPath, UserSettings) }, referencedType.GenericTypeArguments[index], null, readerGlobals, recorderChildContext);
                }

                for (int i = 0; i < seen.Length; ++i)
                {
                    if (!seen[i])
                    {
                        Dbg.Err($"{GetContext()}: Missing field with name `{parameterNames[i]}`");

                        // Patch it up as best we can
                        parameters[i] = Serialization.GenerateResultFallback(null, referencedType.GenericTypeArguments[i]);
                    }
                }
            }
        }

        public override IEnumerable<(string name, ReaderNodeParseable node)> GetFieldNodes()
        {
            foreach (var fieldElement in xml.Elements())
            {
                string fieldName = fieldElement.Name.LocalName;
                yield return (fieldName, new ReaderNodeXml(fieldElement, fileIdentifier, new PathMember(path, fieldName), UserSettings));
            }
        }
    }
}
