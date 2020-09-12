# def


## Summary

def is a C# library intended for defining concepts and templates for game engines. It does not tie itself to any particular engine and works within Godot, Unity, MonoGame, or any other C# environment.

Defs are intended to represent classes of thing rather than instances of things. As an example, you might have `ObjectDef.Chair` and `MonsterDef.Goblin`. Individual chairs or goblins in your level would link to those Defs, in whatever way is easiest for your level editor or generator.


## Why should I use def?

Defining types of things is a common thing to do in game engines, and def is intended as a one-stop-shop to allow defining anything which may need to be defined, from in-game objects like monsters or weapons to conceptual things like actor behaviors.

* Defining new types is easy thanks to reflection-based parsing.

* Full support for cross-referenced objects, preventing consistency-check issues involved in string references.

* Your data is stored in human-readable XML and can be organized in whatever manner best suits your project.

* Extensive internal validation tests for anything that's disallowed, with full filename and line number information.

* Recovery code capable of continuing after almost any data error, if configured to do so.

* Unit test suite to guard against unexpected regressions.

* Zero CPU use at runtime; most uses of def are simple class member accesses.

* Full open source package, available under both MIT license and the Unlicense.


## Why shouldn't I use def yet?

* While def takes design cues from several well-tested production libraries, def itself has not yet been used in any released commercial product.

* Many desired features are planned, but few are currently implemented.

* No development community yet except for the author.

I plan to use this tool in all of my upcoming projects; it remains to be seen if it will gain traction outside of that.


## Why shouldn't I use def ever?

* If your project has hundreds of thousands of templates, def may experience scaling issues due to keeping all template types in memory at all times. This is not likely to be an issue for any game except MMOs.

* Secret items, transmitted at runtime from a server, are not practical because def requires all data to be available at startup. This may present problems for always-online games that are intended to have live-but-undiscovered secrets.

* def's design relies heavily on static typing and reflection, which limits the number of languages it could easily be ported to. At the moment, its sole implementation is in C#, and there are no plans for other implementations. If your game logic is not written in C# you probably shouldn't be using def.

----

If you'd like to delve more deeply, I recommend starting at [Quick Start](articles/quickstart.md).