namespace Def
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    internal abstract class Writer
    {
        public abstract bool RecorderMode { get; }

        public abstract void RegisterPendingWrite(Action action);
        public abstract void DequeuePendingWrites();

        public abstract bool RegisterReference(object referenced, XElement element);
    }

    internal abstract class WriterNode
    {
        public abstract Writer Writer { get; }

        // this needs to be more abstract
        public abstract WriterNode CreateChild(string label);

        public abstract void WritePrimitive(object value);

        // get rid of me
        public abstract XElement GetXElement();
    }
}
