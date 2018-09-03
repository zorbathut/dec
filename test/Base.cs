namespace DefTest
{
    using NUnit.Framework;

    [TestFixture]
    public class Base
    {
        [SetUp] [TearDown]
        public void Clean()
        {
            Def.Database.Clear();
        }
    }
}
