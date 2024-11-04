using System;
using System.Collections.Generic;
using System.Reflection;

namespace Dec.RecorderEnumerator
{
    public static class SystemCollections_List_Enumerator_Converter
    {
        internal static Type RelevantType = typeof(System.Collections.Generic.List<>.Enumerator);
    }

    public class SystemCollections_List_Enumerator_Converter<Iterator, T> : ConverterRecordDynamic
    {
        internal FieldInfo field_List = typeof(Iterator).GetField("_list", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Index = typeof(Iterator).GetField("_index", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Current = typeof(Iterator).GetField("_current", BindingFlags.NonPublic | BindingFlags.Instance);
        internal FieldInfo field_Version = typeof(Iterator).GetField("_version", BindingFlags.NonPublic | BindingFlags.Instance);

        internal FieldInfo field_List_Version = typeof(List<T>).GetField("_version", BindingFlags.NonPublic | BindingFlags.Instance);

        private void StandardRecord(object input, Recorder recorder)
        {
            recorder.Shared().RecordPrivate(input, field_List, "list");
            recorder.RecordPrivate(input, field_Index, "index");
            recorder.SharedIfPossible<T>().RecordPrivate(input, field_Current, "current");
        }

        public override object Record(ref object input, Recorder recorder)
        {
            recorder.Shared().RecordPrivate(input, field_List, "list");
            recorder.RecordPrivate(input, field_Index, "index");
            recorder.SharedIfPossible<T>().RecordPrivate(input, field_Current, "current");

            if (recorder.Mode == Recorder.Direction.Write)
            {
                // Figure out if we're up-to-date
                var list = field_List.GetValue(input);

                // this can be null, so make sure we handle this properly
                bool valid = false;
                if (list != null)
                {
                    int enumeratorVersion = (int)field_Version.GetValue(input);
                    int containerVersion = (int)field_List_Version.GetValue(list);
                    valid = enumeratorVersion == containerVersion;
                }

                recorder.Record(ref valid, "valid");
            }
            else
            {
                // We don't want to serialize the version along with the list, because it's pointless for anyone who isn't doing this madness
                // At the same time, the list isn't guaranteed to be initialized by now, and we normally wouldn't know what version the list would have
                // But Dec specifically sets the version, so we do know! We just set this relative to our known hardcoded global value.
                bool valid = true;
                recorder.Record(ref valid, "valid");
                field_Version.SetValue(input, valid ? global::Dec.Util.CollectionDeserializationVersion : global::Dec.Util.CollectionDeserializationVersion - 1);
            }

            return input;
        }
    }

    public static class SystemCollections_Generic_GenericComparer
    {
        internal static Type RelevantType = typeof(System.Collections.Generic.List<>).Assembly.GetType("System.Collections.Generic.GenericComparer`1");
    }

    public class SystemCollections_Generic_GenericComparer<Iterator> : ConverterRecordDynamic
    {
        public override object Record(ref object input, Recorder recorder)
        {
            // there's actually no fields
            // womp womp
            return input;
        }
    }
}
