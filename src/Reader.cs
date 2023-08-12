namespace Dec
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Xml.Linq;

    internal abstract class ReaderFileDec
    {
        public struct ReaderDec
        {
            public Type type;
            public string name;

            public ReaderNode node;
            public InputContext inputContext;

            public bool abstrct;
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

            public ReaderNode node;
        }

        // this is a little awkward because it mutates state and should not be called twice. we should fix that
        public abstract List<ReaderRef> ParseRefs();
        public abstract ReaderNode ParseNode();
    }

    internal abstract class ReaderNode
    {
        public enum Metadata
        {
            Null,
            Ref,
            Class,
            Mode,
        }

        
        public abstract InputContext GetInputContext(); // note: this function must be really fast!
        public abstract string GetMetadata(Metadata metadata);
        public abstract string GetMetadataUnrecognized();
        public abstract string GetText();

        public abstract int GetChildCount();
        public abstract ReaderNode GetChildNamed(string name);

        public abstract void ParseList(IList list, Type referencedType, ReaderContext readerContext, Recorder.Context recorderContext);
        public abstract void ParseArray(Array array, Type referencedType, ReaderContext readerContext, Recorder.Context recorderContext, int startOffset);

        public abstract XElement HackyExtractXml();
    }

    internal static class ReaderNodeExtension
    {
        public static string ToLowerString(this ReaderNode.Metadata metadata)
        {
            // Hardcode lowercase versions of all the enum names for performance.
            switch (metadata)
            {
                case ReaderNode.Metadata.Null: return "null";
                case ReaderNode.Metadata.Ref: return "ref";
                case ReaderNode.Metadata.Class: return "class";
                case ReaderNode.Metadata.Mode: return "mode";
                default: Dbg.Err($"Unknown attribute type {metadata}"); return "UNKNOWN";
            }
        }

        public static ReaderNode.Metadata MetadataFromLowerString(string metadata)
        {
            // Convert lowercase version of the enum name back to the enum.
            switch (metadata)
            {
                case "null": return ReaderNode.Metadata.Null;
                case "ref": return ReaderNode.Metadata.Ref;
                case "class": return ReaderNode.Metadata.Class;
                case "mode": return ReaderNode.Metadata.Mode;
                default: Dbg.Err($"Unknown attribute name {metadata}"); return ReaderNode.Metadata.Null;
            }
        }
    }
}
