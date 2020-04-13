namespace Def
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    #if UNITY_5_3_OR_NEWER
        using UnityEngine;
    #endif

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
        public static Action<string> InfoHandler { get; set; }

        /// <summary>
        /// Callback for warning messages.
        /// </summary>
        /// <remarks>
        /// This should be made visible to developers and testers.
        ///
        /// If you're using any multithreading, this must be threadsafe.
        /// </remarks>
        public static Action<string> WarningHandler { get; set; }

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
        public static Action<string> ErrorHandler { get; set; }

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
        public static Action<Exception> ExceptionHandler { get; set; }

        public enum DefaultExceptionBehavior
        {
            Never,
            ErrorOnly,
            ErrorAndWarning,
        }

        /// <summary>
        /// Tells the default handlers when to throw exceptions.
        /// </summary>
        /// <remarks>
        /// Ignored if you're not using the default handlers.
        ///
        /// Def is intended to work without exceptions; it's good at recovering from errors. This is very important if you have mods, as mods frequently have minor conflicts.
        ///
        /// However, many developers don't configure the error reporting when first installing the library, often running into bizarre issues because of it.
        ///
        /// This is set to be as loud and intrusive as possible just to get developers over that initial hump. I strongly recommend changing this to DefaultExceptionBehavior.Never, then ensuring that your errors and warnings are shown in a place you can't miss (like a popup or a modal dialog.)
        /// </remarks>
        public static DefaultExceptionBehavior DefaultHandlerThrowExceptions = DefaultExceptionBehavior.ErrorAndWarning;

        /// <summary>
        /// Tells the default handlers to attach a note to every exception saying that this behavior can be changed.
        /// </summary>
        /// <remarks>
        /// Ignored if you're not using the default handlers.
        /// </remarks>
        public static bool DefaultHandlerShowConfigOnException = true;

        /// <summary>
        /// The list of namespaces that def can access transparently.
        /// </summary>
        /// <remarks>
        /// Generally this should consist of your project's primary namespace. If your project lives in multiple namespaces, you may wish to include them all.
        ///
        /// Should not be changed while a Parser or Writer object exists.
        /// </remarks>
        /// <example>
        /// Config.UsingNamespaces = new string[] { "LegendOfAzureFuton" };
        /// </example>
        public static IEnumerable<string> UsingNamespaces
        {
            get => UsingNamespaceBacking;
            set
            {
                UsingNamespaceBacking = value.ToArray();
                UtilType.ClearCache();
            }
        }
        private static string[] UsingNamespaceBacking = new string[0];

        /// <summary>
        /// Parameters that are intended for unit tests. Not recommended or supported for actual code.
        /// </summary>
        public class UnitTestParameters
        {
            public Type[] explicitTypes = null;
            public Type[] explicitStaticRefs = null;
            public Type[] explicitConverters = null;
        }
        public static UnitTestParameters TestParameters { get; set; } = null;

        static Config()
        {
            string ExceptionSuffix()
            {
                if (DefaultHandlerShowConfigOnException)
                {
                    return "\nThis error-handling behavior (as well as this message) can be modified in Def.Config.";
                }
                else
                {
                    return "";
                }
            }

            #if UNITY_5_3_OR_NEWER
                InfoHandler = (str) =>
                {
                    Debug.Log(str);
                };

                WarningHandler = (str) =>
                {
                    Debug.LogWarning(str);
                    if (DefaultHandlerThrowExceptions >= DefaultExceptionBehavior.ErrorAndWarning)
                    {
                        throw new ArgumentException(str + ExceptionSuffix());
                    }
                };

                ErrorHandler = (str) =>
                {
                    Debug.LogError(str);
                    if (DefaultHandlerThrowExceptions >= DefaultExceptionBehavior.ErrorOnly)
                    {
                        throw new ArgumentException(str + ExceptionSuffix());
                    }
                };

                ExceptionHandler = (e) =>
                {
                    Debug.LogException(e);
                    throw e;
                };
            #else
                InfoHandler = (str) =>
                {
                    System.Diagnostics.Debug.Print(str);
                };

                WarningHandler = (str) =>
                {
                    System.Diagnostics.Debug.Print(str);
                    Console.WriteLine(str);
                    if (DefaultHandlerThrowExceptions >= DefaultExceptionBehavior.ErrorAndWarning)
                    {
                        throw new ArgumentException(str + ExceptionSuffix());
                    }
                };

                ErrorHandler = (str) =>
                {
                    System.Diagnostics.Debug.Print(str);
                    Console.WriteLine(str);
                    if (DefaultHandlerThrowExceptions >= DefaultExceptionBehavior.ErrorOnly)
                    {
                        throw new ArgumentException(str + ExceptionSuffix());
                    }
                };

                ExceptionHandler = (e) =>
                {
                    System.Diagnostics.Debug.Print(e.ToString());
                    Console.WriteLine(e.ToString());
                    throw e;
                };
            #endif
        }
    }
}
