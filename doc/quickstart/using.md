# Using Defs from your code

## Enumeration

The first and least common way to use Defs is via [`Def.Database<T>.List`](xref:Def.Database`1.List). This lists all Defs of a given type, which is useful if your Def is a list of things that sensibly needs to be traversed (keybinds, game difficulties, monsters when trying to display a Bestiary).

## Lookup by name

In most cases, you don't want *all* Defs, you want a *specific* Def. A good way to do this is with static references. Create the following class somewhere in your code:

```cs
// The attribute lets def know that it should fill this static class with data.
[Def.StaticReferences]
public static class MonsterDefs
{
  // This bit of copy-pasted code is required to automatically detect some errors.
  // def will also detect if you haven't included this code and warn you about it.
  static MonsterDefs() { Def.StaticReferencesAttribute.Initialized(); }

  // Include static members for any defs you want to reference directly.
  public static MonsterDef Goblin;
  public static MonsterDef DarkGoblin;
  
  // You don't have to include all defs here - in fact, it's recommended that you only include defs that you plan to reference by name.
}
```

Now you can reference `MonsterDefs.Goblin` directly, with no more post-startup performance cost than checking a static member of a class. In addition, Def will verify on startup that all referenced Defs exist.

You can also search Defs by name with [`Def.Database<T>.Get()`](xref:Def.Database`1.Get*) **but this is not recommended** because errors will be detected only at runtime, not startup-time, and it's significantly slower than a static member lookup.

## References

The most common way of using Defs is as a member of another Def. For example:

```cs
class EnemyDef : Def.Def
{
    public WeaponDef weapon;
}
```

The Def library will fill these in based on your XML and you can just use them as a normal member.

## Debug Functionality

If you really need a list of *every* Def, of *every* type, you can use [`Def.Database.List`](xref:Def.Database.List), but this is slow and very uncommon outside of debug code.