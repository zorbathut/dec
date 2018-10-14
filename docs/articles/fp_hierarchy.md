# Def hierarchy control

While the current def hierarchy behavior is simple and easy to describe - "every direct subclass of Def is its own hierarchy" - it's not always desired behavior. Imagine if someone wants to make an abstract NamedObjectDef class. With the current implementation, that would become the root of a hierarchy, and everything named would become a child in that hierarchy.

We could in theory say that abstract def classes are never hierarchy roots, but this isn't necessarily desired either; it's reasonable to *intentionally* make an abstract def class intended as the root of a def hierarchy.

The right solution is to provide an attribute to control what counts as a def hierarchy root and what doesn't.