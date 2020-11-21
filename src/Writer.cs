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
}
