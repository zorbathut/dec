using System;

namespace DecTest
{
    // Entry-tree helpers for the Reflection golden tests.
    public partial class Base
    {
        // A value distinguishable from the one already at a position, of the same runtime type; null where none can be made, meaning the existing value is written back unchanged.
        protected static object Sentinel(object value)
        {
            if (value is Enum)
            {
                foreach (var member in Enum.GetValues(value.GetType()))
                {
                    if (!Equals(member, value))
                    {
                        return member;
                    }
                }

                return null;
            }

            object candidate;
            switch (value)
            {
                case bool v:
                    candidate = !v;
                    break;
                case byte v:
                    candidate = unchecked((byte)(v + 1));
                    break;
                case sbyte v:
                    candidate = unchecked((sbyte)(v + 1));
                    break;
                case short v:
                    candidate = unchecked((short)(v + 1));
                    break;
                case ushort v:
                    candidate = unchecked((ushort)(v + 1));
                    break;
                case int v:
                    candidate = unchecked(v + 1);
                    break;
                case uint v:
                    candidate = unchecked(v + 1);
                    break;
                case long v:
                    candidate = unchecked(v + 1);
                    break;
                case ulong v:
                    candidate = unchecked(v + 1);
                    break;
                case float v:
                    candidate = v + 1;
                    break;
                case double v:
                    candidate = v + 1;
                    break;
                case char v:
                    candidate = unchecked((char)(v + 1));
                    break;
                case string v:
                    candidate = v + "!";
                    break;
                default:
                    return null;
            }

            // Extremes such as float.MaxValue or NaN absorb the change, and a sentinel equal to the original would verify nothing.
            return Equals(candidate, value) ? null : candidate;
        }

        protected static Dec.Reflection.Entry FindByPath(Dec.Reflection.Entry entry, Dec.Path path)
        {
            if (entry.Path.Equals(path))
            {
                return entry;
            }

            foreach (var child in entry.Children)
            {
                var found = FindByPath(child, path);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
