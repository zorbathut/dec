namespace Def
{
    using System.Xml.Linq;

    /// <summary>
    /// Handles writing def structures into files. Generally useful for in-game editors.
    /// </summary>
    /// <remarks>
    /// This class is under heavy development and its API is likely to be unstable and undocumented.
    /// </remarks>
    public class Writer
    {
        public string Write()
        {
            var doc = new XDocument();

            var record = new XElement("Defs");
            doc.Add(record);

            // We're not really storing anything long-term here, we're just 
            var writerContext = new WriterContext(false);

            foreach (var defObj in Database.List)
            {
                // TODO: need a better string identifier; this should be basically the opposite of ParseTypeDefFormatted
                var defXml = Serialization.ComposeElement(defObj, defObj.GetType(), defObj.GetType().Name, writerContext, isRootDef: true);
                defXml.Add(new XAttribute("defName", defObj.DefName));
                record.Add(defXml);
            }

            writerContext.DequeuePendingWrites();

            return doc.ToString();
        }
    }
}
