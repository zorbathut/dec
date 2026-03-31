using System;
using System.Collections.Concurrent;

namespace Dec
{
    public static class Util
    {
        private static ConcurrentDictionary<Type, bool> CanBeSharedCache = new ConcurrentDictionary<Type, bool>();

        /// <summary>
        /// Indicates whether instances of a type can be shared or not.
        /// </summary>
        public static bool CanBeShared(Type type)
        {
            if (CanBeSharedCache.TryGetValue(type, out var result))
            {
                return result;
            }

            bool canBeShared = !type.IsValueType && !typeof(Dec).IsAssignableFrom(type) && !typeof(Enum).IsAssignableFrom(type) && type != typeof(string) && type != typeof(Type);
            if (!canBeShared)
            {
                CanBeSharedCache[type] = false;
                return false;
            }

            var converter = Serialization.ConverterFor(type);
            result = !converter?.TreatAsValuelike() ?? true;

            CanBeSharedCache[type] = result;
            return result;
        }

        /// <summary>
        /// The internal collection version applied to collections on deserialization.
        /// </summary>
        /// <remarks>
        /// This should not matter to you unless you're doing deep black magic.
        /// </remarks>
        public const int CollectionDeserializationVersion = 424242;
    }
}
