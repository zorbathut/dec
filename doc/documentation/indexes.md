# Indexes

Sometimes it's useful to have integer IDs for a class of objects. This is *usually* helpful for performance reasons in tight loops; if you're thinking about using this for anything besides critical performance or limited data storage options such as Unity's DOTS, I recommend rethinking. Dec supports indexes both for Decs themselves, and for members within the Dec system. These indexes are guaranteed to start at 0 and be contiguous, making them suitable for array lookups.

Make an `int` member with the Dec.Index attribute. All instances of that object, whether it be a Dec, a non-Dec class, or a struct, will be given a unique integer index. The full list will also be stored in Index&lt;T&gt;.

```cs
// Here we have a Monster.

// In this game, every monster has a chance of a Blessing applied every frame.
// Calculating the chances for this is done entirely through code, but it's very expensive,
// so we want to calculate it at the beginning of the game.

// The Blessing depends on the zone, so we need to, given a Monster and a Blessing,
// return the chance-of-blessing.

// We could use a `Dictionary<BlessingDec, float>` for this,
// but because we're checking this potentially every frame, on a lot of monsters,
// we want to avoid Dictionary overhead if possible.
public class MonsterDec : Dec.Dec
{
    // [Other monster-related parameters here]

    [NonSerialized] public float[] blessingChance;

    public void PostLoad(Action<string> reporter)
    {
        blessingChance = new float[Index<BlessingDec>.Count];
        for (int i = 0; i < Index<BlessingDec>.Count; ++i)
        {
            // This is an array from BlessingDec's index to the calculated Blessing Chance.
            // Array is faster than Dictionary, which is our goal.
            blessingChance[i] = CalculateBlessingChance(this, Index<BlessingDec>.Get(i));
        }
    }
}

public class BlessingDec : Dec.Dec
{
    [Dec.Index] public int index;
}

public float FindBlessingChance(MonsterDec monster, BlessingDec blessing)
{
    // Two member lookups and an array lookup. Nice and speedy!
    return monster.blessingChance[blessing.index];
}
```

These values have no defined order and are *not* consistent between execution runs - don't use them as part of serializing savegames!

You're limited to one Index per type, but you can have multiple indexes at different parts in a class hierarchy.

```cs
// This class still has `.index` which refers to its position in BlessingDecs.
// It also has `.darkIndex` which is a separate independent index for DarkBlessingDecs.

// Both of these have the same guarantees - start at 0, contiguous -
// and are not guaranteed to store the same number.
public class DarkBlessingDec : BlessingDec
{
    [Dec.Index] public int darkIndex;
}
```

You can also put Indexes on objects that aren't even Decs.

```cs
public class Component
{
    [Dec.Index] public int index;
}

public class EntityDec : Dec.Dec
{
    public List<Component> components;
}
```

```xml
<EntityDec decName="Chair">
    <components>
        <li class="HealthComponent" />
    </components>
</EntityDec>

<EntityDec decName="ExplodingBarrel">
    <components>
        <li class="HealthComponent" />
        <li class="DetonationComponent" />
    </components>
</EntityDec>
```

This setup has three separate Components, and will give them the indexes 0 through 2. Indexes don't care which Decs things are contained in, they're a global unique identifier.

Indexes are currently updated *only* on things created through Dec input files; Dec does not attempt to keep realtime track of objects with Indexes, so if you're creating Indexed objects on the fly, that's your problem.

Warning: Indexes may not be set properly on objects that are created as defaults and that are never explicitly defined in decs. This may be solved in the future; consider this a first revision of this feature. Example:

```cs
public class IndexedItem
{
    [Dec.Index] public int index;
}

public class CarrierDec : Dec.Dec
{
    public IndexedItem item = new IndexedItem();
}
```

```xml
<CarrierDec decName="Valid">
    <item />
</CarrierDec>

<CarrierDec decName="NotValid">
    
</CarrierDec>
```

`Valid`'s IndexedItem will have its index set to a valid number. `NotValid`'s IndexedItem is never traversed by Dec and may have any index, including something completely invalid, or even the same value as Valid's IndexedItem.

This is not an ideal behavior, but it's hard to fix and I haven't gotten there yet. If this is an actual problem for you, please pester me on Discord! I have a fix in mind.