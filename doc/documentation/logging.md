# Logging

**THIS IS VERY IMPORTANT AND YOU SHOULD READ IT.**

Dec is designed to be highly robust in the face of errors. It will recover from missing fields, dubious XML, broken references, and type mismatches whenever possible, and it will do its absolute best to make reasonable decisions as to what an intuitive fallback would be. This is a critical property for mod support - you don't want a single broken mod to crash the entire load - but it has the unfortunate side effect that errors can be easy to miss if you don't have good logging in place.

As a result, Dec defaults to treating errors as immediately lethal. It will throw an exception for the slightest issue. However, because this isn't the intended run mode, that exception will include a message telling you how to turn off the throw-on-error behavior, and directing you to this webpage. (Perhaps that's how you found it?)

It is *strongly* recommended that you set up something that will be impossible to miss. In this author's experience, anything directed to an error log is not visible enough; how many errors does your game output on startup? This author has seen "about four hundred" considered perfectly reasonable and not worthy of concern, and disagrees strongly with that choice. We recommend modal popups of one kind or another so you can't miss them and so they're annoying enough that you fix them. See Rimworld's development-mode console log window for a good example.

If you have a bug report, and the bug turned out to be "I wasn't reading the error", we will direct you to this webpage. If you complain about Dec throwing exceptions for the slightest issue, we will direct you to this webpage. In both cases, the solution is "first, read your errors".

Note that I won't blame you *too* much for making this mistake because ignoring errors is endemic in the game industry, which you are presumably part of. But you should still do better; your life will be improved by it.

&nbsp;

If you have a better idea for how to get people to *read this webpage and stop ignoring errors*, please suggest it, 'cause I'm all out of ideas.

## Recommended setup

For a shipping game, the recommended configuration is:

1. Replace [WarningHandler](xref:Dec.Config.WarningHandler) and [ErrorHandler](xref:Dec.Config.ErrorHandler) with something that surfaces messages visibly and unignorably to whoever is running the build - devs see popups, testers see extremely visible in-game banners, CI sees a build failure.
2. Replace [ExceptionHandler](xref:Dec.Config.ExceptionHandler) with the same (often implemented in terms of ErrorHandler.)
3. [InfoHandler](xref:Dec.Config.InfoHandler) can stay routed at a normal log or be silenced entirely in release builds.
4. If you've left any of the default warning/error handlers in place, which is reasonable if you're intercepting messages from the engine logging system, set [DefaultHandlerThrowExceptions](xref:Dec.Config.DefaultHandlerThrowExceptions) to `Never` so they stop throwing on you.

Be aware that multithread-capable parts of Dec will happily call these functions from multiple threads simultaneously; ensure your implementations are threadsafe. Don't bother being clever with this - if you're getting enough error spam that simple mutexes are a performance issue, then thread contention is the least of your issues.

Errors are meant to be fixed, not suppressed. Configure logging so they're impossible to ignore.
