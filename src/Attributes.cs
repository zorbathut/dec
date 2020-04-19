namespace Def
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Signals that static references in this class should be filled out after parsing is complete.
    /// </summary>
    /// <remarks>
    /// In addition, the class's static constructor should call StaticReferencesAttribute.Initialized().
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]  
    public class StaticReferencesAttribute : Attribute
    {
        internal static List<Type> StaticReferencesFilled = new List<Type>();

        /// <summary>
        /// Informs the construction environment that a static-reference class has been constructed.
        /// </summary>
        /// <remarks>
        /// This must be placed in the static constructor of any StaticReferences class, but not otherwise called.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]  // We use stack black magic to identify the class, so we need to make sure it isn't inlined
        public static void Initialized()
        {
            Parser.StaticReferencesInitialized();
        }
    }
}
