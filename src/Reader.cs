namespace Dec
{
    using System;
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
        public abstract InputContext GetInputContext();
        public abstract string GetText();

        public abstract int GetProspectiveArrayLength();

        public abstract XElement HackyExtractXml();
    }
}
