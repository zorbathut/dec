# Quick Start

Define some classes derived from [`Def.Def`](xref:Def.Def).

```cs
// The starting point for all defs is a class derived from Def.Def.
// Here, we're defining a general template for monsters.
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
  
  // Def references are not limited to a tree structures. Defs can reference other defs in circles, reference other defs of the same type, and even reference themselves.
  public MonsterDef evolvedVariant = null;
}

// Classes that aren't derived from Def.Def will be recursively parsed.
// There's nothing magic about either these classes or `Def.Def`-derived classes. You can have functions, static members, or anything else you'd normally want in a class.
class Color
{
  public Color() { }
  public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; }
  
  public readonly static Color White = new Color(1, 1, 1);
  
  public float r = 1;
  public float g = 1;
  public float b = 1;
}

// Don't hesitate to make simple defs. Defs are lightweight and it's easy to add to them later.
class SpriteSheetDef : Def.Def
{
  public string filename;
}
```

Write XML to define the actual things.

```xml
<!-- Here's a basic goblin. We've overridden the defaults we want changed and ignored the rest. -->
<!-- Every def must be named via the defName attribute. These names must be unique within def type, but can overlap across different types. They are intended to be human-readable for debug and development purposes, but not user-visible. They must match the regexp [A-Za-z][A-Za-z0-9_]*. -->
<MonsterDef defName="Goblin">
  <maxHP>20</maxHP>
  
  <!-- This connects MonsterDef.Goblin's spriteSheet to SpriteSheetDef.Goblin. -->
  <spriteSheet>Goblin</spriteSheet>
</MonsterDef>

<!-- This is a larger goblin. We've given it a new name, a larger render scale, and more health. -->
<MonsterDef defName="MegaGoblin">
  <maxHP>100</maxHP>
  <renderScale>3</renderScale>
  <spriteSheet>Goblin</spriteSheet>
</MonsterDef>

<!-- Here, we've defined a new tint for the DarkGoblin. Modifying members of included classes works like you'd expect XML to. -->
<MonsterDef defName="DarkGoblin">
  <maxHP>40</maxHP>
  <tint>
    <r>0.3</r>
    <g>0.3</g>
    <b>0.4</b>
  </tint>
  <spriteSheet>Goblin</spriteSheet>
</MonsterDef>

<!-- Referencing other defs is as simple as typing their defName. All def references are validated at load time. -->
<MonsterDef defName="AlchemyGoblin">
  <maxHP>10</maxHP>
  <spriteSheet>Goblin</spriteSheet>
  <evolvedVariant>MegaGoblin</evolvedVariant>
</MonsterDef>

<!-- Defs can be referenced before they're defined. XML order is irrelevant. -->
<SpriteSheetDef defName="Goblin">
  <filename>goblin.png</filename>
</SpriteSheetDef>
```

Initialize the def framework via [`Parser`](xref:Def.Parser).

```cs
// The Parser class handles all def initialization.
var parser = new Parser();

// Add individual XML documents through .AddString().
// The second parameter is a file identifier to use for debugging. Use whatever is most convenient for you.
// It's common to read from on-disk files, but you can get the data from any source you want.
parser.AddString(File.ReadAllText("goblinData.xml"), "goblinData.xml");

// Keep adding files until you've added everything.
parser.AddString(File.ReadAllText("elfData.xml"), "elfData.xml");

// Call when you've added everything. This finishes all parsing.
parser.Finish();

// You don't need to keep the Parser object around after this point.
```

Define some static references.

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

// You don't have to include reference classes for def types you don't intend to reference by name.
// In this design, `SpriteSheetDef`s are referenced only through `MonsterDef`s.
// Therefore there is no need for a `SpriteSheetDefs` class.
```

Use defs in code.

```cs
// This is an example of an actual monster. There's no requirement that it be named "Monster"; def does not care what you do with the data it's generated.
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
    // (We're assuming that you have an Entity system in this project already. The details are up to you; def doesn't specify anything about entities.)
    Entity.Spawn(MonsterDefs.Goblin);
  }
}
```

And those are the basics.