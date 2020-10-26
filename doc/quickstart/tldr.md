# Too Long, Didn't Read

## Set up Def and add your XML directory.

```cs
void CalledOnceDuringGameStartup()
{
    Def.Config.DefaultHandlerThrowExceptions = false;
    Def.Config.UsingNamespaces = new string[] { "YourGame" };

    var parser = new Def.Parser();
    parser.AddDirectory("data");
    parser.Finish();
}
```

## Define some classes derived from [`Def.Def`](xref:Def.Def).

```cs
// Here, we're defining a general template for monsters.
// This goes somewhere appropriate in your C# game code.
class MonsterDef : Def.Def
{
  // Most types can be defined as simple fields.
  public float maxHP;
  
  // Fields can have default values.
  public float renderScale = 1;
  
  // Classes and structs can be defined inline.
  public Color tint = Color.White;
  
  // Defs can reference other defs.
  public SpriteSheetDef spriteSheet;
  
  // Def references are not limited to a tree structures.
  // Defs can reference other defs in circles, reference other defs of the same type,
  // and even reference themselves.
  public MonsterDef evolvedVariant = null;
}
```

## Write XML to define our actual objects.

```xml
<Defs>
  <!-- Here's a basic goblin. -->
  <!-- We've overridden the defaults we want changed and ignored the rest. -->
  <!-- Every def must be named via the defName attribute. -->
  <!-- These names must be unique within def type, but can overlap across different types. -->
  <!-- They are intended to be human-readable for debug and development purposes, -->
  <!-- but not user-visible. -->
  <!-- They must match the regexp [A-Za-z][A-Za-z0-9_]*. -->
  <MonsterDef defName="Goblin">
    <maxHP>20</maxHP>
    
    <!-- This connects MonsterDef.Goblin's spriteSheet to SpriteSheetDef.Goblin. -->
    <spriteSheet>Goblin</spriteSheet>
  </MonsterDef>

  <!-- This is a larger goblin. -->
  <!-- We've given it a new name, a larger render scale, and more health. -->
  <MonsterDef defName="MegaGoblin">
    <maxHP>100</maxHP>
    <renderScale>3</renderScale>
    <spriteSheet>Goblin</spriteSheet>
  </MonsterDef>

  <!-- Here, we've defined a new tint for the DarkGoblin. -->
  <!-- Modifying members of included classes works like you'd expect XML to. -->
  <MonsterDef defName="DarkGoblin">
    <maxHP>40</maxHP>
    <tint>
      <r>0.3</r>
      <g>0.3</g>
      <b>0.4</b>
    </tint>
    <spriteSheet>Goblin</spriteSheet>
  </MonsterDef>

  <!-- Referencing other defs is as simple as typing their defName. -->
  <!-- All def references are validated at load time. -->
  <MonsterDef defName="AlchemyGoblin">
    <maxHP>10</maxHP>
    <spriteSheet>Goblin</spriteSheet>
    <evolvedVariant>MegaGoblin</evolvedVariant>
  </MonsterDef>

  <!-- Defs can be referenced before they're defined. XML order is irrelevant. -->
  <SpriteSheetDef defName="Goblin">
    <filename>goblin.png</filename>
  </SpriteSheetDef>
</Defs>
```

## Define some static references.

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
  
  // You don't have to include all defs here - in fact,
  // it's recommended that you only include defs that you plan to reference by name.
}
```

## Use defs in code.

```cs
// This is an example of an actual monster.
// There's no requirement that it be named "Monster";
// def does not care what you do with the data it's generated.
public class Monster
{
  public Monster(MonsterDef def)
  {
    // It's common for objects to keep a reference to the def that they're created from.
    this.def = def;
    
    // defs are just classes; using things from them is simple.
    hp = def.maxHP;
  }
  
  private MonsterDef def;
  
  private float hp;
}

public class GoblinGenerator
{
  public Generate()
  {
    // If you need to use a static reference, just pull it out of the static reference class.
    // (We're assuming that you have an Entity system in this project already.
    // The details are up to you; def doesn't specify anything about entities.)
    Entity.Spawn(MonsterDefs.Goblin);
  }
}
```