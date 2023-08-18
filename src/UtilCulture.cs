
using System;
using System.Globalization;
using System.Threading;
 
namespace Dec
{
    internal class CultureInfoScope : IDisposable
    {
        private readonly CultureInfo originalCulture;
        private readonly CultureInfo intendedCulture;

        public CultureInfoScope(CultureInfo culture)
        {
            this.originalCulture = CultureInfo.CurrentCulture;
            this.intendedCulture = culture;

            Thread.CurrentThread.CurrentCulture = culture;
        }
 
        public void Dispose()
        {
            if (Thread.CurrentThread.CurrentCulture == intendedCulture)
            {
                Thread.CurrentThread.CurrentCulture = this.originalCulture;
            }
            else
            {
                Dbg.Err($"Current culture unexpectedly changed from {intendedCulture} to {Thread.CurrentThread.CurrentCulture}; this may cause parse errors");
            }
        }
    }
}