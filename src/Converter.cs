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
    /// Inherit from this, fill out an appropriate HandledTypes() function, then implement at least one of FromString(), FromXml(), or Record().
    ///
    /// If you want to be able to write things with Recorder, you'll need to implement at least one of ToString(), ToXml(), or Record().
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

            string text = input.GetText();
            if (text != null)
            {
                // try to fall back to string?
                return FromString(text, type, inputName, input.LineNumber());
            }

            // oh well
            return null;
        }

        /// <summary>
        /// Converts an object to a string.
        /// </summary>
        /// <remarks>
        /// `input` will be one of the types provided in HandledTypes(); it will not be null. Whatever you return should be convertable back to an object by an overridden FromString().
        ///
        /// In case of error, call Def.Dbg.Err with some appropriately useful message and return null.
        /// </remarks>
        public virtual string ToString(object input)
        {
            Dbg.Err($"Failed to generate a string when attempting to record {input.GetType()}");
            return null;
        }

        /// <summary>
        /// Converts an object to XML.
        /// </summary>
        /// <remarks>
        /// `input` will be one of the types provided in HandledTypes(); it will not be null. Whatever you return should be convertable back to an object by an overridden FromXml().
        ///
        /// If you need to create any attributes, you should prefix them with a unique identifier so they don't conflict with standard def attributes.
        ///
        /// If you add a single XText child, parsing may be done with FromString() instead of FromXml().
        /// 
        /// In case of error, call Def.Dbg.Err with some appropriately useful message and don't modify context.
        /// </remarks>
        public virtual void ToXml(object input, XElement context)
        {
            // If we don't get anything valid from ToString(), just don't bother to add a node
            var str = ToString(input);
            if (str != null)
            {
                context.Add(new XText(str));
            }
        }

        /// <summary>
        /// Handles serialization and deserialization with support for references.
        /// </summary>
        /// <remarks>
        /// In read mode, `model` will be one of the types provided in HandledTypes(); it will not be null. Use recorder or recorder.Xml to serialize its contents, then return model.
        ///
        /// In write mode, `model` will be one of the types provided in HandledTypes(), or null. If it's null, create an object of an appropriate type. Use recorder or recorder.Xml to fill it, then return it.
        ///
        /// `type` indicates the type that the underlying code expects to get back. The object you return must be assignable to that type.
        /// 
        /// In case of error, call Def.Dbg.Err with some appropriately useful message, then return model.
        ///
        /// In most cases, using Recorder's interface is by far the easiest way to support the requirements of this function. It is expected that you use XML only when absolutely necessary.
        /// </remarks>
        /// <example>
        /// <code>
        ///     public override object Record(object model, Type type, Recorder recorder)
        ///     {
        ///         // If we're in read mode, this leaves model untouched. If we're in write mode, this leaves model untouched unless it's null.
        ///         model = model ?? new ConvertedClass();
        ///
        ///         // The Recorder interface figures out the right thing based on context.
        ///         // Any members that are referenced elsewhere will be turned into refs automatically.
        ///         recorder.Record(ref model.integerMember, "integerMember");
        ///         recorder.Record(ref model.classMember, "classMember");
        ///         recorder.Record(ref model.structMember, "structMember");
        ///         recorder.Record(ref model.collectionMember, "collectionMember");
        ///
        ///         return model;
        ///     }
        /// </code>
        /// </example>
        public virtual object Record(object model, Type type, Recorder recorder)
        {
            switch (recorder.Mode)
            {
                case Recorder.Direction.Read:
                {
                    var sourceName = (recorder as RecorderReader).SourceName;
                    var element = recorder.Xml;

                    if (element.Elements().Any())
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
                    // what have you done
                    // *what have you done*
                    Dbg.Err($"Recorder is somehow in mode {recorder.Mode} which is not valid");
                    return model;
            }
        }
    }
}
