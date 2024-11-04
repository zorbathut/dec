using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Dec
{
    internal abstract class ReaderFileDec
    {
        public struct ReaderDec
        {
            public Type type;
            public string name;

            public ReaderNodeParseable node;
            public InputContext inputContext;

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
        public abstract Recorder.IUserSettings UserSettings { get; }

        public abstract InputContext GetInputContext(); // note: this function must be really fast!

        public abstract ReaderNode GetChildNamed(string name);
        public abstract string[] GetAllChildren();

        public abstract int[] GetArrayDimensions(int rank);

        public abstract object ParseElement(Type type, object model, ReaderContext readerContext, Recorder.Context recorderContext);
    }

    internal abstract class ReaderNodeParseable : ReaderNode
    {
        public enum Metadata
        {
            Null,
            Ref,
            Class,
            Mode,
        }

        public override object ParseElement(Type type, object model, ReaderContext readerContext, Recorder.Context recorderContext)
        {
            return Serialization.ParseElement(new List<ReaderNodeParseable>() { this }, type, model, readerContext, recorderContext);
        }

        public abstract string GetMetadata(Metadata metadata);
        public abstract string GetMetadataUnrecognized();
        public abstract string GetText();

        public abstract bool HasChildren();

        public abstract void ParseList(IList list, Type referencedType, ReaderContext readerContext, Recorder.Context recorderContext);
        public abstract void ParseArray(Array array, Type referencedType, ReaderContext readerContext, Recorder.Context recorderContext, int startOffset);
        public abstract void ParseDictionary(IDictionary dict, Type referencedKeyType, Type referencedValueType, ReaderContext readerContext, Recorder.Context recorderContext, bool permitPatch);
        public abstract void ParseHashset(object hashset, Type referencedType, ReaderContext readerContext, Recorder.Context recorderContext, bool permitPatch);
        public abstract void ParseStack(object hashset, Type referencedType, ReaderContext readerContext, Recorder.Context recorderContext);
        public abstract void ParseQueue(object hashset, Type referencedType, ReaderContext readerContext, Recorder.Context recorderContext);
        public abstract void ParseTuple(object[] parameters, Type referencedType, IList<string> parameterNames, ReaderContext readerContext, Recorder.Context recorderContext);
        public abstract void ParseReflection(object obj, ReaderContext readerContext, Recorder.Context recorderContext);
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
                default: Dbg.Err($"Unknown attribute type {metadata}"); return "UNKNOWN";
            }
        }
    }
}
