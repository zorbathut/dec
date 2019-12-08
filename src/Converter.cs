namespace Def
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// Base class for converting to arbitrary types.
    /// </summary>
    /// <remarks>
    /// This is a standalone class to allow implementation of converters of third-party types.
    ///
    /// Inherit from this, fill out an appropriate HandledTypes() function, then implement at least one of FromString() or FromXml().
    /// </remarks>
    public abstract class Converter
    {
        /// <summary>
        /// Returns a set of types that it can convert to and from.
        /// </summary>
        /// <remarks>
        /// When deserializing, conversion functions are not called for subclasses; that is, if a BaseClass is requested, only a converter that promises to return a BaseClass will be called.
        /// 
        /// It is an error if any two Conversion-derived non-abstract classes report that they can generate the same type.
        ///
        /// This behavior may change someday; please read patch notes if you're relying on it.
        /// </remarks>
        public abstract HashSet<Type> HandledTypes();

        /// <summary>
        /// Converts a string to a suitable object type.
        /// </summary>
        /// <remarks>
        /// `type` is set to the expected return type; you can return null, or anything that can be implicitly converted to that type.
        /// 
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
        /// `type` is set to the expected return type; you can return null, or anything that can be implicitly converted to that type.
        /// 
        /// In case of error, call Def.Dbg.Err with some appropriately useful message and return null. Message should be formatted as $"{inputName}:{(input as IXmlLineInfo).LineNumber}: Something went wrong".
        /// </remarks>
        public virtual object FromXml(XElement input, Type type, string inputName)
        {
            Dbg.Err($"{inputName}:{input.LineNumber()}: Failed to parse XML when attempting to parse {type}");
            return null;
        }

        public virtual string ToString(object input)
        {
            Dbg.Err($"Failed to generate a string when attempting to record {input.GetType()}");
            return null;
        }

        public virtual bool ToXml(object input, XElement context)
        {
            context.Add(new XText(ToString(input)));
            return true;
        }

        public virtual object Record(object model, Type type, Recorder recorder)
        {
            switch (recorder.Mode)
            {
                case Recorder.Direction.Read:
                {
                    var sourceName = (recorder as RecorderReader).SourceName;
                    var element = recorder.Xml;

                    bool hasElements = element.Elements().Any();
                    bool hasText = element.Nodes().OfType<XText>().Any();

                    if (hasElements && hasText)
                    {
                        Dbg.Err($"{sourceName}:{element.LineNumber()}: Elements and text are not valid together with a non-Record Converter");
                    }

                    if (hasElements)
                    {
                        return FromXml(recorder.Xml, type, sourceName);
                    }
                    else
                    {
                        return FromString(element.GetText() ?? "", type, sourceName, element.LineNumber());
                    }
                }    

                case Recorder.Direction.Write:
                    ToXml(model, recorder.Xml);
                    return model;

                default:
                    Dbg.Err($"Recorder is somehow in mode {recorder.Mode} which is not valid");
                    return model;
            }
        }
    }
}
