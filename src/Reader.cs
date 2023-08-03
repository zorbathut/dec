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

        public abstract List<ReaderDec> ParseDecs();
    }

    internal abstract class ReaderFileRecorder
    {
    }

    internal abstract class ReaderNode
    {
        public abstract XElement HackyExtractXml();
    }
}
