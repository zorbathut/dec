namespace Def
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Base class of all def-provided datatypes.
    /// </summary>
    /// <remarks>
    /// All defs should inherit from this.
    /// </remarks>
    [Abstract]
    public abstract class Def
    {
        /// <summary>
        /// Unique string identifier.
        /// </summary>
        public string DefName { get; internal set; }

        public override string ToString()
        {
            return DefName;
        }

        /// <summary>
        /// Overrideable function to report configuration errors.
        /// </summary>
        /// <remarks>
        /// StaticReferences will be initialized before this function is called. This function may be called in parallel across your defs, in any order.
        /// </remarks>
        public virtual void ConfigErrors(Action<string> reporter) { }

        /// <summary>
        /// Overrideable function to do post-load one-time setup tasks.
        /// </summary>
        /// <remarks>
        /// StaticReferences will be initialized before this function is called. This function will be called serially across your defs, but with undefined order.
        ///
        /// Error strings can be reported from this as well, and will be displayed in the same way as ConfigErrors()-reported errors.
        /// </remarks>
        public virtual void PostLoad(Action<string> reporter) { }
    }
}
