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

        internal static XElement ElementNamed(this XElement root, string name, Context context)
        {
            // this is basically SingleOrDefaultChecked with an inline error message, which proved difficult to get the right way
            // A plain loop rather than a Where, which would add a closure and a second iterator on every field a Record() reads.
            XElement result = null;

            foreach (var element in root.Elements())
            {
                if (element.Name.LocalName != name)
                {
                    continue;
                }

                if (result == null)
                {
                    result = element;
                }
                else
                {
                    Dbg.Err($"{context}: Multiple items named `{name}` found when only one is expected");

                    // no point in continuing
                    break;
                }
            }

            return result;
        }

        internal static XElement ElementNamedWithFallback(this XElement root, string name, Context context, string errorPrefix)
        {
            var result = ElementNamed(root, name, context);
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

        // Checks every text node, not just the first: a comment can split indentation from real text.
        internal static bool HasNonWhitespaceText(this XElement element)
        {
            foreach (var node in element.Nodes())
            {
                if (node is XText text && !string.IsNullOrWhiteSpace(text.Value))
                {
                    return true;
                }
            }

            return false;
        }

        internal static XDocument ParseSafely(System.IO.TextReader input, string identifier)
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreWhitespace = false,
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
                        Dbg.Err($"{identifier}: Malformed XML: {e.Message}");
                        return null;
                    }
                }
            }
        }
    }
}
