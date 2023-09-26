namespace Dec.RecorderEnumerator
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;

    internal static class Util
    {
        public static void RecordPrivate(this Recorder recorder, object obj, FieldInfo field, string name)
        {
            object member = field.GetValue(obj);
            recorder.Record(ref member, name);
            field.SetValue(obj, member);
        }
    }
}
