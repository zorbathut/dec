# dec


## Summary

Dec is a C# library for **dec**laring game asset types in XML. It includes extensive error reporting and recovery for ease of development, uses reflection to prevent writing lots of boilerplate code, is designed to allow easy moddability moddable by endusers (though not yet implemented), and includes a serialization system (intended for savegames, but often used for other things) that integrates cleanly with the underlying Dec types.

Dec instances are intended to represent immutable classes of thing rather than specific things. As an example, you might have `ObjectDec.Chair` and `MonsterDec.Goblin`, but you wouldn't have a Dec instance for each individual chair or goblin in a level. Individual chairs or goblins would contain references to the `ObjectDec.Chair`/`MonsterDec.Goblin` Dec instances, in whatever way is easiest for your level editor or entity system.

The Dec library does not tie itself to any particular engine and works within Godot, Unity, MonoGame, or any other C# environment.


## Why should I use dec?

Declaring types of things is a common thing to do in game engines, and the Dec library  is intended as a one-stop-shop to allow defining anything which may need to be defined, from in-game objects like monsters or weapons to conceptual things like actor behaviors.

* Declaring new types is easy thanks to reflection-based parsing.

* Full support for cross-referenced objects, preventing consistency-check issues involved in string references.

* Your data is stored in human-readable XML and can be organized in whatever manner best suits your project.

* Extensive internal validation tests for anything that's disallowed, with full filename and line number information.

* Recovery code capable of continuing after almost any data error, if configured to do so.

* Unit test suite to guard against unexpected regressions.

* Zero CPU use at runtime; most uses of the Dec library are simple class member accesses.

* Full open source package, available under both MIT license and the Unlicense.


## Why shouldn't I use dec yet?

* While the Dec library takes design cues from several well-tested production libraries, it has not yet been used in any released commercial product.

* Many desired features are planned, but few are currently implemented.

* No development community yet except for the author.

I plan to use this tool in all of my upcoming projects; it remains to be seen if it will gain traction outside of that.


## Why shouldn't I use dec ever?

* If your project has hundreds of thousands of Dec instances, the Dec library may experience scaling issues due to keeping all instances in memory at all times. This is not likely to be an issue for any game except MMOs.

* Secret items, transmitted at runtime from a server, are not practical because the Dec library requires all data to be available at startup. This may present problems for always-online games that are intended to have live-but-undiscovered secrets.

* The Dec library's design relies heavily on static typing and reflection, which limits the number of languages it could easily be ported to. At the moment, its sole implementation is in C#, and there are no plans for other implementations. If your game logic is not written in C# you probably shouldn't be using this.

----

If you'd like to delve more deeply, I recommend starting at [Quick Start](quickstart/introduction.md).