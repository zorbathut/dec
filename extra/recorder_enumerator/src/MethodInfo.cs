namespace Dec.RecorderEnumerator
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    public class MethodInfo_Converter : ConverterFactory<MethodInfo>
    {
        public override void Write(MethodInfo input, Recorder recorder)
        {
            Type type = input.DeclaringType;
            string name = input.Name;
            Type[] parameters = input.GetParameters().Select(p => p.ParameterType).ToArray();

            recorder.Record(ref type, "type");
            recorder.Record(ref name, "name");
            recorder.Record(ref parameters, "params");
        }

        public override MethodInfo Create(Recorder recorder)
        {
            Type type = null;
            string name = null;
            Type[] parameters = null;

            recorder.Record(ref type, "type");
            recorder.Record(ref name, "name");
            recorder.Record(ref parameters, "params");

            return type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, parameters, null);
        }

        public override void Read(ref MethodInfo input, Recorder recorder)
        {
            // nothing to do here
        }
    }
}
