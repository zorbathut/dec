using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Dec
{
    // Compares keys the way a given dictionary does, so keys gathered on the side match up exactly as they will once they're in it. A dictionary type without a readable Comparer gets default equality.
    internal sealed class ComparerDictionaryKey : IEqualityComparer<object>
    {
        private readonly IEqualityComparer nonGeneric;
        private readonly object generic;
        private readonly MethodInfo genericEquals;
        private readonly MethodInfo genericGetHashCode;

        public ComparerDictionaryKey(IDictionary dict)
        {
            // Not GetProperty, which throws if a subclass hides Comparer with its own; this takes whichever reflection lists first.
            object comparer = null;
            foreach (var property in dict.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.Name == "Comparer" && property.GetIndexParameters().Length == 0)
                {
                    comparer = property.GetValue(dict);
                    break;
                }
            }

            // The built-in comparers are non-generic as well, which is far faster to call than going through reflection.
            nonGeneric = comparer as IEqualityComparer;
            if (nonGeneric != null || comparer == null)
            {
                return;
            }

            var comparerInterface = comparer.GetType().GetGenericInterfaceArguments(typeof(IEqualityComparer<>));
            if (comparerInterface == null)
            {
                return;
            }

            var interfaceType = typeof(IEqualityComparer<>).MakeGenericType(comparerInterface);
            generic = comparer;
            genericEquals = interfaceType.GetMethod("Equals");
            genericGetHashCode = interfaceType.GetMethod("GetHashCode");
        }

        bool IEqualityComparer<object>.Equals(object lhs, object rhs)
        {
            if (nonGeneric != null)
            {
                return nonGeneric.Equals(lhs, rhs);
            }
            else if (generic != null)
            {
                return (bool)genericEquals.Invoke(generic, new[] { lhs, rhs });
            }
            else
            {
                return Equals(lhs, rhs);
            }
        }

        int IEqualityComparer<object>.GetHashCode(object obj)
        {
            if (nonGeneric != null)
            {
                return nonGeneric.GetHashCode(obj);
            }
            else if (generic != null)
            {
                return (int)genericGetHashCode.Invoke(generic, new[] { obj });
            }
            else
            {
                return obj.GetHashCode();
            }
        }
    }
}
