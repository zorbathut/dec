
namespace Loaf
{
    using System;

    // Locations, in this game, are defined with subclasses of a class called LocationDef. This demonstrates a few different ways to create classes on demand.
    //
    // The DungeonDef subclass has information tied to that type, and creates an instance type hardcoded into the DungeonDef code.
    // This approach works well for objects that are heavily data-driven; in this project, we create three separate Dungeons, distinguished dolrlu by the monster list.
    //
    // The LocationTypeDef subclass has a Def member which represents the type to create.
    // This approach works well for objects that are entirely code-driven, without a significant data component. We have four separate Typed locations, none of which require Def data.
    // We *could* create four separate Def classes inheriting from LocationDef, but that's a lot of extra syntax for not much gain.
    //
    // If you have an object that includes a lot of data and also custom code, I recommend just making a Def subclass.
    // Having the data expressed in XML is generally the right approach for ease of moddability and designer tweaking, but there's no sense in reimplementing C# in XML.
    public abstract class LocationDef : Cns.ChoiceDef
    {
        public ItemDef requiredItem;

        public abstract Location Create();
    }

    // In this project, an entire Location class is honestly pretty silly - it will never be stored anywhere, it exists solely to call Visit on once and then get eaten by the GC.
    // But it's a good example for larger projects.
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
            public static OutcomeDef Victory;
        }
        public class OutcomeDef : Def.Def { }

        public abstract OutcomeDef Visit();
    }

    // Recommend reading the LocationDef documentation for more information.
    //
    // When creating a builder like this, I recommend passing the Def in as a constructor parameter.
    // The constructor can always ignore it, but if there's any relevant data, the constructor will need access to the Def.
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