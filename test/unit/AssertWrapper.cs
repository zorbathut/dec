using System;

namespace DecTest.AssertWrapper
{
    // This is a wrapper for a subset of NUnit.Framework.Assert, so we can intercept it and handle it in cases where we expect (and desire) failure.
    public static class Assert
    {
        public static Action FailureCallback = null;

        public static void IsTrue(bool condition)
        {
            if (FailureCallback != null && !condition)
            {
                FailureCallback();
                return;
            }

            NUnit.Framework.Assert.IsTrue(condition);
        }

        public static void AreEqual(object expected, object actual)
        {
            if (FailureCallback != null && !NUnit.Framework.Is.EqualTo(actual).ApplyTo(expected).IsSuccess)
            {
                FailureCallback();
                return;
            }

            NUnit.Framework.Assert.AreEqual(expected, actual);
        }

        public static void AreSame(object expected, object actual)
        {
            if (FailureCallback != null && !NUnit.Framework.Is.SameAs(actual).ApplyTo(expected).IsSuccess)
            {
                FailureCallback();
                return;
            }

            NUnit.Framework.Assert.AreSame(expected, actual);
        }

        public static void IsNull(object anObject)
        {
            if (FailureCallback != null && anObject != null)
            {
                FailureCallback();
                return;
            }

            NUnit.Framework.Assert.IsNull(anObject);
        }

        public static void Fail()
        {
            if (FailureCallback != null)
            {
                FailureCallback();
                return;
            }

            NUnit.Framework.Assert.Fail();
        }
    }
}
