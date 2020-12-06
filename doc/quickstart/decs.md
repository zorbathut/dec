# Dec types and instances

The next step is to create Dec types.

Dec types are simply C# classes that inherit from [Dec.Dec](xref:Dec.Dec) and contain some number of properties. Here's an example of what that might look like - your game will likely contain a different set of things.

```cs
// Here, we're declaring a general template for monsters.
// This goes somewhere appropriate in your C# game code.
class MonsterDec : Dec.Dec
{
  // Most types can be defined as simple fields.
  public float maxHP;
  
  // Fields can have default values.
  public float renderScale = 1;
  
  // Classes and structs can be defined inline.
  public Color tint = Color.White;
  
  // Decs can reference other decs.
  public SpriteSheetDec spriteSheet;
  
  // Dec references are not limited to tree structures. Decs can reference other decs in circles, reference other decs of the same type, and even reference themselves.
  public MonsterDec evolvedVariant = null;
}
```

Once you have a Dec type, you can start writing Dec instances. Here's a few examples of that, using the above Dec type.

```xml
<!-- This is an XML file, and should be placed so it can be read from your built games. -->
<!-- Details vary per engine; if you're on Unity, look up StreamingAssets. -->
<!-- The filename doesn't matter, aside from having an .xml extension. -->

<Decs>
  <!-- Here's a basic goblin. We've overridden the defaults we want changed and ignored the rest. -->
  <!-- Every dec must be named via the decName attribute. These names must be unique within dec type, but can overlap across different types. They are intended to be human-readable for debug and development purposes, but not user-visible. They must be valid C# identifiers (unicode accepted.) -->
  <MonsterDec decName="Goblin">
    <maxHP>20</maxHP>
    
    <!-- This connects MonsterDec.Goblin's spriteSheet to SpriteSheetDec.Goblin. -->
    <spriteSheet>Goblin</spriteSheet>
  </MonsterDec>

  <!-- This is a larger goblin. We've given it a new name, a larger render scale, and more health. -->
  <MonsterDec decName="MegaGoblin">
    <maxHP>100</maxHP>
    <renderScale>3</renderScale>
    <spriteSheet>Goblin</spriteSheet>
  </MonsterDec>

  <!-- Here, we've declared a new tint for the DarkGoblin. Modifying members of included classes works like you'd expect XML to. -->
  <MonsterDec decName="DarkGoblin">
    <maxHP>40</maxHP>
    <tint>
      <r>0.3</r>
      <g>0.3</g>
      <b>0.4</b>
    </tint>
    <spriteSheet>Goblin</spriteSheet>
  </MonsterDec>

  <!-- Referencing other decs is as simple as typing their decName. All dec references are validated at load time. -->
  <MonsterDec decName="AlchemyGoblin">
    <maxHP>10</maxHP>
    <spriteSheet>Goblin</spriteSheet>
    <evolvedVariant>MegaGoblin</evolvedVariant>
  </MonsterDec>

  <!-- Decs can be referenced before they're declared. XML order is irrelevant. -->
  <SpriteSheetDec decName="Goblin">
    <filename>goblin.png</filename>
  </SpriteSheetDec>
</Decs>
```
