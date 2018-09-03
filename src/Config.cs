namespace Def
{
    using System;

    public static class Config
    {
        public static Action<string> InfoHandler { get; set; } = str => System.Diagnostics.Debug.Print(str);
        public static Action<string> WarningHandler { get; set; } = str => System.Diagnostics.Debug.Print(str);
        public static Action<string> ErrorHandler { get; set; } = str => { System.Diagnostics.Debug.Print(str); Console.WriteLine(str); throw new ArgumentException(str); };
        public static Action<Exception> ExceptionHandler { get; set; } = e => ErrorHandler(e.ToString());
    }
}
