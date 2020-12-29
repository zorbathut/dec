# Changelog
All notable changes to this project will be documented in this file.


## [Unreleased]
### Added
Implemented HashSet support in both Dec and Record.

### Fixed
Parser.AddDirectory was not reading recursively.
structs could not be created via constructor (which happens if you have a List<> of them.)
A Converter returning an incompatible type would result in a null field even if a valid default existed.

### Improved
Filtered out more non-user assemblies for a minor startup performance gain.

### Breaking
Parser.AddDirectory is now recursive.
StaticReferences will no longer work if your project is named "dec" or "netstandard".

### Documentation
Various small documentation improvements and error message improvements.

### Testing
Added lots of small tests.


## [v0.1.0]
### Added
Initial release.

 
# Notes

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

This project does not currently adhere to Semantic Versioning.
