# Justifications

## Text formats

I suspect it's only a matter of time before someone asks me why I'm using XML.

XML is definitely not perfect, but it has a set of features which I want that other text-based data descriptors don't have. I'll list alternatives and explain why they fall short.

### JSON

JSON is designed as a computer-to-computer descriptive language, not a human-facing written language. The big dealbreaker here is a lack of comments, which are near-crucial for large human-authored files, but there's other human-antifeatures like a lack of trailing commas in lists.

On top of that, it's more typed than I prefer; I'd like to keep typing data in exactly one place, and that's the C# class definitions. JSON understands strings, integers, booleans, lists, and dictionaries, and that's about three more things than I want it to know about.

### JSON5/JSONC

JSON5 and JSONC are basically "JSON, with trailing commas and comments". They're definitely better than JSON, but support is lacking; as of today there's a single C# implementation with almost no usage and less documentation than dec. They also share the typing issues of JSON.

### YAML

YAML includes comments and better human-intended features, but has worse typing issues than JSON, with many documented cases where naive but reasonable input results in completely incorrect data. YAML is also a whitespace-sensitive language; I admit I'm not a fan of that in general, but in my experience it's devastating for data markup languages, where it's common to have *extremely* large blocks containing a lot of information.

### StrictYAML

Similar to the JSON5/JSON comparison, StrictYAML is a modified version of YAML, mostly removing YAML's weird typing behavior. I actually rather like StrictYAML and use it on other projects; however, the only StrictYAML parser that exists today is in Python.

It shares the whitespace-sensitive behavior of YAML.

### .ini

Classic Windows INIs are an untyped configuration language. They're actually rather nice for simple configurations, but they don't have native support for nested structures, which makes them somewhat unusable for this application.

### TOML

TOML is basically a better .ini. It's great for simple configurations (or even slightly-less-simple configurations) but is not suited for deeply nested data.

### Custom-written format

I'm not a big fan of reinventing the wheel, especially when I have something that's Good Enough. Why bother when I can just use XML?