# Changelog
All notable changes to this project will be documented in this file.


## [Unreleased]
### Added
* Converters now support generic arguments; they must be converting a generic with the same number of arguments in the same order. This might get changed later.
* Recorder.Clone() function to duplicate objects.

### Improved
* Improved error message when trying to initialize a composite with a string.
* First-time Parser startup speed now faster.

### Fixed
* Missing Mode property on Recorder.Parameters.
* 
### Testing
* Made test harness more durable of errors happening during parsing.
* Fixed a test that was accidentally testing the wrong thing.
* Detect internal errors and report them as true test failures.


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
