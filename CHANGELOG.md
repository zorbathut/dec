# Changelog
All notable changes to this project will be documented in this file.


## [Unreleased]
### Fixed
Parser.AddDirectory was not reading recursively.
structs could not be created via constructor (which happens if you have a List<> of them.)

### Changed
If you relied on Parser.AddDirectory not being recursive, your code will break.

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
