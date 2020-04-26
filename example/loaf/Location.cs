
namespace Loaf
{
    using System;

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

    public abstract class LocationDef : Cns.ChoiceDef
    {
        public abstract Location Create();
    }

    public class LocationTypedDef : LocationDef
    {
        private Type type;

        public override Location Create()
        {
            return (Location)Activator.CreateInstance(type, this);
        }

        public override void ConfigErrors(Action<string> report)
        {
            if (type == null)
            {
                report($"type {type} is null");
            }
            else if (!typeof(Location).IsAssignableFrom(type))
            {
                report($"type {type} needs to inherit from Location");
            }
        }
    }
}