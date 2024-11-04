using System;

namespace Dec
{
    /// <summary>
    /// Base class of all dec-provided datatypes.
    /// </summary>
    /// <remarks>
    /// All decs should inherit from this.
    /// </remarks>
    [Abstract]
    public abstract class Dec
    {
        /// <summary>
        /// Unique string identifier.
        /// </summary>
        public string DecName { get; internal set; }

        public override string ToString()
        {
            // This should probably be getting some kind of dec-namespace-aware string value.
            return $"[{GetType().Name}:{DecName}]";
        }

        /// <summary>
        /// Overrideable function to report configuration errors.
        /// </summary>
        /// <remarks>
        /// StaticReferences will be initialized before this function is called. This function may be called in parallel across your decs, in any order.
        /// </remarks>
        public virtual void ConfigErrors(Action<string> reporter) { }

        /// <summary>
        /// Overrideable function to do post-load one-time setup tasks.
        /// </summary>
        /// <remarks>
        /// StaticReferences will be initialized before this function is called. This function will be called serially across your decs, but with undefined order.
        ///
        /// Error strings can be reported from this as well, and will be displayed in the same way as ConfigErrors()-reported errors.
        /// </remarks>
        public virtual void PostLoad(Action<string> reporter) { }
    }
}
