# Changelog
All notable changes to this project will be documented in this file.


## [Unreleased]
### Breaking
* Stop ignoring whitespace. I . . . actually don't remember why I did this. It's not necessary for numbers, and it just makes it impossible to store strings with prefix/suffix whitespace, which breaks the guaranteed roundtrip requirements. If you're reading this in to-be-released notes then let me know if it breaks your workflow, otherwise this is probably going live.

### Fixed
* String roundtripping with leading/trailing whitespace.
* Exceptions thrown in Recorder could propagate outside Dec.

## [v0.10.0]
### Added
* Initial support for Recorder backwards-compatibility type replacement.
* Default Godot support for error reporting, though right now it works only if the source is embedded in the project.

### Breaking
* Okay I kinda screwed up the Dec Ref thing. Sorry about that. The system should not have been enabled by default; it turns out this causes a bunch of weird unexpected issues. Dec Refs are now disabled by default, and need to be opted-in per-object in PostLoad(). This is a breaking change, although I suspect if you notice any change in your own project, it's likely to be "oh, that was broken before, and now it isn't".
* Also the interface to opt-in to Dec Refs has changed; look at Database.DecLookupEnable() and Database.DecLookupRegisterCustom().
* This is probably not the final form of this feature. I do promise to include this basic functionality in the future, but the actual shape of it might differ considerably.

### Improved
* Added support for RecordAsThis() after a class tag.
* Special-case behavior to convert byte[] input into a Base64-encoded string in XML.
* Special-case error when hitting a shared-object error while recording an empty Array.
* Preemptive warning messages when recording an object that cannot be instantiated.
* Error messages on fatally malformed XML.
* Performance for cloning small arrays improved significantly (~20% in a semi-realistic test case).
* Performance for cloning generic objects improved somewhat (~10% in a semi-realistic test case).
* Warning message attached to the default Error exception-throw behavior.
* More documentation regarding error log exception behavior.
* Error message for unused text content in XML nodes handled by Recorders.

### Fixed
* Thread contention error when running Dec on multiple threads simultaneously.
* Error when cloning a ConverterString-driven class without a parameterless constructor.
* Parsing a class tag of UnknownClass[] results in an unhandled exception.
* Parsing generics with multirank arrays as parameters.
* Overly verbose output when serializing primitive array types.
* Thrown exception when parsing a generic with an invalid parameter.


## [v0.9.0]
### Added
* Initial implementation of Record.Checksum(), intended for easy and fast comparison of recordable objects.
* Initial implementation of Record.ChecksumDiff(), intended for easy and fast detection of how recordable objects differ.
* Added Intent property to Recorder to indicate Serialization/Cloning/Checksum purpose.
* Official support for nullable value types.

### Breaking
* Fixing "Recorded Types could be stored as Dec Refs if the right type showed up in the Dec system" may result in v0.8.0 savegames being broken, sometimes unexpectedly. If this is a problem for you, let me know and I'll come up with a fix. I admit I'm kinda hoping nobody cares.

### Improved
* Exception messages for ConfigErrors() and PostLoad() now include the Dec name.
* Error messages for trying to clone an object into a different object type.
* Remove unnecessary class tag on Recorder nullable fields.

### Fixed
* Reading recorder files with objects with a missing Converter could result in an unhandled exception.
* Cloning objects with children with missing Converters could result in an unhandled exception.
* Recorded Types could be stored as Dec Refs if the right type showed up in the Dec system.


## [v0.8.0]
### Added
* SetupDependsOnAttribute, which allows you to define ConfigErrors/PostLoad dependencies between Dec classes. This is a prototype and will probably change in the future.
* A general-purpose stable dag evaluator, which is exposed mostly because it's often convenient.
* Recorder.InputContext, which gives contextual diagnostic information useful for reporting Recorder issues.
* Debug-log functions to Context and Recorder, making it easier to report issues.
* The ability to reference class objects contained within Decs. Right now the path generation works only for members, lists, arrays, and values of dictionaries that are input with non-`li` keys; this will probably be improved later.
* Added Database.RegisterLookup() to manually add global lookup references. No guarantee this interface keeps existing in this form. But it's there, and one way or another I'll continue to support similar functionality.

### Breaking
* Dec doesn't guarantee what order Decs are initialized in, and it still doesn't . . . but it was *pretty consistent*, and boy, did the above change seriously scramble the order they tend to get initialized in! If you have load dependencies, even if you don't realize that you do, don't be surprised if stuff breaks. Future versions of Dec might include a Dev Mode that intentionally randomizes load order (within the bounds of dependencies) to help catch these issues.
* PostLoad() no longer runs after *all* ConfigErrors(), but only after its own ConfigError(). This was never guaranteed either but you might have been relying on it. This entire system really needs a revamp. Sorry. Come pester me on Discord if you have a strong opinion on how it should work.
* Rename Dec.InputContext to Dec.Context, since it will now also apply to things that aren't input. Sorry about this one too - it'll take some renaming.
* Split CloneWithAssignmentAttribute into CloneStructPiecewiseAttribute and CloneClassAsSharedRef, because the description of CloneWithAssignmentAttribute was just plain confusing.  I don't think anyone's using Clone besides me. If you are, come talk to me on Discord so I can make sure it's doing what you need.

### Improved
* Error messages regarding Shared/Unshared conflicts now report the actual location of those conflicts. This is a first revision; please give feedback.
* Clone performance on Dictionaries with asymmetrical clone-as-valuelike parameters.
* General Clone performance regarding asymmetrical clone-as-valuelike parameters.

### Made Worse
* The new Path system is probably a performance and memory hit. For now, I'm just living with it; if it's a problem for you, please report it so I can work on it with actual numbers.

### Fixed
* Several Clone pathways that would incorrectly Record objects with a TreatAsValuelike converter.


## [v0.7.1]
### Added
* Proper support for Nullable.
* Bespoke_KeyTypeDict() parameter for Recorder which allows dictionary key fields to be implicitly treated as `class` tags for their values. This will be removed once I've figured out a more general way to support this.

### Fixed
* Type parse cache bug that could cause non-arrays to be incorrectly parsed as arrays.
* Cloning an object with a factory that depends on a non-shared recorded object can, if the stack depth is too deep, result in the non-shared recorded object not yet being ready.
* Decs that are also IRecordable cause errors when referenced in other Decs.
* Security issue: XML documents could cause arbitrary HTTP requests.


## [v0.7.0]
### Added
* New Recorder.WriteSimple()/ReadSimple() modes that produce much simpler XML, at the cost of no references.
* Type parsing now accepts curly braces in place of generic angle braces, in order to make XML easier to write.
* Decs can now include `class` tags, if you need to specify a type that doesn't work as an XML node name.

### Breaking
* Bespoke.IgnoreRecordDuringParserAttribute has been removed in favor of IConditionalRecorder. If you want a version that supports both for ease of porting, check out v0.6.0.

### Improved
* Documentation regarding Recorder load order.

### Fixed
* RecorderEnumerator: List<T>.Enumerator would not properly serialize if the enumerator was empty.
* RecorderEnumerator: Recordable Closures did not serialize properly if they were created in a generic function.

### Testing
* Added a small test.


## [v0.6.0]
### Added
* Added IUserSettings, which is passed throughout the Dec system and can be used to change the behavior of Parser and Recorder.
* Added IConditionalRecorder, which can be used to conditionally record objects.
* CloneWithAssignmentAttribute can now be used to specify that objects can be cloned with basic assignment, speeding up array cloning considerably.
* IPostCloneOriginal/IPostCloneNew interfaces that can be used to perform post-clone operations on objects.
* Converter.TreatAsValuelike() which can be used to treat any type like a valuelike. See documentation for details.

### Breaking
* Ignoring provided fields in a Recorder is now considered a warning. Use the new recorder.Ignore() method to explicitly suppress this warning for a field.
* Extra RecorderEnumerator's RecordableClosures attribute now needs to be applied to the class instead of the function (but you weren't using this yet, right?)

### Obsoleted
* Bespoke.IgnoreRecordDuringParserAttribute has been obsoleted in favor of IConditionalRecorder. It remains in the codebase for now but will be removed in the future.

### Improved
* Major revamp of Recorder.Clone functionality to dramatically improve performance (approx 200x in one real-life test case.)
* Recorder no longer depends on Parser.Finish() to find Converters.
* Better support for parallel operations in multiple threads (but not Parser.)
* Fleshed out an error message.

### Fixed
* Inconsistent behavior in Extra RecorderEnumerator Delegate deserialization. (Hopefully.)
* Improper storage of `this` with Extra RecorderEnumerator iterators.
* Unexpected shrapnel caused by the above two fixes.

### Testing
* Improved consistency of a few tests.


## [v0.5.3]
### Added
* Proper support for multidimensional arrays.

### Possibly Breaking
* Changed default culture from `en-US` to `InvariantCulture`. I don't think this should have any effects given the manual string parsing for Infinity, but I may be missing something; report problems, please.

### Improved
* Error messages for database queries interacting with AbstractAttribute.
* Error message for inappropriately-timed StaticReferences initialization.
* More consistent usage of the Dec.Config culture.
* Support case-insensitive nan/infinity float/double parsing.

### Testing
* Added proper testing for AbstractAttribute.
* Add a self-reference test that might be redundant.
 

## [v0.5.2]
### Added
* Now properly serializes NaN-boxed floats and doubles without loss of information.

### Improved
* Error messages when passing null or empty strings to Parser.
* Project configuration revamp to follow established standards for framework choice.
* Better error reporting for Converter read exceptions.
* Tagged an internal-error message as an internal error.
* Error message when attempting to use a class that doesn't inherit from Dec as a Dec.

### Fixed
* A lot of warnings.
* TypeSerialization.Overloaded test inconsistencies.


## [v0.5.1]
### Notes
* Despite the length of this changelog, this is mostly minor bugfixes and documentation. The "big feature" is an Extra package that I do not recommend using in production until it has a *lot* more work.

### Added
* Converters now support generic arguments; they must be converting a generic with the same number of arguments in the same order. This might get changed later.
* Recorder.Clone() function to duplicate objects.
* Added the first extra package, recorder_enumerator, capable of serializing and deserializing both Linq and user-defined enumerators mid-iteration.
* Added support for Stack and Queue.

### Breaking
* Type parsing system no longer supports + separator for nested types, but it never wrote these in the first place, so this shouldn't be a big problem. If you have to edit some xml files, sorry 'bout that, this was an oversight.

### Improved
* Improved error message when trying to initialize a composite with a string.
* First-time Parser startup speed now faster.
* Type parsing system now properly supports generic nested types of generics.
* User-types now don't include anything under the Dec namespace.

### Fixed
* Type caching system no longer results in silent errors.
* Missing Mode property on Recorder.Parameters.
* `bool` not recognized as a primitive type.

### Testing
* Made test harness more durable regarding errors happening during parsing.
* Fixed a test that was accidentally testing the wrong thing.
* Detect internal errors and report them as true test failures, even if a failure was expected.
* Added an integration-unified test for the case where people are just copypasting source files into their project.
* Improved testing of locale issues.
* Reduced compiler warnings in test suite.


## [v0.5.0]
### Added
* ParserModular, a more complicated parser that supports game mods.
* A *large* number of new merge modes intended for game modding.

### Breaking
* Minor changes to Parser API to cleanly support more file types.

### Documentation
* Picked a more modern and less awkwardly stifling visual style.

### Fixed
* Minor errors in several error messages.
* Comment typos in unit tests.

### Testing
* Added internal validation checks.
* Better coverage in various areas.


## [v0.4.0]
### Breaking
* Recorder no longer shares class references by default. The new `.Shared()` recorder decorator can be used to allow this, although it will error on non-null defaults.
* Converter has been split into ConverterString, ConverterRecord, and ConverterFactory.

### Improved
* RecordAsThis() does not (and currently cannot) work on a polymorphic object; document this and explicitly report it as an error.

### Changed
* Recorder now defaults to not doing pretty-print.

### Fixed
* Failed ref instantiations can later cause unhandled exceptions.
* Recorder breaks when trying to deal with multiply referenced Array objects.
* Various problems with both Visual Studio compatibility and VS Code compatibility.
* Fixed a variety of obscure errors with shared objects.

### Documentation
* Fixed typos.
* Suppressed index pages for things that aren't really meant to be used.
* Cleaned up example of InputContext.
 
### Testing
* Better diagnostic output on missing expected errors.
* Added more thorough tests for sharing.


## [v0.3.5]
### Added
* Added `Bespoke.IgnoreRecordDuringParserAttribute`, which provides special-purpose functionality that will be rolled into an eventual Converter redesign.
* Added system-wide CultureInfo support so your games stop breaking in France.

### Improved
* Better text quoting among many error messages.
* Dec.ToString() now returns type data as well.
* Better error message if attempting to Record a never-registered Dec.
* Better error messages and recovery on errors with `key` and `value` elements in <li> Dictionary lists.
* Better error message for capitalization in <li> tags.

### Fixed
* Parser.LoadDirectory's prefix filtering breaks if given a directory that includes `.` or `..`.
* RecordAsThis documentation refers to code that is no longer an example.

### Documentation
* Fixed broken relative links.
* Fixed commandline parameter that docfx originally misspelled and finally dropped support for.

### Testing
* Added tests for a few edge cases (none of which were actually broken, as it turned out.)


## [v0.3.4]
### Fixed
* Recording a referenced object that would otherwise need a class tag results in an invalid (but still readable) file.
* Errors when multiple Decs inherit from a Dec with attributes on its members.
* Documentation crosslink error.

### Improved
* Better error message when failing to find a Dec.
* Better error messages when failing to load StaticReferences.
* Better error message when parsing a Dec without a decName.

### Changed
* Private constructors are now considered valid to use.

### Documentation
* Fix incorrect tl;dr instructions.
* Improve tl;dr instructions.
* Connect the Inheritance documentation to the Merge Mode documentation.
* Rewrite Index documentation.
* Added clarity regarding the interaction between NonSerialized and Recorder.

### Breaking
* Parser.LoadDirectory no longer loads files or directories with . prefixes.


## [v0.3.3]
### Added
* Added a `mode` attribute that lets you specify how things are updated through inheritance.

### Fixed
* Internal parser error when encountering refs in Parser XML.

### Improved
* Proper support for fields with abstract Dec types.
* Slight improvement to error handling behavior when dealing with a ref tag in Parser input.

### Documentation
* Various small error message improvements.

### Testing
* Minor test cleanup and augmentation.
* Improved code coverage.


## [v0.3.2]
### Added
* Added an optional factory callback for Recorder functionality, allowing contextual default options for deserialization.
* Added native Tuple and ValueTuple support.

### Documentation
* Various small documentation improvements and error message improvements.

### Breaking
* Removed the Null writer, which was intended for benchmarking and testing but which was a giant pain and frankly not worth the effort.

### Testing
* Fix several test bugs that could generate false passes (but weren't.)


## [v0.3.1]
### Fixed
* Passing derived classes to Recorder.Write() resulted in the wrong type when deserialized.
* Misleading error messages when attempting to Record an unsupported derived class.
* Converter detection fails if applied to an unsupported base class with a supported derived class.


## [v0.3.0]
### Added
* Added Recorder.RecordAsThis(), which allows objects to turn over their entire node to a child object.

### Fixed
* Incorrect error message on duplicate decs.

### Improved
* 10-20% performance improvement for Recorder serialization.

### Breaking
* Removed user-facing XML exposure, including Converter.ToXml, Converter.FromXml, and Recorder.Xml. This functionality wasn't being used by anyone I could find, was reducing performance, and was seriously complicating development. There is currently no replacement planned.

### Testing
* Add small tests.


## [v0.2.0]
### Added
* Implemented HashSet support in both Dec and Record.

### Fixed
* Parser.AddDirectory was not reading recursively.
* structs could not be created via constructor (which happens if you have a List<> of them.)
* A Converter returning an incompatible type would result in a null field even if a valid default existed.
* Many things did not serialize properly if stored in `object` fields.

### Improved
* Filtered out more non-user assemblies for a minor startup performance gain.

### Breaking
* Parser.AddDirectory is now recursive.
* StaticReferences will no longer work if your project is named "dec" or "netstandard".

### Documentation
* Various small documentation improvements and error message improvements.

### Testing
* Significant improvements to Validation testing on containers.
* Significant improvements to Dec testing on containers.
* Added lots of small tests.


## [v0.1.0]
### Added
* Initial release.

 
# Notes

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

This project does not currently adhere to Semantic Versioning.
