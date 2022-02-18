# Too Long, Didn't Read

## Set up Dec and add your XML directory.

```cs
void CalledOnceDuringGameStartup()
{
    // If your game code isn't in a namespace, you don't need this.
    // If it is, update this to the appropriate namespace.
    Dec.Config.UsingNamespaces = new string[] { "YourGame" };

    var parser = new Dec.Parser();
    parser.AddDirectory("data");
    parser.Finish();
}
```

## Define some classes derived from [`Dec.Dec`](xref:Dec.Dec).

```cs
// Here, we're declaring a general template for monsters.
// This goes somewhere appropriate in your C# game code.
class MonsterDec : Dec.Dec
{
  // Most types can be defined as simple fields.
  public float maxHP;
  
  // Fields can have default values.
  public float renderScale = 1;
  
  // Classes and structs can also have defaults.
  public Color tint = Color.White;
  
  // Decs can reference other decs.
  public SpriteSheetDec spriteSheet;
  
  // Dec references are not limited to tree structures.
  // Decs can reference other decs in circles, reference other decs of the same type,
  // and even reference themselves.
  public MonsterDec evolvedVariant = null;
}
```

## Write XML to declare our actual objects.

```xml
<Decs>
  <!-- Here's a basic goblin. -->
  <!-- We've overridden the defaults we want changed and ignored the rest. -->
  <!-- Every dec must be named via the decName attribute. -->
  <!-- These names must be unique within dec type, but can overlap across different types. -->
  <!-- They are intended to be human-readable for debug and development purposes, -->
  <!-- but not user-visible. -->
  <!-- They must be valid C# identifiers (unicode accepted.) -->
  <MonsterDec decName="Goblin">
    <maxHP>20</maxHP>
    
    <!-- This connects MonsterDec.Goblin's spriteSheet to SpriteSheetDec.Goblin. -->
    <spriteSheet>Goblin</spriteSheet>
  </MonsterDec>

  <!-- This is a larger goblin. -->
  <!-- We've given it a new name, a larger render scale, and more health. -->
  <MonsterDec decName="MegaGoblin">
    <maxHP>100</maxHP>
    <renderScale>3</renderScale>
    <spriteSheet>Goblin</spriteSheet>
  </MonsterDec>

  <!-- Here, we've defined a new tint for the DarkGoblin. -->
  <!-- Modifying members of included classes works like you'd expect XML to. -->
  <MonsterDec decName="DarkGoblin">
    <maxHP>40</maxHP>
    <tint>
      <r>0.3</r>
      <g>0.3</g>
      <b>0.4</b>
    </tint>
    <spriteSheet>Goblin</spriteSheet>
  </MonsterDec>

  <!-- Referencing other decs is as simple as typing their decName. -->
  <!-- All dec references are validated at load time. -->
  <MonsterDec decName="AlchemyGoblin">
    <maxHP>10</maxHP>
    <spriteSheet>Goblin</spriteSheet>
    <evolvedVariant>MegaGoblin</evolvedVariant>
  </MonsterDec>

  <!-- Decs can be referenced before they're declared, or even from other files. -->
  <!-- XML order is irrelevant. -->
  <SpriteSheetDec decName="Goblin">
    <filename>goblin.png</filename>
  </SpriteSheetDec>
</Decs>
```

## Declare some static references.

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
  
  // You don't have to include all decs here - in fact,
  // it's recommended that you only include decs that you plan to reference by name.
}
```

## Use decs in code.

```cs
// This is an example of an actual monster.
// There's no requirement that it be named "Monster";
// dec does not care what you do with the data it's generated.
public class Monster
{
  public Monster(MonsterDec dec)
  {
    // It's common for objects to keep a reference to the dec that they're created from.
    this.dec = dec;
    
    // decs are just classes; using things from them is simple.
    hp = dec.maxHP;
  }
  
  private MonsterDec dec;
  
  private float hp;
}

public class GoblinGenerator
{
  public Generate()
  {
    // If you need to use a static reference, just pull it out of the static reference class.
    // (We're assuming that you have an Entity system in this project already.
    // The details are up to you; dec doesn't specify anything about entities.)
    Entity.Spawn(MonsterDecs.Goblin);
  }
}
```