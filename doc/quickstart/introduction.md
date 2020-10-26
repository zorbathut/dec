# Introduction

Def is a library for defining game asset types in XML. It includes extensive error reporting and recovery for ease of development, uses reflection to prevent writing lots of boilerplate code, is designed to allow easy moddability moddable by endusers (though not yet implemented), and includes a serialization system (intended for savegames, but often used for other things) that integrates cleanly with the underlying Def types.

## The Basics

Def types represent general categories of thing, such as "monster" or "inventory item". They're C# classes that inherit from [Def.Def](xref:Def.Def).

Def instances represent specific types of thing, such as "Goblin" or "Cheese". They don't represent specific goblins or cheese items in the world, only the template that Goblins or Cheeses are based on. Def instances are instances of Def types, defined in XML files and created at runtime by the Def library. Def instances should be immutable during runtime; if you attack a goblin, it doesn't wound the global concept of a goblin, it wounds a specific goblin.

Def instances are created in XML ([with plans for CSV](xref:future_csv)), usually using a text editor.

## Examples

A Def type `class MonsterDef` could refer to any monster that the player may fight. The class would include properties for maximum health, attacks, and loot tables. Def instances would include Goblin, Bugbear, and GnollKing, all defined in XML. At runtime, you might create hundreds of monsters that refer to the Goblin def for their stats, each with their own current health.

A Def type `class CardDef` could refer to a collectible card that the player assembles into decks. Hundreds of CardDef instances are defined in XML, although the player may see only a handful of them during each game. The class includes properties for cost, effects, and card art. Individual `class Card`s are created at runtime; each card can be upgraded or damaged during play, and `Card` contains both the card's Def and the mutable status of that specific card.

A Def type `class KeybindDef` could refer to a single configurable keybind used during gameplay and displayed in the main menu under "Options". The game creates a number of `class Keybind`s which store the actual keybind information. This data is then saved to disk using the serialization system and loaded on startup.

A Def type `class ItemDef` could refer to anything the player can pick up and store in their inventory. `class WeaponDef` and `class ArmorDef` inherit from it. `ItemDef` lists name, weight, value, and description. `WeaponDef` includes damage and attack types, `ArmorDef` includes detailed stats that describe how it resists damage. When an item is dropped, the game instantiates a `class Item` that links to an `ItemDef` and includes extra per-item information, like wear and random stats.

In addition, a Def type `class AlchemyEffectDef` could refer to any possible effect that can be added to potions via a player Alchemy skill. The potions themselves would be a specific `ItemDef` Def instance, but because there are literally trillions of possible player-crafted potion combinations, it would be impossible to make one Def instance for each unique potion. As a result, there's a single `class Potion` that inherits from `Item` and includes properties listing which set of `AlchemyEffectDef`'s would occur if the player drank (or threw) this potion.
