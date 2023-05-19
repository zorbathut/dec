
using System;
using System.Globalization;
using System.Threading;
 
namespace Dec
{
    internal class CultureInfoScope : IDisposable
    {
        private readonly CultureInfo originalCulture;
 
        public CultureInfoScope(CultureInfo culture)
        {
            this.originalCulture = CultureInfo.CurrentCulture;
 
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
        }
 
        public void Dispose()
        {
            Thread.CurrentThread.CurrentCulture = this.originalCulture;
            Thread.CurrentThread.CurrentUICulture = this.originalCulture;
        }
    }
}