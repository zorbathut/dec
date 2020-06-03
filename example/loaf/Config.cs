namespace Loaf
{
    using System.Collections.Generic;

    // In many situations I've found it useful to have a ConfigDef class with a bunch of global parameters.
    // This is simply making use of the Def system to provide a centralized settings registry.
    // It's overkill, but it's effective.
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

    // Using a StaticReferences for the global ConfigDef means you can access it from anywhere via Config.Global.fieldName.
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
