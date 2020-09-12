# Indexes

Sometimes it's useful to have integer IDs for a class of objects. Def supports indices both for Defs themselves, and for members within the Def system.

```cs
public class IndexedDef : Def.Def
{
    [Def.Index] public int index;
}

public class IndexedNonDefClass
{
    [Def.Index] public int index;
}

// Generate a temporary array with exactly the number of elements as there are IndexedDef's.
int[] tempArray = new int[Index<IndexedDef>.Count];

// Iterate over IndexedDef's and calculate values, storing the results in the temp array.
for (int i = 0; i < Index<IndexedDef>.Count; ++i)
{
    tempArray[i] = Index<IndexedDef>.Get(i).CalculateImportantValue();
}

// Modify them to get final results.
ModifyValuesInSomeWayThatRequiresHavingThemAllAvailable(tempArray);

// Get the highest value out of the temp array, then use Index to look up which def it refers to.
IndexedDef best = Index<IndexedDef>.Get(IndexOfMax(tempArray));
```

Make an `int` member with the Def.Index attribute. All instances of that object, whether it be a Def, a non-Def class, or a struct, will be given a unique integer index. The full list will also be stored in Index&lt;T&gt;.

These values are not consistent between execution runs - don't use them as part of serializing savegames!

Indices may not be set properly on objects that are created as defaults and that are never explicitly defined in defs. This may be solved in the future; consider this a first revision of this feature.