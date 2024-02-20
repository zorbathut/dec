dec
---

[![Language: C#](https://img.shields.io/badge/language-C%23-blue)](https://docs.microsoft.com/en-us/dotnet/csharp/) [![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://opensource.org/licenses/MIT) [![License: Unlicense](https://img.shields.io/badge/license-Unlicense-blue.svg)](http://unlicense.org/)

[![Build status](https://img.shields.io/github/actions/workflow/status/zorbathut/dec/test.yml?branch=dev)](https://github.com/zorbathut/dec/actions?query=workflow%3ATest+branch%3Adev) [![Coverage Status](https://coveralls.io/repos/github/zorbathut/dec/badge.svg)](https://coveralls.io/github/zorbathut/dec) [![NuGet Downloads](https://img.shields.io/nuget/dt/dec?label=nuget%20downloads)](https://www.nuget.org/packages/dec) [![Support](https://img.shields.io/discord/703688553707601962?label=support&logo=discord)](https://discord.gg/vQv9DMA)

Dec is a C# library for declaring game asset types in XML. It includes extensive error reporting and recovery for ease of development, uses reflection to prevent writing lots of boilerplate code, is designed to allow easy moddability moddable by endusers (though not yet implemented), and includes a serialization system (intended for savegames, but often used for other things) that integrates cleanly with the underlying Dec types.

Dec instances are intended to represent classes of thing rather than specific things. As an example, you might have `ObjectDec.Chair` and `MonsterDec.Goblin`, but you wouldn't have a Dec instance for each individual chair or goblin in a level. Individual chairs or goblins would contain references to the `ObjectDec.Chair`/`MonsterDec.Goblin` Dec instances, in whatever way is easiest for your level editor or entity system.

The Dec library does not tie itself to any particular engine and works within Godot, Unity, MonoGame, or any other C# environment.

Dual-licensed under MIT or the [Unlicense](http://unlicense.org).


### Releases

* [Latest](https://github.com/zorbathut/dec/releases/latest)

### Documentation

* [Frontpage](https://zorbathut.github.io/dec/release/)

* [Quick start](https://zorbathut.github.io/dec/release/quickstart/introduction.html)

* [API reference](https://zorbathut.github.io/dec/release/api/index.html)

* [Example project: Legend of the Amethyst Futon](example/loaf)


### Extremely Dense Quick-Start

#### Set up Dec and add your XML directory.

```cs
void CalledOnceDuringGameStartup()
{
    Dec.Config.UsingNamespaces = new string[] { "YourGame" };

    var parser = new Dec.Parser();
    parser.AddDirectory("data");
    parser.Finish();
}
```

#### Declare some classes derived from [`Dec.Dec`](xref:Dec.Dec).

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
  
  // Dec references are not limited to tree structures. Decs can reference other decs in circles,
  // reference other decs of the same type, and even reference themselves.
  public MonsterDec evolvedVariant = null;
}
```

#### Write XML to declare our actual objects.

```xml
<Decs>
  <!-- Here's a basic goblin. We've overridden the defaults we want changed and ignored the rest. -->
  <!-- Every dec must be named via the decName attribute. -->
  <!-- These names must be unique within dec type, but can overlap across different types. -->
  <!-- They are intended to be human-readable for debug and development purposes, but not user-visible. -->
  <!-- They must be valid C# identifiers (unicode accepted.) -->
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

  <!-- Here, we've declared a new tint for the DarkGoblin. -->
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

#### Declare some static references.

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

#### Use decs in code.

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


### Extras

Dec includes a few extra packages providing functionality which may not be desired, for reasons of functionality, security, or stability. I recommend [browsing them](https://github.com/zorbathut/dec/tree/dev/extra) to see what's available.


### Support

* [Discord server](https://discord.gg/vQv9DMA)
