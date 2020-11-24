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
        public string ComposeXml(bool pretty)
        {
            var writerContext = new WriterXmlCompose();

            foreach (var defObj in Database.List)
            {
                Serialization.ComposeElement(writerContext.StartDef(defObj.GetType(), defObj.DefName), defObj, defObj.GetType(), isRootDef: true);
            }

            return writerContext.Finish(pretty);
        }

        public string ComposeValidation()
        {
            var writerContext = new WriterValidationCompose();

            foreach (var defObj in Database.List)
            {
                Serialization.ComposeElement(writerContext.StartDef(defObj.GetType(), defObj.DefName), defObj, defObj.GetType(), isRootDef: true);
            }

            return writerContext.Finish();
        }
    }
}
