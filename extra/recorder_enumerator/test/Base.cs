using Dec;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace RecorderEnumeratorTest
{
    [TestFixture]
    public class Base : DecTest.Base
    {
        [OneTimeSetUp]
        public void PrepConfig()
        {
            Config.ConverterFactory = global::Dec.RecorderEnumerator.Config.ConverterFactory;
        }
    }
}