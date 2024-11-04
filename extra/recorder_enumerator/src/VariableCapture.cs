using System;
using System.Reflection;

namespace Dec.RecorderEnumerator
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RecordableClosuresAttribute : Attribute { }

    public class RecordableClosureConverter : ConverterFactoryDynamic
    {
        Type enumerableType;

        public RecordableClosureConverter(Type type)
        {
            enumerableType = type;
        }

        public override void Write(object input, Recorder recorder)
        {
            foreach (var field in enumerableType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (global::Dec.Util.CanBeShared(field.FieldType))
                {
                    recorder.Shared().RecordPrivate(input, field, Util.SanitizeForXMLToken(field.Name));
                }
                else
                {
                    recorder.RecordPrivate(input, field, Util.SanitizeForXMLToken(field.Name));
                }
            }
        }

        public override object Create(Recorder recorder)
        {
            return Activator.CreateInstance(enumerableType);
        }

        public override void Read(ref object input, Recorder recorder)
        {
            Write(input, recorder);
        }
    }
}
