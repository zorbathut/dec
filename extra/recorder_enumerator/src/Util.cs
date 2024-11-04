using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Dec.RecorderEnumerator
{
    internal static class Util
    {
        internal static void RecordPrivate(this Recorder recorder, object obj, FieldInfo field, string name)
        {
            RecordBasePrivateInternalMethod.MakeGenericMethod(field.FieldType).Invoke(null, new object[] { recorder, obj, field, name });
        }

        internal static void RecordPrivate(this Recorder.Parameters recorder, object obj, FieldInfo field, string name)
        {
            RecordParamPrivateInternalMethod.MakeGenericMethod(field.FieldType).Invoke(null, new object[] { recorder, obj, field, name });
        }

        private static MethodInfo RecordParamPrivateInternalMethod = typeof(Util).GetMethod(nameof(RecordParamPrivateInternal), BindingFlags.NonPublic | BindingFlags.Static);
        private static void RecordParamPrivateInternal<T>(this Recorder.Parameters recorder, object obj, FieldInfo field, string name)
        {
            if (recorder.Mode == Recorder.Direction.Read)
            {
                T member = default(T);
                recorder.Record(ref member, name);
                field.SetValue(obj, member);
            }
            else
            {
                T member = (T)field.GetValue(obj);
                recorder.Record(ref member, name);
            }
        }

        private static MethodInfo RecordBasePrivateInternalMethod = typeof(Util).GetMethod(nameof(RecordBasePrivateInternal), BindingFlags.NonPublic | BindingFlags.Static);
        private static void RecordBasePrivateInternal<T>(this Recorder recorder, object obj, FieldInfo field, string name)
        {
            if (recorder.Mode == Recorder.Direction.Read)
            {
                T member = default(T);
                recorder.Record(ref member, name);
                field.SetValue(obj, member);
            }
            else
            {
                T member = (T)field.GetValue(obj);
                recorder.Record(ref member, name);
            }
        }

        internal static string SanitizeForXMLToken(string input)
        {
            return input.Replace("<", "LAB").Replace(">", "RAB");
        }

        internal static Recorder.Parameters SharedIfPossible<T>(this Recorder recorder)
        {
            if (global::Dec.Util.CanBeShared(typeof(T)))
            {
                return recorder.Shared();
            }
            else
            {
                // okay this is nasty; I'm taking advantage of the fact that I know how the internals work
                // I really should come up with a better approach here
                // admittedly, that better approach might be "structify recorder", I'd love to get rid of that object churn
                return recorder.WithFactory(null);
            }
        }

        internal static FieldInfo GetPrivateFieldInHierarchy(this Type type, string name)
        {
            while (type != null)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    return field;
                }
                type = type.BaseType;
            }

            return null;
        }
    }
}
