using System;

namespace Loaf
{
    // Locations, in this game, are declared with subclasses of a class called LocationDec. This demonstrates a few different ways to create classes on demand.
    //
    // The DungeonDec subclass has information tied to that type, and creates an instance type hardcoded into the DungeonDec code.
    // This approach works well for objects that are heavily data-driven; in this project, we create three separate Dungeons, distinguished dolrlu by the monster list.
    //
    // The LocationTypeDec subclass has a Dec member which represents the type to create.
    // This approach works well for objects that are entirely code-driven, without a significant data component. We have four separate Typed locations, none of which require Dec data.
    // We *could* create four separate Dec classes inheriting from LocationDec, but that's a lot of extra syntax for not much gain.
    //
    // If you have an object that includes a lot of data and also custom code, I recommend just making a Dec subclass.
    // Having the data expressed in XML is generally the right approach for ease of moddability and designer tweaking, but there's no sense in reimplementing C# in XML.
    public abstract class LocationDec : Cns.ChoiceDec
    {
        public ItemDec requiredItem;

        public abstract Location Create();
    }

    // In this project, an entire Location class is honestly pretty silly - it will never be stored anywhere, it exists solely to call Visit on once and then get eaten by the GC.
    // But it's a good example for larger projects.
    public abstract class Location
    {
        [Dec.StaticReferences]
        public static class Outcomes
        {
            static Outcomes()
            {
                Dec.StaticReferencesAttribute.Initialized();
            }

            public static OutcomeDec Return;
            public static OutcomeDec Death;
            public static OutcomeDec Victory;
        }
        public class OutcomeDec : Dec.Dec { }

        public abstract OutcomeDec Visit();
    }

    // Recommend reading the LocationDec documentation for more information.
    //
    // When creating a builder like this, I recommend passing the Dec in as a constructor parameter.
    // The constructor can always ignore it, but if there's any relevant data, the constructor will need access to the Dec.
    public class LocationTypedDec : LocationDec
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