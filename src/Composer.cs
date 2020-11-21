namespace Def
{
    using System.Xml.Linq;

    /// <summary>
    /// Handles writing def structures into files. Generally useful for in-game editors.
    /// </summary>
    /// <remarks>
    /// This class is under heavy development and its API is likely to be unstable and undocumented.
    /// </remarks>
    public class Composer
    {
        public string Compose()
        {
            var doc = new XDocument();

            var record = new XElement("Defs");
            doc.Add(record);

            var writerContext = new WriterXMLCompose();

            foreach (var defObj in Database.List)
            {
                var defXml = Serialization.ComposeElement(defObj, defObj.GetType(), defObj.GetType().ComposeDefFormatted(), writerContext, isRootDef: true);
                defXml.Add(new XAttribute("defName", defObj.DefName));
                record.Add(defXml);
            }

            writerContext.DequeuePendingWrites();

            return doc.ToString();
        }
    }
}
