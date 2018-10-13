# Serialization

The def system is basically a big deserialization library. It makes sense to also support serialization, and non-def deserialization, so it can be used to make savegames. This is definitely less work than writing a separate serialization/deserialization system, but it does require a few new features.

## Versioning

Save formats inevitably have to be changed with newer versions of the software. One way or another, we'll need an elegant way to handle save layout changes.

## Explicit defId's

Defs can be referenced by defName, but it's common to rename defs, both for organizational purposes and for actual entity renaming purposes. My plan is that defs will include a list of old defNames, listed under a new defId attribute; if the deserialization routing reads an old defName, it will just map it transparently to the up-to-date def.

## Interior save links

It's somewhat common for saved structures to want references to the guts of a def. We'd need some way to name those interior structures so they can be reliably serialized and deserialized.