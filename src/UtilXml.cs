using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Dec
{
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

        internal static XElement ElementNamedWithFallback(this XElement root, string name, InputContext context, string errorPrefix)
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
                Dbg.Err($"{context}: {errorPrefix}; falling back on `{result.Name.LocalName}`, which is not the right case!");
                return result;
            }

            if (root.Elements().Any())
            {
                Dbg.Err($"{context}: {errorPrefix}; options include [{string.Join(", ", root.Elements().Select(child => child.Name.LocalName))}]");
                return null;
            }
            else
            {
                Dbg.Err($"{context}: {errorPrefix}; no elements to use");
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

        internal static XDocument ParseSafely(System.IO.TextReader input)
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreWhitespace = true,
            };

            using (var reader = XmlReader.Create(input, settings))
            {
                // this may be a second layer of culture info scoping, but I'll live with it for now
                using (var _ = new CultureInfoScope(Config.CultureInfo))
                {
                    try
                    {
                        return XDocument.Load(reader, LoadOptions.SetLineInfo);
                    }
                    catch (System.Xml.XmlException e)
                    {
                        Dbg.Ex(e);
                        return null;
                    }
                }
            }
        }
    }
}
