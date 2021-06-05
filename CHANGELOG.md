# Changelog
All notable changes to this project will be documented in this file.


## [Unreleased]
### Fixed
Passing derived classes to Recorder.Write() resulted in the wrong type when deserialized.
Misleading error messages when attempting to Record an unsupported derived class.


## [v0.3.0]
### Added
Added Recorder.RecordAsThis(), which allows objects to turn over their entire node to a child object.

### Fixed
Incorrect error message on duplicate decs.

### Improved
10-20% performance improvement for Recorder serialization.

### Breaking
Removed user-facing XML exposure, including Converter.ToXml, Converter.FromXml, and Recorder.Xml. This functionality wasn't being used by anyone I could find, was reducing performance, and was seriously complicating development. There is currently no replacement planned.

### Testing
Add small tests.


## [v0.2.0]
### Added
Implemented HashSet support in both Dec and Record.

### Fixed
Parser.AddDirectory was not reading recursively.
structs could not be created via constructor (which happens if you have a List<> of them.)
A Converter returning an incompatible type would result in a null field even if a valid default existed.
Many things did not serialize properly if stored in `object` fields.

### Improved
Filtered out more non-user assemblies for a minor startup performance gain.

### Breaking
Parser.AddDirectory is now recursive.
StaticReferences will no longer work if your project is named "dec" or "netstandard".

### Documentation
Various small documentation improvements and error message improvements.

### Testing
Significant improvements to Validation testing on containers.
Significant improvements to Dec testing on containers.
Added lots of small tests.


## [v0.1.0]
### Added
Initial release.

 
# Notes

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

This project does not currently adhere to Semantic Versioning.
