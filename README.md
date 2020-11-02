def
---

[![Language: C#](https://img.shields.io/badge/language-C%23-blue)](https://docs.microsoft.com/en-us/dotnet/csharp/) [![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://opensource.org/licenses/MIT) [![License: Unlicense](https://img.shields.io/badge/license-Unlicense-blue.svg)](http://unlicense.org/)

[![Build status](https://circleci.com/gh/zorbathut/def.svg?style=shield)](https://circleci.com/gh/zorbathut/def) [![Coverage Status](https://coveralls.io/repos/github/zorbathut/def/badge.svg)](https://coveralls.io/github/zorbathut/def) [![Support](https://img.shields.io/discord/703688553707601962?label=support&logo=discord)](https://discord.gg/vQv9DMA)

Def is a C# library for defining game asset types in XML. It includes extensive error reporting and recovery for ease of development, uses reflection to prevent writing lots of boilerplate code, is designed to allow easy moddability moddable by endusers (though not yet implemented), and includes a serialization system (intended for savegames, but often used for other things) that integrates cleanly with the underlying Def types.

Def instances are intended to represent classes of thing rather than specific things. As an example, you might have `ObjectDef.Chair` and `MonsterDef.Goblin`, but you wouldn't have a Def instance for each individual chair or goblin in a level. Individual chairs or goblins would contain references to the `ObjectDef.Chair`/`MonsterDef.Goblin` Def instances, in whatever way is easiest for your level editor or entity system.

The Def library does not tie itself to any particular engine and works within Godot, Unity, MonoGame, or any other C# environment.

Dual-licensed under MIT or the [Unlicense](http://unlicense.org).


### Documentation

* [Frontpage](https://zorbathut.github.io/def/)

* [Quick start](https://zorbathut.github.io/def/quickstart/introduction.html)

* [API reference](https://zorbathut.github.io/def/api/index.html)

* [Example project: Legend of the Amethyst Futon](example/loaf)


### Extremely Dense Quick-Start

#### Set up Def and add your XML directory.

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

#### Define some classes derived from [`Def.Def`](xref:Def.Def).

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
  
  // Def references are not limited to a tree structures. Defs can reference other defs in circles,
  // reference other defs of the same type, and even reference themselves.
  public MonsterDef evolvedVariant = null;
}
```

#### Write XML to define our actual objects.

```xml
<Defs>
  <!-- Here's a basic goblin. We've overridden the defaults we want changed and ignored the rest. -->
  <!-- Every def must be named via the defName attribute. -->
  <!-- These names must be unique within def type, but can overlap across different types. -->
  <!-- They are intended to be human-readable for debug and development purposes, but not user-visible. -->
  <!-- They must match the regexp [A-Za-z][A-Za-z0-9_]*. -->
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

#### Define some static references.

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

#### Use defs in code.

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


### Support

* [Discord server](https://discord.gg/vQv9DMA)
