# Modding

A major goal of Dec is to make it easy to support user-created game mods. This feature contains a few major parts:

* Standard descriptors for defining mods and defining mods' interactions with each other (see [Factorio's info.json file](https://wiki.factorio.com/Tutorial:Mod_structure#info.json) for an example of this)
* A load system that loads mod .dll's and code in an appropriate order
* The ability to override and modify Dec data within mods, with appropriate error messages and diagnostics

All of this is provided by the optional [modding extra](https://github.com/zorbathut/dec/tree/dev/extra/modding), which reads mod manifests, resolves their dependencies into an automatic load order, loads mods' assemblies, and builds a `ParserModular` for you. We recommend using that; go read up on it, then come back here.

## Merge Modes

Mods often want to modify existing Decs in various ways. This behavior is similar to how [inheritance](inheritance.md) works. See [merge modes](mergemodes.md) for details.

## Security

Dec is designed for an environment where mod developers will be able to create mods based around C# DLLs. As it's nearly impossible to sandbox a C# DLL, Dec makes very little effort to add more security through the .xml files themselves. To make matters worse, a major tool for C# modding is [Harmony](https://github.com/pardeike/Harmony), which monkeypatches the C# runtime through inserting machine-code changes at runtime. This is also essentially impossible to sandbox.

Historically, this seems to mostly work out; modders are generally not malicious. This is also not solvable without either requiring some form of sandboxable user-provided code, or without removing code entirely from the modding system.

I'd like to solve this, but practically speaking this will not be solvable without either major engine-level changes or a completely separate programming language; the latter I consider undesirable, the former is extraordinarily unlikely for Unity.

If your game design requires security against malicious mods, you should not use Dec.

## ParserModular

While the `modding` extra provides all the functionality you need, nothing stops you from reimplementing the load order/dependency stuff on your own. If you want to do that, you'll need to build a ParserModular yourself.

Dec organizes its input into groups of files called "modules", defined by [`Dec.ParserModular.Module`](xref:Dec.ParserModular.Module). Each module represents a single mod's worth of data. Modules are created via [`Dec.ParserModular`](xref:Dec.ParserModular), which is a mod-capable replacement for [`Dec.Parser`](xref:Dec.Parser).

(Dec.Parser is just a thin wrapper around Dec.ParserModule to make it easier to develop non-moddable games.)

Be aware that module load order is *critical*, and right now, this is defined simply as the order in which Module objects are created.

```cs
var parser = new Dec.ParserModular();
parser.CreateModule("Base").AddDirectory("XML");

// Order alphabetically to ensure that the load order is consistent
var modDirectories = Directory.GetDirectories("Mods").OrderBy(d => d);
foreach (var modDirectory in modDirectories)
{
    var modName = Path.GetFileName(modDirectory);
    parser.CreateModule(modName).AddDirectory(Path.Combine(modDirectory, "XML"));
}

parser.Finish();
```
