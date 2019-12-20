# Indexes

It's occasionally convenient for defs to have an integer index associated with them, where the index among a set of defs is between [0, defCount). This is mostly handy for doing array lookups; array lookups are much faster than a Dictionary<Def, object> lookup.

The problem is that, in a complex hierarchy, it's not always obvious what type to associate the index with. Choosing a type too close to Def.Def itself results in an array with gaps, choosing a type too far away results in an array that doesn't contain everything necessary. This problem gets much worse with def hierarchy control.

As a much smaller concern, most types will never have their index used, and that's just wasted memory. This is not really relevant though because there just aren't that many defs in most systems.

The right solution is to add an Index Attribute that lets the def creator specify where indices need to be created.

I'll get to this someday.