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
            var writerContext = new WriterXmlCompose();

            foreach (var defObj in Database.List)
            {
                Serialization.ComposeElement(defObj, defObj.GetType(), writerContext.StartDef(defObj.GetType(), defObj.DefName), isRootDef: true);
            }

            return writerContext.Finish();
        }
    }
}
