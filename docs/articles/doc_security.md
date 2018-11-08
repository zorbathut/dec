# Security

Def is currently not intended to be secure in any way. It is expected to be used in an environment where all pieces - the assemblies running, the function calls made on it, and the XML data passed in - are trusted. If you want to use it in a partially-untrusted environment I strongly recommend doing a security audit beforehand.

That said, it shouldn't be unnecessarily insecure. Most of what it does is text parsing, and aside from obvious things like a malicious attacker sending a literally infinite XML document, it should generally not be exploitable in any way. Even though security is not a goal of this project, exploits that can be fixed without removing features are considered bugs.

Some things an attacker can do that you may wish to be aware of:

* By choosing which class to create, an attacker may be ale to create classes of any type that inherit from types used as members in Defs. This will cause the default constructor to be executed. This can be mitigated by initializing all class members in Defs to non-null values; by modifying source, one could mostly remove the ability of this library to create new classes.
* This also applies to Defs themselves. Any Def-inheriting class can be instantiated. This is harder to remove without eliminating necessary functionality.
* It's common for Defs to include Type members, usually as a factory pattern to create objects with complicated behavior. Type members are entirely unconstrained and can refer to any type, even types without the expected base class. This is best mitigated by implementing [type constraints](fp_typeconstraints.md).
* Def will happily allow people to change both public and private members, including private members in base classes. This behavior is not always expected and can easily put objects in internally inconsistent states. This issue can be mitigated by implementing [the NonSerialized attribute](fp_nonserialized.md).

If you think of another possible attack, please let me know and I'll add it to this list.