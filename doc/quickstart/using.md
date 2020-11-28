# Using Decs from your code

## Enumeration

The first and least common way to use Decs is via [`Dec.Database<T>.List`](xref:Dec.Database`1.List). This lists all Decs of a given type, which is useful if your Dec is a list of things that sensibly needs to be traversed (keybinds, game difficulties, monsters when trying to display a Bestiary).

## Lookup by name

In most cases, you don't want *all* Decs, you want a *specific* Dec. A good way to do this is with static references. Create the following class somewhere in your code:

```cs
// The attribute lets dec know that it should fill this static class with data.
[Dec.StaticReferences]
public static class MonsterDecs
{
  // This bit of copy-pasted code is required to automatically detect some errors.
  // dec will also detect if you haven't included this code and warn you about it.
  static MonsterDecs() { Dec.StaticReferencesAttribute.Initialized(); }

  // Include static members for any decs you want to reference directly.
  public static MonsterDec Goblin;
  public static MonsterDec DarkGoblin;
  
  // You don't have to include all decs here - in fact, it's recommended that you only include decs that you plan to reference by name.
}
```

Now you can reference `MonsterDecs.Goblin` directly, with no more post-startup performance cost than checking a static member of a class. In addition, Dec will verify on startup that all referenced Decs exist.

You can also search Decs by name with [`Dec.Database<T>.Get()`](xref:Dec.Database`1.Get*) **but this is not recommended** because errors will be detected only at runtime, not startup-time, and it's significantly slower than a static member lookup.

## References

The most common way of using Decs is as a member of another Dec. For example:

```cs
class EnemyDec : Dec.Dec
{
    public WeaponDec weapon;
}
```

The Dec library will fill these in based on your XML and you can just use them as a normal member.

## Debug Functionality

If you really need a list of *every* Dec, of *every* type, you can use [`Dec.Database.List`](xref:Dec.Database.List), but this is slow and very uncommon outside of debug code.