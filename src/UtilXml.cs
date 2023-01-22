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

        internal static XElement ElementNamedWithFallback(this XElement root, string name, string inputLine, int lineNumber, string errorPrefix)
        {
            var result = ElementNamed(root, name);
            if (result != null)
            {
                // yay
                return result;
            }

            // check to see if we have a case-insensitive match
            result = root.Elements().Where(child => string.Compare(child.Name.LocalName, name, true) == 0).FirstOrDefault();
            if (result != null)
            {
                Dbg.Err($"{inputLine}:{lineNumber}: {errorPrefix}; falling back on `{result.Name.LocalName}`, which is not the right case!");
                return result;
            }

            if (root.Elements().Any())
            {
                Dbg.Err($"{inputLine}:{lineNumber}: {errorPrefix}; options include [{string.Join(", ", root.Elements().Select(child => child.Name.LocalName))}]");
                return null;
            }
            else
            {
                Dbg.Err($"{inputLine}:{lineNumber}: {errorPrefix}; no elements to use");
                return null;
            }
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
