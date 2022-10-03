namespace Dec
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;

    internal static class UtilXml
    {
        internal static int LineNumber(this XElement element)
        {
            if (element is IXmlLineInfo lineinfo)
            {
                return lineinfo.LineNumber;
            }
            else
            {
                return 0;
            }
        }

        internal static XElement ElementNamed(this XElement root, string name)
        {
            return root.Elements().Where(child => child.Name.LocalName == name).SingleOrDefaultChecked();
        }

        internal static string GetText(this XElement element)
        {
            if (element == null)
            {
                return null;
            }
            return element.Nodes().OfType<XText>().FirstOrDefault()?.Value;
        }

        internal static string ConsumeAttribute(this XElement element, string stringName, string attributeName, ref HashSet<string> consumedAttributes)
        {
            var attribute = element.Attribute(attributeName);

            if (attribute == null)
            {
                return null;
            }
            else
            {
                if (consumedAttributes == null)
                {
                    consumedAttributes = new HashSet<string>();
                }

                if (consumedAttributes.Contains(attributeName))
                {
                    Dbg.Err($"{stringName}:{element.LineNumber()}: Attempted to consume the same attribute twice; internal error in Dec, please report!");
                }

                consumedAttributes.Add(attributeName);

                return attribute.Value;
            }
        }
    }
}
