using NUnit.Framework;

namespace RecorderEnumeratorTest
{
    // Every suite again on the reflection fallback that ConverterFactoryFields uses where the runtime can't generate code; RecordedOutput's pinned output holds both paths to the same format.
    [TestFixture]
    public class ContainerFallback : Container
    {
        public ContainerFallback()
        {
            forceFallbackEmit = true;
        }
    }

    [TestFixture]
    public class DelegateTestFallback : DelegateTest
    {
        public DelegateTestFallback()
        {
            forceFallbackEmit = true;
        }
    }

    [TestFixture]
    public class EnumerFallback : Enumer
    {
        public EnumerFallback()
        {
            forceFallbackEmit = true;
        }
    }

    [TestFixture]
    public class FunctionFallback : Function
    {
        public FunctionFallback()
        {
            forceFallbackEmit = true;
        }
    }

    [TestFixture]
    public class LinqFallback : Linq
    {
        public LinqFallback()
        {
            forceFallbackEmit = true;
        }
    }

    [TestFixture]
    public class RecordedOutputFallback : RecordedOutput
    {
        public RecordedOutputFallback()
        {
            forceFallbackEmit = true;
        }
    }
}
