using System;
using System.Collections.Generic;
using System.Linq;

namespace Dec
{
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
        /// Can be made to throw an exception. If it does, the exception will propagate to the caller. Otherwise, dec will attempt to recover from the error.
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
        /// Can be made to rethrow the exception or throw a new exception. If it does, the exception will propagate to the caller. Otherwise, dec will attempt to recover from the error.
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
        /// Dec is intended to work without exceptions; it's good at recovering from errors. This is very important if you have mods, as mods frequently have minor conflicts.
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
        /// The culture to use for parsing and writing values.
        /// </summary>
        /// <remarks>
        /// This must be set statically, rather than loaded from the user's system, or parsing might break unpredictably. Recommend leaving this set to InvariantCulture for compatibility with the general Dec ecosystem; other options may have bugs (but report them and I'll fix them!)
        ///
        /// Changing this while Dec is running is undefined behavior. Don't do that. Dec may be unable to read files written under a different CultureInfo; if you don't want that to be a problem, well, choose today, and choose wisely.
        ///
        /// (just leave it set to its default for christ's sake)
        /// </remarks>
        public static System.Globalization.CultureInfo CultureInfo = System.Globalization.CultureInfo.InvariantCulture;

        /// <summary>
        /// The list of namespaces that dec can access transparently.
        /// </summary>
        /// <remarks>
        /// Generally this should consist of your project's primary namespace. If your project lives in multiple namespaces, you may wish to include them all.
        ///
        /// Should not be changed while a Parser or Composer object exists.
        /// </remarks>
        /// <example>
        /// Config.UsingNamespaces = new string[] { "LegendOfAmethystFuton" };
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
        /// A remapping of old type names to actual types, for compatibility with older files.
        /// </summary>
        /// <remarks>
        /// This will do reasonable things with generic parameters and arrays and the like. As of this writing, it's whitespace-sensitive, but this is not guaranteed to remain the case, although if it changes it will print warnings on whitespace-related ambiguity.
        ///
        /// This should not be changed while a Parser or Composer object exists, or while Recorder is active.
        /// </remarks>
        /// <example>
        /// Config.CompatTypeLookup = new Dictionary&lt;string, Type&gt;() { { "OldNamespace.OldTypeName", typeof(NewNamespace.NewTypeName) } };
        /// </example>
        public static Dictionary<string, Type> CompatTypeLookup
        {
            get => CompatTypeLookupBacking ?? new Dictionary<string, Type>();
            set
            {
                CompatTypeLookupBacking = value;
                UtilType.ClearCache();
            }
        }

        private static Dictionary<string, Type> CompatTypeLookupBacking;

        /// <summary>
        /// A remapping of old Dec names to new Dec names, scoped per Dec type, for compatibility with older files.
        /// </summary>
        /// <remarks>
        /// The outer dictionary key is the Dec type. The inner dictionary maps old DecName strings to new DecName strings.
        ///
        /// When looking up a Dec by name, if the name is not found, this lookup is checked.
        /// The lookup walks up the type hierarchy to find applicable remappings.
        ///
        /// This should not be changed while a Parser or Composer object exists, or while Recorder is active. You may get weird results by changing it after Parser has finished; you should probably just be setting it early, then not touching it.
        /// </remarks>
        /// <example>
        /// Config.CompatDecLookup = new Dictionary&lt;Type, Dictionary&lt;string, string&gt;&gt;()
        /// {
        ///     { typeof(WeaponDec), new Dictionary&lt;string, string&gt;() { { "OldSword", "NewSword" } } },
        /// };
        /// </example>
        public static Dictionary<Type, Dictionary<string, string>> CompatDecLookup
        {
            get => CompatDecLookupBacking ?? new Dictionary<Type, Dictionary<string, string>>();
            set => CompatDecLookupBacking = value;
        }

        private static Dictionary<Type, Dictionary<string, string>> CompatDecLookupBacking;

        /// <summary>
        /// A factory function that can be used to provide custom converters.
        /// </summary>
        /// <remarks>
        /// This is a tool of last resort; in most cases you should just be inheriting from ConverterString'1 et al. This is intended for converters from non-public classes, which can be access through (ab)use of reflection.
        /// </remarks>
        public static Func<Type, Converter> ConverterFactory;

        /// <summary>
        /// Used for unit tests. Not recommended or supported for actual code.
        /// </summary>
        public class UnitTestParameters
        {
            public Type[] explicitTypes = null;
            public Type[] explicitStaticRefs = null;
            public Type[] explicitConverters = null;
        }
        internal static UnitTestParameters TestParameters = null;
        internal static bool TestRefEverything = false;

        static Config()
        {
            string ExceptionSuffix()
            {
                if (DefaultHandlerShowConfigOnException)
                {
                    return "\nIf you don't want errors to be critical exceptions, this can be modified in Dec.Config; change DefaultHandlerThrowExceptions to avoid exceptions, or replace WarningHandler, ErrorHandler, and ExceptionHandler with your own handlers. Doing this is STRONGLY RECOMMENDED. Dec can recover smoothly from most error cases, but it's still important that you see the error.\nIf you want the current behavior but don't want to see this message, set DefaultHandlerShowConfigOnException to false.";
                }
                else
                {
                    return "";
                }
            }

            #if UNITY_5_3_OR_NEWER
                InfoHandler = (str) =>
                {
                    UnityEngine.Debug.Log(str);
                };

                WarningHandler = (str) =>
                {
                    UnityEngine.Debug.LogWarning(str);
                    if (DefaultHandlerThrowExceptions >= DefaultExceptionBehavior.ErrorAndWarning)
                    {
                        throw new ArgumentException(str + ExceptionSuffix());
                    }
                };

                ErrorHandler = (str) =>
                {
                    UnityEngine.Debug.LogError(str);
                    if (DefaultHandlerThrowExceptions >= DefaultExceptionBehavior.ErrorOnly)
                    {
                        throw new ArgumentException(str + ExceptionSuffix());
                    }
                };

                ExceptionHandler = (e) =>
                {
                    UnityEngine.Debug.LogException(e);
                    throw e;
                };
            #elif GODOT
                InfoHandler = (str) =>
                {
                    Godot.GD.Print(str);
                };

                WarningHandler = (str) =>
                {
                    Godot.GD.PushWarning(str);
                    if (DefaultHandlerThrowExceptions >= DefaultExceptionBehavior.ErrorAndWarning)
                    {
                        throw new ArgumentException(str + ExceptionSuffix());
                    }
                };

                ErrorHandler = (str) =>
                {
                    Godot.GD.PushError(str);
                    if (DefaultHandlerThrowExceptions >= DefaultExceptionBehavior.ErrorOnly)
                    {
                        throw new ArgumentException(str + ExceptionSuffix());
                    }
                };

                ExceptionHandler = (e) =>
                {
                    Godot.GD.PushError(e.ToString());
                    if (DefaultHandlerThrowExceptions >= DefaultExceptionBehavior.ErrorOnly)
                    {
                        throw e;
                    }
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
                    if (DefaultHandlerThrowExceptions >= DefaultExceptionBehavior.ErrorOnly)
                    {
                        throw e;
                    }
                };
            #endif
        }
    }
}
