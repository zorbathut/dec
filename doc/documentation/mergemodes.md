# Merge Modes

## Introduction

Frequently, when using [inheritance](inheritance.md), you'll want a behavior for updating collections that is not simple replacement. Imagine the following structure in a component-based system:

```xml
<EntityDec decName="Animal" abstract="true">
  <intelligence>Animal</intelligence>
  <components>
    <li class="Component.WorldPosition" />
    <li class="Component.HealthBehavior">
      <type>Biological</type>
    </li>
  </components>
</EntityDec>

<EntityDec decName="Squirrel" parent="Animal">
  <components>
    <li class="Component.Brain">
      <type>Herbivore</type>
    </li>
  </components>
  <drops>
    <SquirrelPelt>1</SquirrelPelt>
    <Meat>2</Meat>
  </drops>
</EntityDec>
```

This seems like a good idea - animals have a position and biological health behavior, and then you just add a brain to the squirrel, right?

But because `components` is a `List<Component.Base>` in this example, this will, by default, erase the vector in inherited classes. Squirrels will end up not having a world position or health behavior.

## Example

This can be solved using `mode` attributes. Example:

```xml
<EntityDec decName="Squirrel" parent="Animal">
  <components mode="append">
    <li class="Component.Brain">
      <type>Herbivore</type>
    </li>
  </components>
  <drops>
    <SquirrelPelt>1</SquirrelPelt>
    <Meat>2</Meat>
  </drops>
</EntityDec>
```

The `mode="append"` tag on `components` indicates that this shouldn't replace existing elements in a list, but rather append new elements to that list. This will have to be specified in each EntityDec that inherits from `Animal` - for clarity's sake, it's specified on the child, not the parent.

## Containers and their Valid Modes

`List`: The default `replace` will clear the collection, then add new elements to it. `append` will append new elements to the end of an existing list.

`Dictionary`/`HashSet`: The default `replace` will clear the collection, then add new elements to it. `patch` will add new items and silently replace any items with an existing key. `append` will add new items, but error if any existing keys are re-used.

More options will be added in the future; if you need something specific, please ask on Discord, it may already be planned.