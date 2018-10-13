namespace Def
{
    using System.Collections.Generic;

    /// <summary>
    /// Base class of all def-provided datatypes.
    /// </summary>
    /// <remarks>
    /// All defs should inherit from this.
    /// </remarks>
    public class Def
    {
        /// <summary>
        /// Unique string identifier.
        /// </summary>
        public string defName;

        /// <summary>
        /// Unique integer identifier.
        /// </summary>
        /// <remarks>
        /// Guaranteed to be in the range [0, DefDatabase&lt;T&gt;.Count). Will not change at runtime, but not guaranteed to be stable between processes. Do not use as a serialization key.
        /// </remarks>
        public int index;

        public override string ToString()
        {
            return defName;
        }

        /// <summary>
        /// Overrideable function to return configuration errors.
        /// </summary>
        /// <remarks>
        /// StaticReferences will be initialized before this function is called. This function may be called in parallel across your defs, in any order.
        /// </remarks>
        public virtual IEnumerable<string> ConfigErrors()
        {
            yield break;
        }

        /// <summary>
        /// Overrideable function to do post-load one-time setup tasks.
        /// </summary>
        /// <remarks>
        /// StaticReferences will be initialized before this function is called. This function will be called serially across your defs, but with undefined order.
        ///
        /// Error strings can be returned from this as well, and will be reported in the same way as ConfigErrors()-reported errors.
        /// </remarks>
        public virtual IEnumerable<string> PostLoad()
        {
            yield break;
        }
    }
}
