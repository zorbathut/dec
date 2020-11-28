# Serialization: Interior save links

It's somewhat common for saved structures to want references to the guts of a dec. We'd need some way to name those interior structures so they can be reliably serialized and deserialized. In most cases, this can be done automatically by the path of the child object, but list or array paths tend to be unstable and we'd want to provide explicit sub-object naming of some kind.

This will potentially turn into a nasty hairball if we ever support non-tree structures in decs themselves.