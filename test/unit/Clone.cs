using System.Collections.Generic;
using System.Linq;
using Dec;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class Clone : Base
    {
        public class CloneWithRecordableClass : IRecordable
        {
            public void Record(Dec.Recorder recorder) { }
        }

        public struct CloneWithRecordableStruct : IRecordable
        {
            public List<int> list;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref list, "list");
            }
        }

        [Dec.CloneWithAssignment]
        public class CloneWithAssignmentClass
        {

        }

        [Dec.CloneWithAssignment]
        public struct CloneWithAssignmentStruct
        {
            public List<int> list;
        }


        [Dec.CloneWithAssignment]
        public class CloneWithAssignmentResolutionClass : IRecordable
        {
            public void Record(Dec.Recorder recorder) { }
        }

        [Dec.CloneWithAssignment]
        public struct CloneWithAssignmentResolutionStruct : IRecordable
        {
            public List<int> list;

            public void Record(Dec.Recorder recorder)
            {
                recorder.Record(ref list, "list");
            }
        }

        [Test]
        public void WithAssignmentAttribute_CloneWithRecordableClass()
        {
            var cwrc = new CloneWithRecordableClass();
            var cwrcClone = Dec.Recorder.Clone(cwrc);
            Assert.AreNotSame(cwrc, cwrcClone);
        }

        [Test]
        public void WithAssignmentAttribute_CloneWithRecordableStruct()
        {
            var cwrs = new CloneWithRecordableStruct();
            cwrs.list = new List<int>();
            var cwrsClone = Dec.Recorder.Clone(cwrs);
            Assert.AreNotSame(cwrs, cwrsClone);
            Assert.AreNotSame(cwrs.list, cwrsClone.list);
        }

        [Test]
        public void WithAssignmentAttribute_CloneWithAssignmentClass()
        {
            var cwac = new CloneWithAssignmentClass();
            var cwacClone = Dec.Recorder.Clone(cwac);
            Assert.AreSame(cwac, cwacClone);
        }

        [Test]
        public void WithAssignmentAttribute_CloneWithAssignmentStruct()
        {
            var cwas = new CloneWithAssignmentStruct();
            cwas.list = new List<int>();
            var cwasClone = Dec.Recorder.Clone(cwas);
            Assert.AreNotSame(cwas, cwasClone);
            Assert.AreSame(cwas.list, cwasClone.list);
        }

        [Test]
        public void WithAssignmentAttribute_CloneWithAssignmentResolutionClass()
        {
            var cwarc = new CloneWithAssignmentResolutionClass();
            var cwarcClone = Dec.Recorder.Clone(cwarc);
            Assert.AreSame(cwarc, cwarcClone);
        }

        [Test]
        public void WithAssignmentAttribute_CloneWithAssignmentResolutionStruct()
        {
            var cwars = new CloneWithAssignmentResolutionStruct();
            cwars.list = new List<int>();
            var cwarsClone = Dec.Recorder.Clone(cwars);
            Assert.AreNotSame(cwars, cwarsClone);
            Assert.AreSame(cwars.list, cwarsClone.list);
        }

        [Test]
        public void WithAssignmentAttribute_CloneArrayWithRecordableClass()
        {
            var cwrc = new CloneWithRecordableClass();
            var cwrcArray = new[] { cwrc };
            var cwrcArrayClone = Dec.Recorder.Clone(cwrcArray);
            Assert.AreNotSame(cwrcArray, cwrcArrayClone);
            Assert.AreNotSame(cwrc, cwrcArrayClone[0]);
        }

        [Test]
        public void WithAssignmentAttribute_CloneArrayWithRecordableStruct()
        {
            var cwrs = new CloneWithRecordableStruct();
            cwrs.list = new List<int>();
            var cwrsArray = new[] { cwrs };
            var cwrsArrayClone = Dec.Recorder.Clone(cwrsArray);
            Assert.AreNotSame(cwrsArray, cwrsArrayClone);
            Assert.AreNotSame(cwrs, cwrsArrayClone[0]);
            Assert.AreNotSame(cwrs.list, cwrsArrayClone[0].list);
        }

        [Test]
        public void WithAssignmentAttribute_CloneArrayWithAssignmentClass()
        {
            var cwac = new CloneWithAssignmentClass();
            var cwacArray = new[] { cwac };
            var cwacArrayClone = Dec.Recorder.Clone(cwacArray);
            Assert.AreNotSame(cwacArray, cwacArrayClone);
            Assert.AreSame(cwac, cwacArrayClone[0]);
        }

        [Test]
        public void WithAssignmentAttribute_CloneArrayWithAssignmentStruct()
        {
            var cwas = new CloneWithAssignmentStruct();
            cwas.list = new List<int>();
            var cwasArray = new[] { cwas };
            var cwasArrayClone = Dec.Recorder.Clone(cwasArray);
            Assert.AreNotSame(cwasArray, cwasArrayClone);
            Assert.AreNotSame(cwas, cwasArrayClone[0]);
            Assert.AreSame(cwas.list, cwasArrayClone[0].list);
        }

        [Test]
        public void WithAssignmentAttribute_CloneArrayWithAssignmentResolutionClass()
        {
            var cwarc = new CloneWithAssignmentResolutionClass();
            var cwarcArray = new[] { cwarc };
            var cwarcArrayClone = Dec.Recorder.Clone(cwarcArray);
            Assert.AreNotSame(cwarcArray, cwarcArrayClone);
            Assert.AreSame(cwarc, cwarcArrayClone[0]);
        }

        [Test]
        public void WithAssignmentAttribute_CloneArrayWithAssignmentResolutionStruct()
        {
            var cwars = new CloneWithAssignmentResolutionStruct();
            cwars.list = new List<int>();
            var cwarsArray = new[] { cwars };
            var cwarsArrayClone = Dec.Recorder.Clone(cwarsArray);
            Assert.AreNotSame(cwarsArray, cwarsArrayClone);
            Assert.AreNotSame(cwars, cwarsArrayClone[0]);
            Assert.AreSame(cwars.list, cwarsArrayClone[0].list);
        }

        [Test]
        public void WithAssignmentAttribute_Dict11()
        {
            Dictionary<CloneWithAssignmentClass, CloneWithAssignmentClass> dict = new Dictionary<CloneWithAssignmentClass, CloneWithAssignmentClass>();
            dict[new CloneWithAssignmentClass()] = new CloneWithAssignmentClass();

            var dictClone = Dec.Recorder.Clone(dict);
            Assert.AreSame(dict.First().Key, dictClone.First().Key);
            Assert.AreSame(dict.First().Value, dictClone.First().Value);
        }

        [Test]
        public void WithAssignmentAttribute_Dict10()
        {
            Dictionary<CloneWithAssignmentClass, CloneWithRecordableClass> dict = new Dictionary<CloneWithAssignmentClass, CloneWithRecordableClass>();
            dict[new CloneWithAssignmentClass()] = new CloneWithRecordableClass();

            var dictClone = Dec.Recorder.Clone(dict);
            Assert.AreSame(dict.First().Key, dictClone.First().Key);
            Assert.AreNotSame(dict.First().Value, dictClone.First().Value);
        }

        [Test]
        public void WithAssignmentAttribute_Dict01()
        {
            Dictionary<CloneWithRecordableClass, CloneWithAssignmentClass> dict = new Dictionary<CloneWithRecordableClass, CloneWithAssignmentClass>();
            dict[new CloneWithRecordableClass()] = new CloneWithAssignmentClass();

            var dictClone = Dec.Recorder.Clone(dict);
            Assert.AreNotSame(dict.First().Key, dictClone.First().Key);
            Assert.AreSame(dict.First().Value, dictClone.First().Value);
        }
    }
}
