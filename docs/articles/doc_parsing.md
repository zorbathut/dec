# Member parsing

Many types can be parsed natively by def. This page lists them all.

Note that more types and formats may work than this page lists, but if it's not listed here, it's not officially supported.

## Numeric types

`short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double`: All of these parse from text representations in the way you'd expect, deferring to the C# implementation.

## Strings

`string` is read verbatim as a string.

## Booleans

`bool` should be entered as either `true` or `false`.

## Enums

All enums can be parsed by entering the name of the enum token.

```cs
enum Animals
{
  Cat,
  Dog,
  Fish,
}
```
```xml
<AnimalDef defName="MaineCoon">
  <type>Cat</type>
</AnimalDef>
```

## Lists

Lists are entered with &lt;li&gt; sequences.

```xml
<NumberListDef defName="Primes">
  <numbers>
    <li>2</li>
    <li>3</li>
    <li>5</li>
    <li>7</li>
    <li>11</li>
    <li>13</li>
    <li>17</li>
  </numbers>
</NumberListDef>
```

## Dictionaries

Dictionaries are normally defined using XML tags as keys.

```xml
<MaterialValueDef defName="Medieval">
  <materials>
    <Iron>1</Iron>
    <Steel>3</Steel>
    <Uranium>0.1</Uranium>
    <Aluminum>20</Aluminum>
  </materials>
</MaterialValueDef>
```

 Using this format, you can make dictionaries only with key types that conform to the XML tag requirements; practically, this currently means `enum`, `bool`, `Def`-derived types, and most (but not all) `string`s.

 If you need to make a dictionary with looser requirements, you can use &lt;li&gt; sequences and &lt;key&gt;/&lt;value&gt; tags. This allows keys of any type that def can parse.

```xml
<MaterialValueDef defName="Medieval">
  <materials>
    <li>
      <key>Iron</key>
      <value>1</value>
    </li>
    <li>
      <key>Steel</key>
      <value>3</value>
    </li>
    <li>
      <key>Uranium</key>
      <value>0.1</value>
    </li>
    <li>
      <key>Aluminum</key>
      <value>20</value>
    </li>
  </materials>
</MaterialValueDef>
```

## Defs

Defs are defined using the defName as a key.

```xml
<LevelDef defName="Introduction">
  <nextLevel>Caves</nextLevel>
</LevelDef>

<LevelDef defName="Caves">
  <nextLevel>Swamp</nextLevel>
</LevelDef>

<LevelDef defName="Swamp">
  <nextLevel>MountainPeak</nextLevel>
</LevelDef>
```

## Types

Types can currently be defined using either the class name alone (if unambiguous), or the entire class name with namespaces. Generic types are currently not supported.

This will likely change in future versions, though mostly in the direction of providing better tools to resolve ambiguities.