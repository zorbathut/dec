namespace Loaf
{
    public class ConfigDef : Def.Def
    {
        public int baud;
        public float crlfDelay;
    }

    [Def.StaticReferences]
    public static class Config
    {
        static Config()
        {
            Def.StaticReferencesAttribute.Initialized();
        }

        public static ConfigDef Global;
    }
}
