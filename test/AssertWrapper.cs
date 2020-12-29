namespace DecTest.AssertWrapper
{
    // This is a wrapper for a subset of NUnit.Framework.Assert, so we can intercept it and handle it in cases where we expect (and desire) failure.
    public static class Assert
    {
        public static void IsTrue(bool condition)
        {
            NUnit.Framework.Assert.IsTrue(condition);
        }

        public static void AreEqual(object expected, object actual)
        {
            NUnit.Framework.Assert.AreEqual(expected, actual);
        }

        public static void AreSame(object expected, object actual)
        {
            NUnit.Framework.Assert.AreSame(expected, actual);
        }

        public static void IsNull(object anObject)
        {
            NUnit.Framework.Assert.IsNull(anObject);
        }
    }
}
