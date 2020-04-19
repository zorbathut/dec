
namespace Loaf
{
    using System.IO;

    public abstract class LocationDef : Cns.ChoiceDef
    {
        public abstract Location Create();
    }

    public abstract class Location
    {
        [Def.StaticReferences]
        public static class Outcomes
        {
            static Outcomes()
            {
                Def.StaticReferencesAttribute.Initialized();
            }

            public static OutcomeDef Return;
            public static OutcomeDef Death;
        }
        public class OutcomeDef : Def.Def { }

        public abstract OutcomeDef Visit();
    }
}