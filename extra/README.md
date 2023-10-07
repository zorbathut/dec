dec extras
---

Welcome to the Dec extras!

This directory contains optional packages containing functionality that may be more heavyweight, specialized, or unstable than the norm. Nothing in here is necessary for use of Dec; some parts may be desirable, however.

## [recorder_enumerator](recorder_enumerator)

This provides a wide set of converters and other functionality to serialize and deserialize enumerators, both for built-in containers and Linq functions and for user-created enumerators. It currently has limited support - official support is provided only for .NET 7.0 - but that could be broadened if anyone needs it.

This needs to be clear: this functionality is dark magic and curses all who look upon it. If you build a game on it and it later turns out that there's some core issue preventing full support that I was not aware of, my response is going to be "huh, that's too bad". There be dragons here.

I'm using it, though! So I'll probably run into those problems before you do.