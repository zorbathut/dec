using System;
using System.Collections;
using System.Collections.Generic;

namespace Dec
{
    internal abstract class ReaderFileDec
    {
        public struct ReaderDec
        {
            public Type type;
            public string name;

            // this is a factory because we need to be able to generate lots of it with different paths
            // this is not a good solution
            // this needs to go away along with the whole polymorphic ReaderNode system
            public Func<Path, ReaderNodeParseable> nodeFactory;
            public Context context;

            public bool? abstrct;
            public string parent;
        }

        // this is a little awkward because it mutates state and should not be called twice. we should fix that
        public abstract List<ReaderDec> ParseDecs();
    }

    internal abstract class ReaderFileRecorder
    {
        public struct ReaderRef
        {
            public Type type;
            public string id;

            public ReaderNodeParseable node;
        }

        // this is a little awkward because it mutates state and should not be called twice. we should fix that
        public abstract List<ReaderRef> ParseRefs();
        public abstract ReaderNodeParseable ParseNode();
    }

    internal abstract class ReaderNode
    {
        public virtual bool AllowAsThis { get => true; }
        public abstract Recorder.Purpose Intent { get; }
        public abstract Recorder.IUserSettings UserSettings { get; }

        public abstract Context GetContext(); // note: this function must be really fast!

        public abstract ReaderNode GetChildNamed(string name);
        public abstract string[] GetAllChildren();
        public abstract bool HasText();

        public abstract int[] GetArrayDimensions(int rank);

        public abstract object ParseElement(Type type, object model, ReaderGlobals readerGlobals, Recorder.Settings recorderSettings);
    }

    // Several layers of XML for one object, read together by a Recorder. Each member it asks for gets every layer's version of itself at once, so the member's own mode handling decides which layers survive.
    internal class ReaderNodeLayers : ReaderNode
    {
        public List<ReaderNodeParseable> Layers { get; }

        public ReaderNodeLayers(List<ReaderNodeParseable> layers)
        {
            Layers = layers;
        }

        public override Recorder.Purpose Intent { get { return Layers[0].Intent; } }
        public override Recorder.IUserSettings UserSettings { get { return Layers[0].UserSettings; } }

        // The last layer is the dec actually being built, so it's the most useful place to point.
        public override Context GetContext()
        {
            return Layers[Layers.Count - 1].GetContext();
        }

        public override ReaderNode GetChildNamed(string name)
        {
            ReaderNodeParseable single = null;
            List<ReaderNodeParseable> several = null;
            foreach (var layer in Layers)
            {
                var child = (ReaderNodeParseable)layer.GetChildNamed(name);
                if (child == null)
                {
                    continue;
                }

                if (single == null)
                {
                    single = child;
                }
                else
                {
                    several = several ?? new List<ReaderNodeParseable> { single };
                    several.Add(child);
                }
            }

            if (several != null)
            {
                return new ReaderNodeLayers(several);
            }

            return single;
        }

        // Nothing asks these of a whole stack of layers; unused-field reporting goes layer by layer.
        public override string[] GetAllChildren()
        {
            throw new NotImplementedException();
        }

        public override bool HasText()
        {
            throw new NotImplementedException();
        }

        public override int[] GetArrayDimensions(int rank)
        {
            throw new NotImplementedException();
        }

        public override object ParseElement(Type type, object model, ReaderGlobals readerGlobals, Recorder.Settings recorderSettings)
        {
            return Serialization.ParseElement(Layers, type, model, readerGlobals, recorderSettings);
        }
    }

    // One key/value pair of a dictionary's XML, with its key already parsed and its value not yet.
    internal struct ReaderDictionaryEntry
    {
        public object key;
        public ReaderNodeParseable value;
        public Type valueType;
        public Context context;
    }

    internal abstract class ReaderNodeParseable : ReaderNode
    {
        public enum Metadata
        {
            Null,
            Ref,
            Class,
            Mode,
            Error,
        }

        public override object ParseElement(Type type, object model, ReaderGlobals readerGlobals, Recorder.Settings recorderSettings)
        {
            return Serialization.ParseElement(new List<ReaderNodeParseable>() { this }, type, model, readerGlobals, recorderSettings);
        }

        public abstract string GetMetadata(Metadata metadata);
        public abstract string GetMetadataUnrecognized();
        public abstract string GetText();

        public override bool HasText()
        {
            return !string.IsNullOrEmpty(GetText());
        }

        public abstract bool HasChildren();

        public abstract void ParseList(IList list, Type referencedType, ReaderGlobals readerGlobals, Recorder.Settings recorderSettings);
        public abstract void ParseArray(Array array, Type referencedType, ReaderGlobals readerGlobals, Recorder.Settings recorderSettings, int startOffset);
        public abstract IEnumerable<ReaderDictionaryEntry> GetDictionaryEntries(Type referencedKeyType, Type referencedValueType, ReaderGlobals readerGlobals, Recorder.Settings recorderSettings);
        public abstract void ParseHashset(object hashset, Type referencedType, ReaderGlobals readerGlobals, Recorder.Settings recorderSettings, bool permitPatch);
        public abstract void ParseStack(object hashset, Type referencedType, ReaderGlobals readerGlobals, Recorder.Settings recorderSettings);
        public abstract void ParseQueue(object hashset, Type referencedType, ReaderGlobals readerGlobals, Recorder.Settings recorderSettings);
        public abstract void ParseTuple(object[] parameters, Type referencedType, IList<string> parameterNames, ReaderGlobals readerGlobals, Recorder.Settings recorderSettings);
        public abstract IEnumerable<(string name, ReaderNodeParseable node)> GetFieldNodes();
    }

    internal static class ReaderNodeExtension
    {
        public static string ToLowerString(this ReaderNodeParseable.Metadata metadata)
        {
            // Hardcode lowercase versions of all the enum names for performance.
            switch (metadata)
            {
                case ReaderNodeParseable.Metadata.Null: return "null";
                case ReaderNodeParseable.Metadata.Ref: return "ref";
                case ReaderNodeParseable.Metadata.Class: return "class";
                case ReaderNodeParseable.Metadata.Mode: return "mode";
                case ReaderNodeParseable.Metadata.Error: return "error";
                default: Dbg.Err($"Unknown attribute type {metadata}"); return "UNKNOWN";
            }
        }
    }
}
