namespace Def
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;

    /// <summary>
    /// Base class for converting to arbitrary types.
    /// </summary>
    /// <remarks>
    /// This is a standalone class to allow implementation of converters of third-party types.
    ///
    /// Inherit from this, fill out an appropriate GeneratedTypes() function, then implement at least one of FromString() or FromXml().
    /// </remarks>
    public abstract class Converter
    {
        /// <summary>
        /// Returns a set of types that it can convert to.
        /// </summary>
        /// <remarks>
        /// Conversion functions are not called for subclasses; that is, if a BaseClass is requested, only a converter that promises to return a BaseClass will be called.
        /// 
        /// It is an error if any two Conversion-derived non-abstract classes report that they can generate the same type.
        ///
        /// This behavior may change someday; please read patch notes if you're relying on it.
        /// </remarks>
        public abstract HashSet<Type> GeneratedTypes();

        /// <summary>
        /// Converts a string to a suitable object type.
        /// </summary>
        /// <remarks>
        /// In case of error, call Def.Dbg.Err with some appropriately useful message and return null. Message should be formatted as $"{inputName}:{lineNumber}: Something went wrong".
        ///
        /// Note: In the case of empty input (i.e. &lt;member&gt;&lt;/member&gt; or &lt;member /&gt;) this function will be called.
        /// </remarks>
        public virtual object FromString(string input, Type type, string inputName, int lineNumber)
        {
            Dbg.Err($"{inputName}:{lineNumber}: Failed to parse string when attempting to parse {type}");
            return null;
        }

        /// <summary>
        /// Converts an XML element to a suitable object type.
        /// </summary>
        /// <remarks>
        /// In case of error, call Def.Dbg.Err with some appropriately useful message and return null. Message should be formatted as $"{inputName}:{(input as IXmlLineInfo).LineNumber}: Something went wrong".
        ///
        /// In case of error, call Def.Dbg.Err with some appropriately useful message and return null.
        /// </remarks>
        public virtual object FromXml(XElement input, Type type, string inputName)
        {
            Dbg.Err($"{inputName}:{input.LineNumber()}: Failed to parse XML when attempting to parse {type}");
            return null;
        }
    }
}
