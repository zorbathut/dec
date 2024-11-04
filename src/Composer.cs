using System.Xml.Linq;

namespace Dec
{
    /// <summary>
    /// Handles writing dec structures into files. Generally useful for in-game editors.
    /// </summary>
    /// <remarks>
    /// This class is under heavy development and its API is likely to be unstable and undocumented.
    /// </remarks>
    public class Composer
    {
        public string ComposeXml(bool pretty, Recorder.IUserSettings userSettings = null)
        {
            using (var _ = new CultureInfoScope(Config.CultureInfo))
            {
                var writerContext = new WriterXmlCompose(userSettings);

                foreach (var decObj in Database.List)
                {
                    Serialization.ComposeElement(writerContext.StartDec(decObj.GetType(), decObj.DecName), decObj, decObj.GetType(), isRootDec: true);
                }

                return writerContext.Finish(pretty);
            }
        }

        public string ComposeValidation(Recorder.IUserSettings userSettings = null)
        {
            using (var _ = new CultureInfoScope(Config.CultureInfo))
            {
                var writerContext = new WriterValidationCompose(userSettings);

                foreach (var decObj in Database.List)
                {
                    Serialization.ComposeElement(writerContext.StartDec(decObj.GetType(), decObj.DecName), decObj, decObj.GetType(), isRootDec: true);
                }

                return writerContext.Finish();
            }
        }
    }
}
