# Indexes

Sometimes it's useful to have integer IDs for a class of objects. Dec supports indices both for Decs themselves, and for members within the Dec system.

```cs
public class IndexedDec : Dec.Dec
{
    [Dec.Index] public int index;
}

public class IndexedNonDecClass
{
    [Dec.Index] public int index;
}

// Generate a temporary array with exactly the number of elements as there are IndexedDec's.
int[] tempArray = new int[Index<IndexedDec>.Count];

// Iterate over IndexedDec's and calculate values, storing the results in the temp array.
for (int i = 0; i < Index<IndexedDec>.Count; ++i)
{
    tempArray[i] = Index<IndexedDec>.Get(i).CalculateImportantValue();
}

// Modify them to get final results.
ModifyValuesInSomeWayThatRequiresHavingThemAllAvailable(tempArray);

// Get the highest value out of the temp array, then use Index to look up which dec it refers to.
IndexedDec best = Index<IndexedDec>.Get(IndexOfMax(tempArray));
```

Make an `int` member with the Dec.Index attribute. All instances of that object, whether it be a Dec, a non-Dec class, or a struct, will be given a unique integer index. The full list will also be stored in Index&lt;T&gt;.

These values are not consistent between execution runs - don't use them as part of serializing savegames!

Indices may not be set properly on objects that are created as defaults and that are never explicitly defined in decs. This may be solved in the future; consider this a first revision of this feature.