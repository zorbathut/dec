namespace Dec.RecorderCoroutine
{
    using System;

    public static class Config
    {
        public static Converter ConverterFactory(Type type)
        {
            if (type == SystemLinqEnumerable_RangeIterator_Converter.RelevantType)
            {
                return new SystemLinqEnumerable_RangeIterator_Converter();
            }

            return null;
        }
    }
}
