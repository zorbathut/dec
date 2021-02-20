This file is a work in progress.

## Core Serialization Concepts

The brains of dec is the Serialization class. ParseElement is the deserialization core, which takes input data and turns it into objects. ComposeElement is the serialization core, which takes objects and turns it into output data.

"Input data" is currently defined as "XML", but will probably turn into "calls to something inherited from ReaderNode". "Output data" is defined as "calls to something inherited from WriterNode". In both cases, the Node is expected to do whatever internal processing is necessary, handle whatever primitives can be handled, and then call right back to ParseElement/ComposeElement to deal with the details.

As an example, if attempting to write a List<HashSet<SomeClass>>, you'll get a call series that looks like this:

* Serialization.ComposeElement() - Determines that this is a List<> of some kind, then calls:
* WriterNode.WriteList() - Does necessary handling (figures out the expected types and creates a child node, perhaps), then for each element, calls:
* Serialization.ComposeElement() - Determines that this is a HashSet<> of some kind, then calls:
* WriterNode.WriteHashSet() - Does necessary handling (figures out the expected types and creates a child node, perhaps), then for each element, calls:
* Serialization.ComposeElement() - Determines that this is a SomeClass or something derived from it. Calls WriterNode.TagClass() if the underlying node needs to be stored, then for each member, calls WriterNode.CreateMember() to create the child, and then Serialization.ComposeElement() to actually write the data.

This all gets extra-complicated if an IRecordable or Converter is involved. In this case, there's an extra step where a RecorderWriter is created.

* Serialization.ComposeElement() - Determines that this is a Convertible of some kind, then calls:
* WriterNode.WriteConvertible() - Creates a new RecorderWriter context object, then calls:
* Converter.Record() - By default, calls FromXml() or FromString() as appropriate, but it's more interesting if this has been overloaded by the user, in which case it will, for each field the user cares about, call:
* RecorderWriter.Record() - Does a small amount of bookkeping and validation, then calls:
* Serialization.ComposeElement() - Handles whatever the user is trying to serialize (potentially going right back to WriterNode.WriteConvertible()).

If writing highly-self-referential savegames via the Record system, this can potentially recurse so much that it blows the stack. This is solved by, upon calling WriterNode.WriteConvertible() or WriterNode.WriteRecord(), potentially queueing up the write into a global list that can be done later - when the stack is lower - instead of finishing it immediately.