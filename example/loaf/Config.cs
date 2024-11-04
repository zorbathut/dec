using System.Collections.Generic;

namespace Loaf
{
    // In many situations I've found it useful to have a ConfigDec class with a bunch of global parameters.
    // This is simply making use of the Dec system to provide a centralized settings registry.
    // It's overkill, but it's effective.
    public class ConfigDec : Dec.Dec
    {
        public int baud;
        public float crlfDelay;

        public bool suppressDelay = false;

        public int playerHp;
        public List<ItemDec> startingItems;
        public string saveFilename;

        public bool alternateEnding = false;
    }

    // Using a StaticReferences for the global ConfigDec means you can access it from anywhere via Config.Global.fieldName.
    [Dec.StaticReferences]
    public static class Config
    {
        static Config()
        {
            Dec.StaticReferencesAttribute.Initialized();
        }

        public static ConfigDec Global;
    }
}
