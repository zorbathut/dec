# Factories/Component decs

```xml
<MonsterDec decName="Goblin">
    <aiWorker class="GoblinAIWorker">
        <aggressiveness>0.9</aggressiveness>
        <greed>1.6</greed>
    </aiWorker>
</MonsterDec>
```

When we use Types in Decs, we often expect to instantiate those Types on the fly. Perhaps it would make sense to create an actual Factory concept, where you could associate data to the created objects in the manner that seems expected.

On the other hand, Dec is built for global static data, not per-instance data. Perhaps the Factory concept should associate included data with some kind of member-dec structure, then pass that to the created object as a parameter . . . somehow.

It's unclear how this "should" work; solutions I've seen have usually been confusing, insufficient, non-typesafe, or required a lot of boilerplate code, often a combination of those. This page is mostly here to observe that a problem exists and should someday be solved.