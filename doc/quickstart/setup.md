# Setup

Def is designed to be initialized exactly once, at the beginning of your program. Normally you'll want to initialize it after displaying your initial loading screen but before displaying the main menu. This allows you to include Def-authored information within the main menu, such as difficulty settings, keybinds, or savegame metadata.

The exact way of accomplishing this will depend on your engine and your game architecture. On Unity, we recommend <a href="https://docs.unity3d.com/ScriptReference/RuntimeInitializeOnLoadMethodAttribute-ctor.html">RuntimeInitializeOnLoadMethod</a>; on Godot, <a href="https://docs.godotengine.org/en/stable/getting_started/step_by_step/singletons_autoload.html">AutoLoad</a>. These are not the only solutions and you should use whatever you feel most comfortable with, as long as you can guarantee that the following code will be run exactly once each time the game is started.

## Summary

Most initial Def initialization routines will look like this.

```cs
void StartupDef()
{
    Def.Config.DefaultHandlerThrowExceptions = false;
    Def.Config.UsingNamespaces = new string[] { "YourGame" };

    var parser = new Def.Parser();
    parser.AddDirectory("data");
    parser.Finish();
}
```

Read on for more explanation.

## Logging

Def will generate warning and error messages. <i>It is strongly recommended that you make these unignorable to developers.</i> Popup windows and modal dialogs may be appropriate here; Def does its best to recover from errors, which is great for mod compatibility but can be frustrating for developers if the error message is easily missable.

By default, Def will output to your normal system error log (Unity log for Unity programs, C# console otherwise). If you have your own logging framework, or want to decorate Def messages with a recognizable tag, this is the place to do it. [Def.Config.InfoHandler](xref:Def.Config.InfoHandler), [Def.Config.WarningHandler](xref:Def.Config.WarningHandler), [Def.Config.ErrorHandler](xref:Def.Config.ErrorHandler), and [Def.Config.ExceptionHandler](xref:Def.Config.ExceptionHandler) can all be assigned separately for their respective type of log message.

```cs
Def.Config.InfoHandler = str => YourGame.Logging.LogInfoMessage(str);
Def.Config.WarningHandler = str => YourGame.Logging.LogWarningMessage(str);
Def.Config.ErrorHandler = str => YourGame.Logging.LogErrorMessage(str);
Def.Config.ExceptionHandler = e => YourGame.Logging.LogException(e);
```

## Exceptions

As mentioned above, Def attempts to recover from errors whenever possible. However, we've found that new Def developers often miss error messages and end up very confused. To help with that, Def's default log handler throws exceptions on warnings and errors, along with an explanatory message about how this can be suppressed; specifically, through [Def.Config.DefaultHandlerThrowExceptions](xref:Def.Config.DefaultHandlerThrowExceptions).

```cs
Def.Config.DefaultHandlerThrowExceptions = false;
```

If you want to keep the exception behavior, but don't want the message, the message alone can be suppressed with [Def.Config.DefaultHandlerShowConfigOnException](xref:Def.Config.DefaultHandlerShowConfigOnException).

```cs
Def.Config.DefaultHandlerShowConfigOnException = false;
```

If you've defined your own logging handlers, Def's default exception throwing will be automatically disabled. You can throw your own exceptions if you prefer that behavior.

## Namespaces

It's common to specify classes within Def XML. It's also common for games to be contained within their own namespaces, which makes class names verbose and slow to type. [Def.Config.UsingNamespaces](xref:Def.Config.UsingNamespaces) lets you provide Def with your game namespaces in the manner of a C# `using` directive. You can specify as many namespaces as you like.

```cs
Def.Config.UsingNamespaces = new string[] { "YourGame", "YourGame.Gameplay" };
```

## Parsing

Def construction is done via a class named [Def.Parser](xref:Def.Parser). At some point in the future Parser will support multithreaded parsing and incremental parsing. That day is not today, and at the moment it's a simple interface for feeding in XML data.

Def files are simple XML files, normally stored along with the rest of your game data. Most people will simply put these in a dedicated subdirectory and [Def.Parser.AddDirectory()](xref:Def.Parser.AddDirectory*) will scan the entire directory for `*.xml` files (or other extensions, provided as a parameter to the function) to make this as easy as possible.

```cs
var parser = new Def.Parser();
parser.AddDirectory("data");
parser.Finish();
```

Parser can be garbage-collected after this completes.

If you need to add files manually, [Def.Parser.AddFile()](xref:Def.Parser.AddFile*) exists; if your XML data is stored in some format besides plain files, [Def.Parser.AddString()](xref:Def.Parser.AddString*) allows you to add raw XML data.

On Unity, it's likely that you'll place XML files inside the StreamingAssets directory, in which case you'd want to read them using Unity's [Application.streamingAssetsPath](https://docs.unity3d.com/ScriptReference/Application-streamingAssetsPath.html) property.

```cs
var parser = new Def.Parser();
parser.AddDirectory(Application.streamingAssetsPath);
parser.Finish();
```
