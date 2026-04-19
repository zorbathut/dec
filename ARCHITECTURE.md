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

## Threading Contract

Dec has a narrow but deliberate threading model. In short:

* **Parser runs are single-threaded, and only one Parser can exist globally at a time.** Everything from constructing a `ParserModular` / `Parser` through `Finish()` - including `AddString` / `AddFile`, `ConfigErrors`, `PostLoad`, and the static-reference distribution step - must happen on one thread with no concurrent Recorder / Database / Composer activity. Parser state (`s_Status`, the static-reference handshake, the per-module accumulators) does not tolerate concurrent use and will not reliably detect violations.
* **Post-parse Recorder and Composer operations are concurrent-safe on independent data.** Once `Parser.Finish()` has returned, `Recorder.Write`, `Recorder.Read`, `Recorder.Clone`, `Recorder.Checksum`, and `Composer.ComposeXml` may be called simultaneously on different data from different threads. The internal caches that grow on-demand during those calls (converter registry, type-hierarchy status, compose / parse caches, shareability caches, clone strategy cache) are all either `ConcurrentDictionary` or one-shot caches whose final contents are published by a single reference assignment. Under contention, those one-shot caches may be wastefully rebuilt, but never corrupted. (Note: this relies on x86/x64 / CLR publication semantics; no issues have been reported on ARM, but the code doesn't insert explicit memory fences, and honestly probably nobody is using ARM right now.)
* **Database reads are concurrent-safe post-parse.** `Database.Get`, `Database<T>.Get`, `Database.List`, `Database<T>.List`, `Index<T>.Get`, `Index<T>.List`, and similar read APIs can be called from any thread. The underlying `Dictionary` / `List` state isn't mutated after parse completes; lazily-built caches like `Database.CachedList` and `Index<T>.IndexArray` may be wastefully rebuilt under contention.
* **Explicit mutation APIs are single-threaded.** `Database.Clear`, `Database.DecLookupEnable`, `Database.DecLookupRegisterCustom`, `Database.DecRegisterForbid`, `Database.Create`, `Database.Delete`, `Database.Rename`, and assignments to `Config.CompatTypeLookup` / `Config.CompatDecLookup` / `Config.UsingNamespaces` / `Config.ConverterFactory` must not run concurrently with any Recorder / Composer / Database-read operation from another thread. These are intended to be used during setup (including inside `ConfigErrors` / `PostLoad`, which run on the Parser thread) or between load / save cycles - not during live multithreaded serialization.
* **User code invoked from inside Record / Convert / PostLoad runs under the same rules as its caller.** Don't call mutation APIs from inside an `IRecordable.Record` method, a `Converter.Record`, or a `PostLoad` - and don't spawn threads from those methods that touch Dec APIs while the calling operation is still in flight. Database reads and other Record calls on disjoint data are fine.

If you need to split serialization work across threads, do the Parser run on one thread, wait for it to finish, then hand the Database off. The usual pattern is "load on startup, write savegames concurrently from worker threads."