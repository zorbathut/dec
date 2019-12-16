namespace Def
{
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

        internal static string ConsumeAttribute(this XElement element, string attributeName)
        {
            var attribute = element.Attribute(attributeName);

            if (attribute == null)
            {
                return null;
            }
            else
            {
                string result = attribute.Value;
                attribute.Remove();
                return result;
            }
        }
    }
}
