# Setup

Dec is designed to be initialized exactly once, at the beginning of your program. Normally you'll want to initialize it after displaying your initial loading screen but before displaying the main menu. This allows you to include Dec-authored information within the main menu, such as difficulty settings, keybinds, or savegame metadata.

The exact way of accomplishing this will depend on your engine and your game architecture. On Unity, we recommend <a href="https://docs.unity3d.com/ScriptReference/RuntimeInitializeOnLoadMethodAttribute-ctor.html">RuntimeInitializeOnLoadMethod</a>; on Godot, <a href="https://docs.godotengine.org/en/stable/getting_started/step_by_step/singletons_autoload.html">AutoLoad</a>. These are not the only solutions and you should use whatever you feel most comfortable with, as long as you can guarantee that the following code will be run exactly once each time the game is started.

## Summary

Most initial Dec initialization routines will look like this.

```cs
void StartupDec()
{
    // If your game code isn't in a namespace, you don't need this.
    // If it is, update this to the appropriate namespace.
    Dec.Config.UsingNamespaces = new string[] { "YourGame" };

    var parser = new Dec.Parser();
    parser.AddDirectory("data");
    parser.Finish();
}
```

Read on for more explanation.

## Logging

Dec generates warning and error messages. *It is strongly recommended that you make these unignorable to developers.* We recommend popup windows and modal dialogs; Dec is designed to aggressively recover from errors, which is great for mod compatibility and frustrating for developers if the error message is easily missable.

By default, Dec routes messages to the engine-appropriate log (Unity, Godot, or the C# console) <i>and also throws exceptions on warnings and errors</i>. The first time you hit one, the exception will tell you how to turn this off. You'll usually want to install your own [InfoHandler](xref:Dec.Config.InfoHandler) / [WarningHandler](xref:Dec.Config.WarningHandler) / [ErrorHandler](xref:Dec.Config.ErrorHandler) / [ExceptionHandler](xref:Dec.Config.ExceptionHandler):

```cs
Dec.Config.InfoHandler = str => YourGame.Logging.LogInfoMessage(str);
Dec.Config.WarningHandler = str => YourGame.Logging.LogWarningMessage(str);
Dec.Config.ErrorHandler = str => YourGame.Logging.LogErrorMessage(str);
Dec.Config.ExceptionHandler = e => YourGame.Logging.LogException(e);
```

See [Logging](~/documentation/logging.md) for more details and justifications.

## Namespaces

It's common to specify classes within Dec XML. It's also common for games to be contained within their own namespaces, which makes class names verbose and slow to type. [Dec.Config.UsingNamespaces](xref:Dec.Config.UsingNamespaces) lets you provide Dec with your game namespaces in the manner of a C# `using` directive. You can specify as many namespaces as you like.

```cs
Dec.Config.UsingNamespaces = new string[] { "YourGame", "YourGame.Gameplay" };
```

## Parsing

Dec construction is done via a class named [Dec.Parser](xref:Dec.Parser). At some point in the future Parser will support multithreaded parsing and incremental parsing. That day is not today, and at the moment it's a simple interface for feeding in XML data.

Dec files are simple XML files, normally stored along with the rest of your game data. Most people will simply put these in a dedicated subdirectory and [Dec.Parser.AddDirectory()](xref:Dec.Parser.AddDirectory*) will scan the entire directory for `*.xml` files (or other extensions, provided as a parameter to the function) to make this as easy as possible.

```cs
var parser = new Dec.Parser();
parser.AddDirectory("data");
parser.Finish();
```

Parser can be garbage-collected after this completes.

If you need to add files manually, [Dec.Parser.AddFile()](xref:Dec.Parser.AddFile*) exists; if your XML data is stored in some format besides plain files, [Dec.Parser.AddStream()](xref:Dec.Parser.AddStream*) or [Dec.Parser.AddString()](xref:Dec.Parser.AddString*) allows you to add raw XML data.

On Unity, it's likely that you'll place XML files inside the StreamingAssets directory, in which case you'd want to read them using Unity's [Application.streamingAssetsPath](https://docs.unity3d.com/ScriptReference/Application-streamingAssetsPath.html) property.

```cs
var parser = new Dec.Parser();
parser.AddDirectory(Application.streamingAssetsPath);
parser.Finish();
```
