namespace Def
{
    public class Dbg
    {
        public void Inf(string format, params object[] args)
        {
            Config.InfoHandler(string.Format(format, args));
        }

        public void Wrn(string format, params object[] args)
        {
            Config.WarningHandler(string.Format(format, args));
        }

        public void Err(string format, params object[] args)
        {
            Config.ErrorHandler(string.Format(format, args));
        }
    }
}