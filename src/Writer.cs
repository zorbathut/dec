using System;
using System.Collections;

namespace Dec
{
    internal abstract class WriterNode
    {
        private Recorder.Settings settings;
        private Path path;

        public WriterNode(Recorder.Settings settings, Path path)
        {
            this.settings = settings;
            this.path = path;
        }

        public Recorder.Settings RecorderSettings { get => settings; }
        public abstract bool AllowReflection { get; }
        public abstract bool AllowDecPath { get; }
        public virtual bool AllowAsThis { get => true; }
        public virtual bool AllowCloning { get => false; }
        public abstract Recorder.Purpose Intent { get; }
        public abstract Recorder.IUserSettings UserSettings { get; }

        public Path Path { get => path; }

        // I'm not real happy with the existence of this function; it's kind of a hack so that a shared Converter that writes a string or an int can avoid errors
        public void MakeRecorderContextChild()
        {
            settings = settings.CreateChild();
        }

        public abstract WriterNode CreateRecorderChild(string label, Recorder.Settings settings);
        public abstract WriterNode CreateReflectionChild(System.Reflection.FieldInfo field, Recorder.Settings settings);

        public abstract void WritePrimitive(object value);
        public abstract void WriteEnum(object value);
        public abstract void WriteString(string value);
        public abstract void WriteType(Type value);
        public abstract void WriteDec(Dec value);
        public abstract void WriteDecPathRef(object value);
        public abstract void WriteExplicitNull();
        public abstract bool WriteReference(object value, Path path);
        public abstract void WriteArray(Array value);
        public abstract void WriteList(IList value);
        public abstract void WriteDictionary(IDictionary value);
        public abstract void WriteHashSet(IEnumerable value);
        public abstract void WriteQueue(IEnumerable value);
        public abstract void WriteStack(IEnumerable value);
        public abstract void WriteTuple(object value, System.Runtime.CompilerServices.TupleElementNamesAttribute names);
        public abstract void WriteValueTuple(object value, System.Runtime.CompilerServices.TupleElementNamesAttribute names);
        public abstract void WriteRecord(IRecordable value);
        public abstract void WriteConvertible(Converter converter, object value);
        public virtual void WriteCloneCopy(object value) { Dbg.Err("Internal error, attempting to clone an object without being in clone mode"); }
        public virtual void WriteError() { }  // "this should be a thing, but it isn't, sorry"

        public abstract void TagClass(Type type);

        // general behavior that polymorphics should not reimplement (so far at least?)
        protected bool flaggedAsClass = false;
        protected bool flaggedAsThis = false;

        // attempts to flag as self, posts error if it can't
        public bool FlagAsThis()
        {
            flaggedAsThis = true;
            return true;
        }
        protected bool FlagAsClass()
        {
            if (flaggedAsThis)
            {
                Dbg.Err("Polymorphic Record() detected after a RecordAsThis(); this does not work, polymorphic Record() must be the first item in a This() chain");
                return false;
            }

            flaggedAsClass = true;
            return true;
        }

        protected bool FlagAsNull()
        {
            if (flaggedAsClass || flaggedAsThis)
            {
                Dbg.Err("Null tag detected after a class tag or a RecordAsThis(); this currently does not work, RecordAsThis() must not be used on a null value");
                return false;
            }

            return true;
        }
    }
}
