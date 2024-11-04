using System;
using System.Text.RegularExpressions;

namespace Dec
{
    public static class Util
    {
        /// <summary>
        /// Indicates whether instances of a type can be shared or not.
        /// </summary>
        public static bool CanBeShared(Type type)
        {
            bool canBeShared = !type.IsValueType && !typeof(Dec).IsAssignableFrom(type) && !typeof(Enum).IsAssignableFrom(type) && type != typeof(string) && type != typeof(Type);
            if (!canBeShared)
            {
                return false;
            }

            var converter = Serialization.ConverterFor(type);
            return !converter?.TreatAsValuelike() ?? true;
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
