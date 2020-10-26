# Def types and instances

The next step is to create Def types.

Def types are simply C# classes that inherit from [Def.Def](xref:Def.Def) and contain some number of properties. Here's an example of what that might look like - your game will likely contain a different set of things.

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
  
  // Def references are not limited to a tree structures. Defs can reference other defs in circles, reference other defs of the same type, and even reference themselves.
  public MonsterDef evolvedVariant = null;
}
```

Once you have a Def type, you can start writing Def instances. Here's a few examples of that, using the above Def type.

```xml
<!-- This is an XML file, and should be placed so it can be read from your built games. -->
<!-- Details vary per engine; if you're on Unity, look up StreamingAssets. -->
<!-- The filename doesn't matter, aside from having an .xml extension. -->

<Defs>
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
</Defs>
```
