namespace Dec
{
    using System;
    using System.Text.RegularExpressions;

    public static class Util
    {
        /// <summary>
        /// Indicates whether instances of a type can be shared or not.
        /// </summary>
        public static bool CanBeShared(Type type)
        {
            return !type.IsValueType && !typeof(Dec).IsAssignableFrom(type) && type != typeof(string) && type != typeof(Type);
        }
    }
}
