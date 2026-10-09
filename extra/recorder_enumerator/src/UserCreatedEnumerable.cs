using System;
using System.Reflection;

namespace Dec.RecorderEnumerator
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RecordableEnumerableAttribute : Attribute { }

    public class UserCreatedEnumerableConverter : ConverterFactoryFields
    {
        // C#-compiler-generated iterator state machines carry an `<>l__initialThreadId` field, set in the constructor to the creating thread's ManagedThreadId, used by GetEnumerator() to decide whether the iterator object can be reused. We don't preserve or checksum it: serialized iterators are intended to be resumed via MoveNext() rather than re-enumerated via GetEnumerator(), and the field's value would otherwise vary by recording thread, polluting checksums. Reset to -1 (an invalid ManagedThreadId, which are positive) on construction so any future GetEnumerator() call produces a fresh state machine rather than aliasing this one.
        // There is a potential mismatch here involving cases where we get an enumerable, clone it, then get enumerables, and store them together. Technically these would be shared objects in the first case and not the second. I have absolutely no idea how to deal with this because this means our state depends on which thread something was created in *and* which thread something is running in, neither of which necessarily has any relationship to each other or to the Record()/Clone() call.
        private const string InitialThreadIdFieldName = "<>l__initialThreadId";
        private const int InvalidThreadId = -1;

        readonly Type enumerableType;
        readonly FieldInfo initialThreadIdField;

        public UserCreatedEnumerableConverter(Type type) : base(type, Util.RecordFieldsAll(type, InitialThreadIdFieldName), IgnoredLabels(type))
        {
            enumerableType = type;
            initialThreadIdField = type.GetField(InitialThreadIdFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        // Older files recorded the thread id; ignoring its label keeps reading them from warning about it.
        private static string[] IgnoredLabels(Type type)
        {
            if (type.GetField(InitialThreadIdFieldName, BindingFlags.Instance | BindingFlags.NonPublic) == null)
            {
                return new string[0];
            }

            return new[] { Util.SanitizeForXMLToken(InitialThreadIdFieldName) };
        }

        public override object Create(Recorder recorder)
        {
            // appears to be a sentinel value for "hasn't yet 'created' an 'instance'", which this currently hasn't
            // we'll overwrite this later though
            var instance = Activator.CreateInstance(enumerableType, -2);

            // Overwrite the initialThreadId the constructor just stamped in with the current thread's id.
            initialThreadIdField?.SetValue(instance, InvalidThreadId);

            return instance;
        }
    }
}
