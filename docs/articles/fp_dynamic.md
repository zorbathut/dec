# Dynamic def creation

Creating new defs at runtime is rarely the technically-best solution, but sometimes it's a whole lot easier than the "best" solution, and game development is all about tradeoffs.

We'll need to provide a point in the setup process where a callback can return code-generated defs. This will need to go through extensive validation to ensure that we're not overriding defNames. In addition, it's currently unclear how this should work relative to StaticReferences; certainly many StaticReferences will be necessary for building defs, but then we'll need to evaluate StaticReferences again to link those defs in. In fact, we may need to do this several times.

Doing this efficiently isn't trivial, and coming up with an interface for it is similarly awkward. This feature is likely to wait until someone actually needs it. (If that's you, please let me know!)