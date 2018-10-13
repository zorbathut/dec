namespace Def
{
    using System;

    /// <summary>
    /// Contains global configuration data that may be needed before parsing.
    /// Initialize as soon as possible.
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// Callback for informational messages.
        /// </summary>
        /// <remarks>
        /// This should be made visible in debug mode.
        ///
        /// If you're using any multithreading, this must be threadsafe.
        /// </remarks>
        public static Action<string> InfoHandler { get; set; } = str => System.Diagnostics.Debug.Print(str);

        /// <summary>
        /// Callback for warning messages.
        /// </summary>
        /// <remarks>
        /// This should be made visible to developers and testers.
        ///
        /// If you're using any multithreading, this must be threadsafe.
        /// </remarks>
        public static Action<string> WarningHandler { get; set; } = str => System.Diagnostics.Debug.Print(str);

        /// <summary>
        /// Callback for error messages.
        /// </summary>
        /// <remarks>
        /// This should be made unmissably visible to developers and testers, ideally with a popup or a modal dialog.
        /// 
        /// Can be made to throw an exception. If it does, the exception will propagate to the caller. Otherwise, def will attempt to recover from the error.
        ///
        /// If you're using any multithreading, this must be threadsafe.
        /// </remarks>
        public static Action<string> ErrorHandler { get; set; } = str => { System.Diagnostics.Debug.Print(str); Console.WriteLine(str); throw new ArgumentException(str); };

        /// <summary>
        /// Callback for unhandled exceptions.
        /// </summary>
        /// <remarks>
        /// This should be made unmissably visible to developers and testers, ideally with a popup or a modal dialog.
        /// 
        /// Can be made to rethrow the exception or throw a new exception. If it does, the exception will propagate to the caller. Otherwise, def will attempt to recover from the error.
        ///
        /// If you're using any multithreading, this must be threadsafe.
        /// </remarks>
        public static Action<Exception> ExceptionHandler { get; set; } = e => { System.Diagnostics.Debug.Print(e.ToString()); Console.WriteLine(e.ToString()); throw e; };
    }
}
