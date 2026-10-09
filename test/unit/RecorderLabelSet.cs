using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class RecorderLabelSet : Base
    {
        [Test]
        public void Semantics([Values(1, Dec.RecorderLabelSet.Threshold - 1, Dec.RecorderLabelSet.Threshold, Dec.RecorderLabelSet.Threshold + 1, Dec.RecorderLabelSet.Threshold * 3)] int size)
        {
            var set = new Dec.RecorderLabelSet();

            // The second round runs on the cleared set.
            for (int round = 0; round < 2; ++round)
            {
                for (int i = 0; i < size; ++i)
                {
                    Assert.IsFalse(set.Contains("label" + i));
                    Assert.IsTrue(set.Add("label" + i));
                    Assert.AreEqual(i + 1, set.Count);
                }

                // Built strings, equal to the ones added but not the same objects.
                for (int i = 0; i < size; ++i)
                {
                    Assert.IsTrue(set.Contains("label" + i));
                    Assert.IsFalse(set.Add("label" + i));
                }

                Assert.IsFalse(set.Contains("absent"));
                Assert.AreEqual(size, set.Count);

                set.Clear();
                Assert.AreEqual(0, set.Count);
                Assert.IsFalse(set.Contains("label0"));
            }
        }

        [Test]
        public void ClearAfterRefusalAtThreshold()
        {
            var set = new Dec.RecorderLabelSet();
            for (int i = 0; i < Dec.RecorderLabelSet.Threshold; ++i)
            {
                set.Add("first" + i);
            }

            Assert.IsFalse(set.Add("first0"));
            set.Clear();

            for (int i = 0; i < Dec.RecorderLabelSet.Threshold; ++i)
            {
                set.Add("second" + i);
            }

            // Past the threshold now, where none of the first round's labels may linger.
            Assert.IsTrue(set.Add("first1"));
            Assert.IsTrue(set.Contains("first1"));
            Assert.IsFalse(set.Contains("first2"));
        }
    }
}
