using System;
using System.Reflection;

namespace Dec.RecorderEnumerator
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RecordableEnumerableAttribute : Attribute { }

    public class UserCreatedEnumerableConverter : ConverterFactoryDynamic
    {
        Type enumerableType;

        public UserCreatedEnumerableConverter(Type type)
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
            // appears to be a sentinel value for "hasn't yet 'created' an 'instance'", which this currently hasn't
            // we'll overwrite this later though
            return Activator.CreateInstance(enumerableType, -2);
        }

        public override void Read(ref object input, Recorder recorder)
        {
            Write(input, recorder);
        }
    }
}
