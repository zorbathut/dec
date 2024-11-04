using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace DecTest
{
    [TestFixture]
    public class Validation : Base
    {
        public Action<T> GenerateValidationFunction<T>(T input)
        {
            var code = Dec.Recorder.WriteValidation(input);

            var ComposeCSFormatted = Assembly.GetAssembly(typeof(Dec.Dec)).GetType("Dec.UtilType").GetMethod("ComposeCSFormatted", BindingFlags.NonPublic | BindingFlags.Static);

            string source = @"
                    using DecTest.AssertWrapper;

                    public static class TestClass
                    {
                        public static void Test(" + ComposeCSFormatted.Invoke(null, new object[] { input.GetType() }) + @" input)
                        {
                            " + code + @"
                        }
                    }";

            var assembly = DecUtilLib.Compilation.Compile(source, new Assembly[] { this.GetType().Assembly });
            var t = assembly.GetType("TestClass");
            var m = t.GetMethod("Test");

            return test => m.Invoke(null, new object[] { test });
        }

        public void TestValidation<T>(T input)
        {
            // lol
            GenerateValidationFunction(input)(input);
        }

        public Action<T> GenerateFailingValidationFunction<T>(T input)
        {
            var validation = GenerateValidationFunction(input);

            return test =>
            {
                bool gotError = false;
                AssertWrapper.Assert.FailureCallback = () => gotError = true;
                validation(test);
                AssertWrapper.Assert.FailureCallback = null;
                Assert.IsTrue(gotError);
            };
        }

        [Test]
        public void List()
        {
            var list = new List<int>();
            list.Add(1);
            list.Add(2);

            TestValidation(list);
        }

        [Test]
        public void ListAddition()
        {
            var list = new List<int>();
            list.Add(1);
            list.Add(2);

            var validate = GenerateFailingValidationFunction(list);

            list.Add(3);

            validate(list);
        }

        [Test]
        public void ListRemoval()
        {
            var list = new List<int>();
            list.Add(1);
            list.Add(2);

            var validate = GenerateFailingValidationFunction(list);

            list.RemoveAt(0);

            validate(list);
        }

        [Test]
        public void ListChange()
        {
            var list = new List<int>();
            list.Add(1);
            list.Add(2);

            var validate = GenerateFailingValidationFunction(list);

            list[0] = 10;

            validate(list);
        }

        [Test]
        public void Dictionary()
        {
            var dict = new Dictionary<int, int>();
            dict[1] = 2;
            dict[3] = 4;

            TestValidation(dict);
        }

        [Test]
        public void DictionaryAddition()
        {
            var dict = new Dictionary<int, int>();
            dict[1] = 2;
            dict[3] = 4;

            var validate = GenerateFailingValidationFunction(dict);

            dict[5] = 6;

            validate(dict);
        }

        [Test]
        public void DictionaryRemoval()
        {
            var dict = new Dictionary<int, int>();
            dict[1] = 2;
            dict[3] = 4;

            var validate = GenerateFailingValidationFunction(dict);

            dict.Remove(3);

            validate(dict);
        }

        [Test]
        public void DictionaryChange()
        {
            var dict = new Dictionary<int, int>();
            dict[1] = 2;
            dict[3] = 4;

            var validate = GenerateFailingValidationFunction(dict);

            dict[1] = 10;

            validate(dict);
        }

        [Test]
        public void HashSet()
        {
            var set = new HashSet<int>();
            set.Add(1);
            set.Add(2);

            TestValidation(set);
        }

        [Test]
        public void HashSetAddition()
        {
            var set = new HashSet<int>();
            set.Add(1);
            set.Add(2);

            var validate = GenerateFailingValidationFunction(set);

            set.Add(3);

            validate(set);
        }

        [Test]
        public void HashSetRemoval()
        {
            var set = new HashSet<int>();
            set.Add(1);
            set.Add(2);

            var validate = GenerateFailingValidationFunction(set);

            set.Remove(2);

            validate(set);
        }
    }
}

