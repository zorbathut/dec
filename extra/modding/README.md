modding
---

This finds installed mods, reads their manifests, resolves their dependencies into an automatic load order, loads their assemblies, runs their entry points, and hands back a `ParserModular` with one module per mod.

### Installation instructions

Add the source code to your project, along with the [SemanticVersioning](https://www.nuget.org/packages/SemanticVersioning) NuGet package. It requires .NET 6 or later; Unity is not supported.

### Usage

```cs
var loader = new Dec.Modding.ModLoader("MyGame", "1.4.0", "Data/Decs");
var resolution = loader.Resolve(new[] { "Mods", workshopDirectory });

// Optionally show a mod screen: resolution.ModsInstalled, resolution.GetStatus(mod), resolution.Problems.

var parser = resolution.Load();
parser.Finish();
```

`Resolve` returns every problem with mods and their relationships in `resolution.Problems`, and `Load` reports them all through Dec's usual handlers before loading anything. Dec's own diagnostics while reading manifests, such as malformed XML or unknown elements, are reported through those handlers as they come up. Dec's default handlers throw on warnings as well as errors, so a game that ships with mod support should configure them.

The base game's version is what mods' version ranges for it are checked against. It can be the game's own version, or a separate modding-API version that changes only when mods need to.

### Mod layout

```
Mods/
  SomeFolder/
    Mod.xml          the manifest
    icon.png         optional; Dec only reports its path
    Decs/            dec XML, loaded like any module's
    Assemblies/      .NET assemblies
```

Every folder directly inside a mod directory is a mod, except folders starting with a dot. A file or folder whose name matches one of these only when ignoring case gets a warning, since it works on case-insensitive filesystems but not on others. A mod with XML but none in `Decs`, or with assemblies but none directly inside `Assemblies`, gets a warning, since those files were probably put in the wrong place and won't load. The manifest, icon, and folder names can be changed, for every mod at once, through `ModLoader`'s properties.

### Mod.xml

```xml
<Mod>
  <id>Author.ModName</id>
  <version>1.2.0</version>
  <name>Mod Name</name>
  <description>What it does.</description>
  <authors>
    <li><name>Author</name><email>author@example.com</email><urls><li><url>https://example.com</url></li></urls></li>
  </authors>
  <urls>
    <li><url>https://example.com/mod</url><label>Homepage</label></li>
  </urls>
  <dependencies>
    <li><id>Other.Library</id><version>^2.0.0</version></li>
    <li><id>Other.Mod</id><kind>Supported</kind><order>Before</order><text>Adds integration with Other.Mod.</text></li>
    <li><id>Broken.Mod</id><kind>Incompatible</kind><version>&lt;3.0.0</version></li>
  </dependencies>
</Mod>
```

- `id` and `version` are required. Ids are 1 to 64 ASCII letters, digits, `_`, `.`, and `-`, starting with a letter; they're case-sensitive, but two mods' ids may not differ only in case. `Author.ModName` is a good convention. Never change a mod's id.
- `version` is a [semantic version](https://semver.org); build metadata is ignored when comparing. Whitespace around ids and versions is ignored.
- `authors` entries may have any of `name`, `email`, and `urls`, but need at least one. `urls` entries need a `url` and may have a `label`.
- `schemaVersion` is optional and defaults to 1. A mod with a newer schema than the game's mod loader understands is disabled with a single explanation.
- Unknown elements produce a warning, not an error.

Each dependency has an `id`, plus:

- `kind`, which defaults to `Required`:

| Kind | If the target is present and in range | If not |
| --- | --- | --- |
| Required | loads | this mod is disabled |
| Recommended | loads | info message |
| Supported | loads | info message |
| Accommodated | loads | nothing |
| Discouraged | both load, with a warning | nothing |
| Incompatible | this mod is disabled | nothing |

- `version`, an [npm-style version range](https://github.com/npm/node-semver#ranges) such as `^1.2.0`, `>=1.0.0 <2.0.0`, or `1.x || 3.x`; leave it out to match every version. Write `<` as `&lt;`. Prereleases of an in-range version match.
- `order`: `After` loads this mod after the target, `Before` loads it before, `Unordered` doesn't constrain order. An entry that leaves it out loads after the target by preference: if mods' default orders form a cycle, those are ignored with a warning. Orders written explicitly are always enforced. Incompatible entries can't set it, and entries for the base game can only say `After`.
- `text`, shown to the player alongside any problem the entry causes.

An entry applies only when the target's installed version is in its range, so one mod may have several entries for the same target with different ranges: for example, Incompatible with `<2.0.0` and Recommended with `^2.0.0`. At most one may be Required, and their ranges may not overlap.

Every mod implicitly requires the base game and loads after it. A mod that lists the base game itself replaces the implicit requirement with its own entries, but still loads after it.

### Resolution

Load order is fully automatic: dependencies first, ties broken by ordinal id, so the order depends only on which mods are installed.

When something is wrong, the affected mods are disabled and everything else still loads:

- A manifest error, such as a missing id or an invalid version, disables the mod.
- Two mods whose ids differ only in capitalization are both disabled, as is a mod using the base game's id.
- A dependency entry of any kind that names an installed mod with the wrong capitalization disables the mod.
- A missing, disabled, or out-of-range Required dependency disables the mod, and in turn any mod that requires it.
- A cycle of explicitly written load orders disables every mod in it. Default orders in a cycle are dropped with a warning instead.
- An Incompatible target that's present disables the mod declaring the incompatibility.
- Two mods shipping an assembly with the same name are both disabled; shared libraries belong in their own mod that others depend on. A mod shipping an assembly the game already has or directly depends on uses the game's copy instead, with a warning, as long as some enabled mod's code references it, every enabled mod's reference to it is to a version no newer than the game's, and both are signed the same way; otherwise, or if the game's version is unknown, the mod is disabled.
- If a mod is installed more than once, the newest working copy is used and the others are hidden, with a warning.

### Code

Every assembly in each enabled mod's `Assemblies` folder is loaded, in load order, before any dec is parsed, except copies of assemblies the game already has, which are skipped in favor of the game's; native libraries in that folder are skipped with a warning. Dec finds the mods' dec types, converters, `[Dec.Setup]` functions, and `[StaticReferences]` classes automatically.

A mod's project needs `<EnableDynamicLoading>true</EnableDynamicLoading>` so its own dependencies are copied into its output, but should reference the game's assemblies, and any package the game already ships, without copying them, at a version no newer than the game's: `<Private>false</Private>` on a `Reference`, `ExcludeAssets="runtime"` on a `PackageReference`, and both on a `ProjectReference`. A mod that ships one of them anyway gets a warning and uses the game's copy, which keeps it working when a later version of the game takes on or upgrades a library the mod ships; if the mod needs a newer version than the game's, it's disabled instead. A copy nothing in any mod references might be the mod's own assembly with a clashing name, so that disables the mod too; but two unsigned assemblies sharing a name and an acceptable version are taken to be the same library. Dec can't confirm that an assembly the game references but hasn't loaded will actually be found, so if the game references an assembly it doesn't ship and a mod ships one by that name, the mod's copy is still skipped, and its code fails to find the assembly. Handing mod authors a template project or reference package with this already set saves everyone the trouble.

A mod may contain one class implementing `Dec.Modding.IModEntrypoint`, whose `Initialize` receives a `ModEntrypointInfo` with the mod and the list of enabled mods. It's created and called once per process, in load order, after every mod's assemblies are loaded and before parsing; this is the place for Harmony patches and other pre-parse work.

Mods' dec types usually live in their own namespaces, which aren't in `Dec.Config.UsingNamespaces`; either use full type names in XML, such as `<Author.ModName.SomeDec decName="Thing">`, or append the namespace to `Dec.Config.UsingNamespaces` in the mod's entry point.

Assemblies can't be unloaded. Loading again in the same process, after `Database.Clear()`, reuses assemblies already loaded without running their entry points again, but a mod whose code was loaded earlier and isn't enabled now keeps running, and is reported as an error. Changing which code mods are enabled needs a restart.

Assemblies load into the same `AssemblyLoadContext` as Dec. That's the default context in an ordinary application, and should make mods bind to the right copies of Dec and the game under hosts that use their own context, such as Godot, though that hasn't been tested.

### Threading

Like `Parser`, `Load` must run on a single thread with no other Dec activity.
