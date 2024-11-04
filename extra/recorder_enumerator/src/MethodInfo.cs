using System;
using System.Linq;
using System.Reflection;

namespace Dec.RecorderEnumerator
{
    public class MethodInfo_Converter : ConverterFactory<MethodInfo>
    {
        public override bool TreatAsValuelike()
        {
            return true;
        }

        public override void Write(MethodInfo input, Recorder recorder)
        {
            Type type = input.DeclaringType;
            string name = input.Name;

            // FUN FACT:
            // in C#, if you call .ToArray() on a zero-length enumerable, you get a zero-length array.
            // You get, in fact, a specific zero-length array object. Same object is returned every time you do this, even if it's a completely unrelated zero-length enumerable.
            Type[] parametersOrig = input.GetParameters().Select(p => p.ParameterType).ToArray();

            // Usually this is not a problem but it means we end up trying to store multiple references to the same zero-length array.
            // And we need to read that reference in Create(), but you can't read shared references in Create() because of format limitations.
            // So we're cloning the array just to guarantee we have a unique copy of it.
            // This is inefficient; really, we should have a `DuplicateIfShared` attribute that just forces no-sharing and also no-error and records multiple copies of it.
            // But that's a lot of work and I haven't done that yet.
            // Practically speaking I don't think this is likely to be the bottleneck ever, so, uh, yeah! Woo! This!
            Type[] parameters = new Type[parametersOrig.Length];
            Array.Copy(parametersOrig, parameters, parameters.Length);

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
