using System;

namespace Dec.RecorderEnumerator
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RecordableClosuresAttribute : Attribute { }

    public class RecordableClosureConverter : ConverterFactoryFields
    {
        readonly Type enumerableType;

        public RecordableClosureConverter(Type type) : base(type, Util.RecordFieldsAll(type))
        {
            enumerableType = type;
        }

        public override object Create(Recorder recorder)
        {
            return Activator.CreateInstance(enumerableType);
        }
    }
}
