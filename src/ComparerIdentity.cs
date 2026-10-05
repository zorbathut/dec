using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Dec
{
    // Compares by reference identity, never calling a type's own Equals or GetHashCode. The BCL's ReferenceEqualityComparer doesn't exist on netstandard2.1.
    internal sealed class ComparerIdentity : IEqualityComparer<object>
    {
        public static readonly ComparerIdentity Instance = new ComparerIdentity();

        bool IEqualityComparer<object>.Equals(object lhs, object rhs)
        {
            return ReferenceEquals(lhs, rhs);
        }

        int IEqualityComparer<object>.GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
