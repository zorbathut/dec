# Merge Modes

## Introduction

Dec now supports two different ways of combining Decs, [inheritance](inheritance.md) and [modules](modding.md). In both cases the Dec writer is likely to need to override or modify existing properties. Control over this can be complicated; there isn't a single default behavior that works in all cases.

Imagine a developer who wants to take an existing bronze sword:

```xml
<WeaponDec decName="BronzeSword">
  <damage>1</damage>
  <damageType>Sharp</damageType>
  <materials><Bronze>10</Bronze></materials>
</WeaponDec>
```

and use it to make a chainsaw:

```xml
<WeaponDec decName="Chainsaw" parent="BronzeSword">
  <damage>10</damage>
  <materials><Steel>10</Steel></materials>
</WeaponDec>
```

This example introduces questions. Have we replaced the Bronze with Steel, or have we merely added some steel? Does it still do Sharp damage?

We run into further potential problems with a component-based system and inheritance:

```xml
<CreatureDec decName="Animal" abstract="true">
  <intelligence>Animal</intelligence>

  <components>
    <li class="Component.WorldPosition" />
    <li class="Component.HealthBehavior">
      <type>Biological</type>
    </li>
  </components>

  <drops>
    <Meat>5</Meat>
    <AnimalGuts>1</AnimalGuts>
  </drops>
</CreatureDec>

<CreatureDec decName="Squirrel" parent="Animal">
  <components>
    <li class="Component.Brain">
      <type>Herbivore</type>
    </li>
  </components>

  <drops>
    <Fur>1</Fur>
    <Meat>2</Meat>
  </drops>
</CreatureDec>
```

This seems like a good idea - animals have a position and biological health behavior, and then you just add an appropriate brain to the squirrel, right? But now we're relying on the list to append the new component, while in our previous example we expected the Bronze to be replaced with Steel.

Dec's solution is to fall back on reasonable defaults that are also designed to fail noisily when mistakes are made, and allow `mode=` tags to override those defaults.

Dec supports three standard merge modes to help issues of this sort: `replace`, `patch`, and `append`.

## Standard Merge Modes

### replace

`replace` is the default merge mode for values and collections. It clears the existing element and replaces it from scratch. A list with existing elements will be rebuilt from empty.

```xml
<WeaponDec decName="IronSword" parent="BronzeSword">
  <!-- replace the weapon damage -->
  <damage>4</damage>

  <!-- replace the material requirements; Bronze is removed entirely -->
  <materials><Iron>10</Iron></materials>
</WeaponDec>
```

### append

`append` is valid only for collections. For List-style collections, it adds more elements to the end; for HashSets or Dictionaries, it adds new elements to the container, but key collisions are an error.

```xml
<CreatureDec decName="Rabbit" parent="Animal">
  <!-- add a new Brain component to the end of the array -->
  <components mode="append">
    <li class="Component.Brain">
      <type>Herbivore</type>
    </li>
  </components>

  <!-- standard animal drops, but add a rabbit pelt -->
  <drops mode="append">
    <RabbitPelt>1</RabbitPelt>
  </drops>
</CreatureDec>
```

### patch

`patch` is the default merge mode for dec inheritance, classes, and structs, and valid for associative containers such as Dictionary or HashSet. It leaves existing elements in place but overrides the existing items with new items.

```xml
<CreatureDec decName="Cow" parent="Herbivore">
  <!-- Cows are big and should drop a lot of meat, but leave the rest of the drops alone. -->
  <drops mode="patch">
    <Meat>40</Meat>
  </drops>
</CreatureDec>
```

## Dec Module Merge Modes

As mentioned above, both inheritance and modules allow some form of compositing. Inheritance itself, however, is *always* done with `patch` semantics. (`append` isn't valid for Decs, and if you wanted `replace`, you could accomplish that by just not doing inheritance!)

The above three modes work fine for most data types, but Decs themselves are significantly more complicated. Mods may want to create new Decs, modify existing Decs, replace Decs entirely, or even simply delete them.

### Common Merge Modes

These four merge modes will solve most of your problems and should be used when possible.

#### `create`

Decs default to the `create` mode, which creates a Dec with a new name.

It's an error to `create` a dec that already exists; this is to help detect conflicts between mods that may be trying to use the same Dec name.

#### `patch`

This lets you modify individual fields in an already-existing Dec. This is by far the most common merge mode to use explicitly; if you're trying to figure out how to modify core game data, this is it!

It's an error to `patch` a Dec that doesn't exist.

#### `replace`

Similar to the non-Dec `replace`, this erases a Dec entirely and starts over from scratch.

It's an error to `replace` a Dec that doesn't exist.

#### `delete`

This allows you to remove a Dec from the game entirely.

It's an error to `delete` a Dec that never existed. If one module creates a Dec, it can be deleted by any number of following modules.

### Hybrid Merge Modes

Sometimes a mod will find itself needing to modify *another* mod, or will be in a situation where the exact Decs available aren't perfectly known. These merge modes are designed for those purposes. They should generally be avoided unless needed.

#### `createOrReplace`

Creates a Dec if it doesn't exist; replaces a Dec if it does exist.

Useful if you want to ensure that your definition is the one that is used no matter what.

#### `createOrPatch`

Creates a Dec if it doesn't exist; patches a Dec if it does exist.

Useful if you would leave some fields as default values, and want previously-loaded mods to be able to set those.

#### `createOrIgnore`

Creates a Dec if it doesn't exist. Does nothing if it already does exist.

Useful if you're trying to create a fallback option for Decs that would be generated inside a mod that may or may not be loaded.

#### `replaceIfExists`

Replaces a Dec if it already exists; does nothing if it doesn't exist.

Useful if you need to significantly change the behavior of a mod that may or may not be loaded.

#### `patchIfExists`

Patches a Dec if it already exists; does nothing if it doesn't exist.

Useful if you're trying to make minor adjustments to a mod that may or may not be loaded.

#### `deleteIfExists`

Deletes a Dec if it exists; does nothing if it doesn't exist.

Useful if you're trying to remove items from a mod that may or may not be loaded.

## Full Behavior Reference Table

Empty spaces are errors; green spaces are default behavior.

<table>
  <tr>
    <td>Merge mode</td>
    <td>Dec (new)</td>
    <td>Dec (existing)</td>
    <td>class/struct</td>
    <td>List&lt;&gt;</td>
    <td>Dictionary&lt;&gt;/<wbr/>HashSet&lt;&gt;</td>
    <td>Value (int, string, etc)</td>
  </tr>
  <tr>
    <td><pre>replace</pre></td>
    <td></td>
    <td>
        Created from constructor.
    </td>
    <td></td>
    <td class="mergeModeDefault">Erased and replaced with new data.</td>
    <td class="mergeModeDefault">Erased and replaced with new data.</td>
    <td class="mergeModeDefault">Replaced with new data.</td>
  </tr>
  <tr>
    <td><pre>append</pre></td>
    <td></td>
    <td></td>
    <td></td>
    <td>
        New data appended to end of list.
    </td>
    <td>
        New elements added by key.
        Error if a key already exists.
    </td>
    <td></td>
  </tr>
  <tr>
    <td><pre>patch</pre></td>
    <td></td>
    <td>
        Individual elements replaced by name.
    </td>
    <td class="mergeModeDefault">
        Individual elements replaced by name.
    </td>
    <td></td>
    <td>
        Individual elements replaced by key.
    </td>
    <td></td>
  </tr>
  <tr>
    <td><pre>create</pre></td>
    <td class="mergeModeDefault">
        Created from constructor.
    </td>
    <td class="mergeModeDefault"></td>
    <td></td>
    <td></td>
    <td></td>
    <td></td>
  </tr>
  <tr>
    <td><pre>delete</pre></td>
    <td>
        If the Dec previously existed, do nothing. Otherwise, error.
    </td>
    <td>
        Deleted from database.
    </td>
    <td></td>
    <td></td>
    <td></td>
    <td></td>
  </tr>
  <tr>
    <td><pre>createOrReplace</pre></td>
    <td>
        Created from constructor.
    </td>
    <td>
        Created from constructor.
    </td>
    <td></td>
    <td></td>
    <td></td>
    <td></td>
  </tr>
  <tr>
    <td><pre>createOrPatch</pre></td>
    <td>
        Created from constructor.
    </td>
    <td>
        Individual elements replaced by name.
    </td>
    <td></td>
    <td></td>
    <td></td>
    <td></td>
  </tr>
  <tr>
    <td><pre>createOrIgnore</pre></td>
    <td>
        Created from constructor.
    </td>
    <td>
        Do nothing.
    </td>
    <td></td>
    <td></td>
    <td></td>
    <td></td>
  </tr>
  <tr>
    <td><pre>replaceIfExists</pre></td>
    <td>
        Do nothing.
    </td>
    <td>
        Created from constructor.
    </td>
    <td></td>
    <td></td>
    <td></td>
    <td></td>
  </tr>
  <tr>
    <td><pre>patchIfExists</pre></td>
    <td>
        Do nothing.
    </td>
    <td>
        Individual elements replaced by name.
    </td>
    <td></td>
    <td></td>
    <td></td>
    <td></td>
  </tr>
  <tr>
    <td><pre>deleteIfExists</pre></td>
    <td>
        Do nothing.
    </td>
    <td>
        Deleted from database.
    </td>
    <td></td>
    <td></td>
    <td></td>
    <td></td>
  </tr>
</table>


## Subtleties

For the sake of local comprehension, **these tags apply only to a single element**; they do not persist through children, the parsing immediately reverts back to reasonable defaults. Setting a `patch` mode does not mean that all children inherit `patch`! They revert to their default behavior immediately.

```xml
<CreatureDec decName="Rabbit" mode="patch">
  <!-- After this module, the modded rabbit now drops fur, guts, a rabbit pelt, *and* a holy hand grenade -->
  <!-- Without the mode tag, this would revert back to `replace`, and the rabbit would drop only the hand grenade -->
  <drops mode="patch">
    <HolyHandGrenade>1</HolyHandGrenade>
  </drops>
</CreatureDec>
```

## Epilogue

More options may be added in the future; current possibilities include `prepend`, `patchToConstructor`, `patchToOriginal`, `appendToOriginal`, and the ability to use Dec merge modes for Dictionary/HashSet keys. If you need something specific, please ask on Discord.

The patch system is not likely to become turing-complete; if you need functionality that includes conditionals, loops, or matches, it is *strongly* recommended to just do it in C#.
