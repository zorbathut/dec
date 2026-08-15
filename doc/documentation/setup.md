# Setup functions

After `Parser.Finish()` loads all your decs and fills static references, it runs a setup pass; `Recorder.Read` and `Recorder.ReadSimple` run the same pass on the objects they load. The setup functions are standard functions tagged with the `[Dec.Setup]` attribute, which can go on methods of any class and gives you explicit control over ordering and parallelism. (The older `ConfigErrors`/`PostLoad` overrides on dec classes are deprecated predecessors of this; they still work and run as part of the same graph, but new code should use `[Dec.Setup]`.)

```cs
public class MonsterDec : Dec.Dec
{
    [NonSerialized] public float[] blessingChance;

    [Dec.Setup]
    private void CalculateBlessings(Action<string> reporter)
    {
        blessingChance = new float[Index<BlessingDec>.Count];
        for (int i = 0; i < Index<BlessingDec>.Count; ++i)
        {
            blessingChance[i] = CalculateBlessingChance(this, Index<BlessingDec>.Get(i));
        }
    }
}
```

A setup function is either an instance method, which runs once for every instance of its class that a load created or encountered, or a static method, which runs exactly once per parser load. Instance setup functions work on non-Dec classes too - if your decs contain component objects, the components' setup functions run for every component the parser created, and the same goes for objects inside a savegame when it's read back. Static setup functions are good for building cross-instance lookup tables.

The method signature must be `void M(Action<string> reporter)`; call the reporter to report configuration errors attributed to the instance being processed. Setup functions aren't supported on structs.

## Ordering

By default, setup functions run in an unspecified (but deterministic) order. To constrain the order, use `[Dec.SetupAfter]` and `[Dec.SetupBefore]`:

```cs
public class RoomDec : Dec.Dec
{
    // Runs after every setup function on ItemDec
    [Dec.Setup]
    [Dec.SetupAfter(typeof(ItemDec))]
    private void PlaceItems(Action<string> reporter) { /* ... */ }

    // Or reference one specific function.
    [Dec.SetupAfter(typeof(DoorDec), nameof(DoorDec.BuildIndex))]
    [Dec.Setup]
    private void ConnectDoors(Action<string> reporter) { /* ... */ }
}
```

Referencing a bare type means after every setup function the type has, declared on it or inherited into it, as it runs on instances of the type and all of its subclasses. Setup functions *introduced by* subclasses are not included. If you mean "after everything inheriting from this type is fully ready", use `IncludeDerived`:

```cs
// Runs after every setup function on ItemDec and everything derived from it
[Dec.Setup]
[Dec.SetupAfter(typeof(ItemDec), IncludeDerived = true)]
private void BuildItemCatalog(Action<string> reporter) { /* ... */ }
```

Referencing a type plus a member name targets that one function, which must belong to the referenced type's own setup.

Because subclass-introduced functions are outside the base's own setup, a subclass can order its additions relative to its ancestor: a setup function introduced on a derived class can declare `[Dec.SetupAfter(typeof(BaseClass))]` and run after the inherited functions, on every instance of the hierarchy including its own.

Both attributes can also go on a class or interface, where they constrain every setup function belonging to that stage. `[Dec.SetupBefore]` is particularly useful in mods, where you can order your setup ahead of a class you can't edit.

All constraints are combined into one dependency graph and executed in a stable topological order. Cycles are reported as errors and broken arbitrarily.

## Stages

When several unrelated functions form a phase that others need to order against, referencing each function individually gets brittle - it turns private method names into a contract. Instead, declare a stage: any class acts as a named rendezvous point.

```cs
public static class SetupStage
{
    public class IndexesBuilt { }
}

public class ItemDec : Dec.Dec
{
    [Dec.Setup(Stage = typeof(SetupStage.IndexesBuilt))]
    private void BuildIndex(Action<string> reporter) { /* ... */ }
}

public class RoomDec : Dec.Dec
{
    // waits for every member of the stage, without naming any of them
    [Dec.Setup]
    [Dec.SetupAfter(typeof(SetupStage.IndexesBuilt))]
    private void PlaceItems(Action<string> reporter) { /* ... */ }
}
```

A stage marker class can itself carry `[Dec.SetupAfter]`/`[Dec.SetupBefore]` attributes, which apply to all of its members at once.

## Parallelism

An instance setup function can opt into running across its instances on multiple threads:

```cs
[Dec.Setup(Parallel = true)]
private void ExpensivePrecalculation(Action<string> reporter) { /* ... */ }
```

Rules for parallel setup functions: don't call Dec's database mutation APIs, and don't touch shared mutable state without your own synchronization. Reporter output and any thrown exceptions are reported as they happen, from whatever thread the instance ran on - your error handlers must be threadsafe if you use any of Dec's threaded features, and report order across instances isn't deterministic. An error handler that throws won't stop the other instances from running. See ARCHITECTURE.md's threading contract for the full details.

## Boundaries worth knowing

* Instance collection happens while a load is traversing data. An object created in a constructor or field initializer that's never mentioned in the XML is invisible to the load and won't get setup functions run on it - the same boundary Indexes have. Like Indexes, this may change later - don't rely on this behavior! If you need full traversal, let me know on Discord.
* Instance setup functions run at the end of `Parser.Finish()`, `Recorder.Read`, and `Recorder.ReadSimple`, on whatever thread you called from. `Recorder.Clone` doesn't trigger them, and neither does creating decs at runtime through `Database.Create`. Static setup functions run only from `Parser.Finish()`.
* Decs and dec-owned objects referenced by a savegame keep their parse-time setup; reading a savegame never re-runs setup on them. Ordering constraints naming types with nothing present in a given load are silently satisfied. Declaration mistakes (bad signatures and the like) on savegame-only types surface as errors when a savegame first loads them.
* If you call `Recorder.Read` concurrently from multiple threads, your setup functions can run concurrently with themselves on different instances; keep them safe for that, or don't read concurrently.
