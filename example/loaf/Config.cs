namespace Loaf
{
    using System.Collections.Generic;

    public class ConfigDef : Def.Def
    {
        public int baud;
        public float crlfDelay;

        public bool suppressDelay = false;

        public int playerHp;
        public List<ItemDef> startingItems;
        public string saveFilename;

        public bool alternateEnding = false;
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
