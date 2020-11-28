
namespace Dec
{
    internal static class Compat
    {
        // See https://github.com/dotnet/runtime/issues/12035
        internal static bool FloatRoundtripBroken { get; private set; }

        // it's possible that static constructors have a serious speed hit
        // in which case I'll need to deal with this some other way.
        static Compat()
        {
            FloatRoundtripBroken = -8.22272715124268E-63 != double.Parse("-8.22272715124268E-63");
        }
    }
}
