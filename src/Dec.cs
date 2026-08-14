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
        /// Deprecated; prefer a [Dec.Setup] function, which is the same thing with explicit ordering control.
        ///
        /// StaticReferences will be initialized before this function is called. This runs as a node in the setup graph alongside [Dec.Setup] functions; within a type, all ConfigErrors calls happen before any PostLoad calls, in DecName order. Cross-type order is undefined unless constrained with [Dec.SetupAfter]/[Dec.SetupBefore].
        /// </remarks>
        [Obsolete("ConfigErrors is deprecated; use a [Dec.Setup] function instead. Overrides still run as part of the setup graph.")]
        public virtual void ConfigErrors(Action<string> reporter) { }

        /// <summary>
        /// Overrideable function to do post-load one-time setup tasks.
        /// </summary>
        /// <remarks>
        /// Deprecated; prefer a [Dec.Setup] function, which is the same thing with explicit ordering control.
        ///
        /// StaticReferences will be initialized before this function is called. This runs as a node in the setup graph alongside [Dec.Setup] functions, serially across your decs in DecName order, after the same type's ConfigErrors. Cross-type order is undefined unless constrained with [Dec.SetupAfter]/[Dec.SetupBefore].
        ///
        /// Error strings can be reported from this as well, and will be displayed in the same way as ConfigErrors()-reported errors.
        /// </remarks>
        [Obsolete("PostLoad is deprecated; use a [Dec.Setup] function instead. Overrides still run as part of the setup graph.")]
        public virtual void PostLoad(Action<string> reporter) { }
    }
}
