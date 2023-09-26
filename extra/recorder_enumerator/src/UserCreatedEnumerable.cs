namespace Dec.RecorderEnumerator
{
    using System;
    using System.Reflection;

    [AttributeUsage(AttributeTargets.Method)]
    public class RecordableAttribute : Attribute { }

    public class UserCreatedEnumerableConverter : ConverterFactoryDynamic
    {
        Type enumerableType;

        public UserCreatedEnumerableConverter(Type type)
        {
            enumerableType = type;
        }

        public override void Write(object input, Recorder recorder)
        {
            foreach (var field in enumerableType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                recorder.RecordPrivate(input, field, SanitizeForXMLToken(field.Name));
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
            foreach (var field in enumerableType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                recorder.RecordPrivate(input, field, SanitizeForXMLToken(field.Name));
            }
        }


        private static string SanitizeForXMLToken(string input)
        {
            return input.Replace("<", "LAB").Replace(">", "RAB");
        }
    }
}
