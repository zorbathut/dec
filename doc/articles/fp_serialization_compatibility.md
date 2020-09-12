# Serialization: Compatibility

Save formats inevitably have to be changed with newer versions of the software. One way or another, we'll need an elegant way to handle save layout changes. This should in theory be able to easily update through multiple incompatible revisions.

A brief list of things we'll want to provide:

* Def renaming, to deal with defs that have changed name from one version to another
* Class renaming, to deal with classes that have changed name from one version to another
* Some form of data updating, possibly with externally registered Converter-esque update classes