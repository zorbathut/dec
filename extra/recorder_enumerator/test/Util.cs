using System;
using System.Collections.Generic;

namespace RecorderEnumeratorTest
{

    internal static class Util
    {
        public static bool AreEquivalentEnumerators<T>(IEnumerator<T> lhs, IEnumerator<T> rhs)
        {
            if (lhs == rhs)
            {
                // no, can't be the *same* enumerator, stop that
                return false;
            }

            // Make sure the Current field has been prepped properly
            if (!object.Equals(lhs.Current, rhs.Current))
            {
                return false;
            }

            while (lhs.MoveNext())
            {
                if (!rhs.MoveNext())
                {
                    return false; // Second enumerator is shorter
                }

                if (!object.Equals(lhs.Current, rhs.Current))
                {
                    return false; // Current items are different
                }
            }

            if (rhs.MoveNext())
            {
                return false; // Second enumerator is longer
            }

            // Reset them and do it all again!
            try
            {
                lhs.Reset();
            }
            catch (NotSupportedException)
            {
                return true; // First enumerator does not support Reset
            }

            rhs.Reset();

            // run them again! just in case!
            while (lhs.MoveNext())
            {
                if (!rhs.MoveNext())
                {
                    return false; // Second enumerator is shorter
                }

                if (!object.Equals(lhs.Current, rhs.Current))
                {
                    return false; // Current items are different
                }
            }

            if (rhs.MoveNext())
            {
                return false; // Second enumerator is longer
            }

            return true;
        }
    }
}
