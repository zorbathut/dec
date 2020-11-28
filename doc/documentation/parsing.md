# Member parsing

Many types can be parsed natively by dec. This page lists them all.

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
<AnimalDec decName="MaineCoon">
  <type>Cat</type>
</AnimalDec>
```

## Lists

Lists are entered with &lt;li&gt; sequences.

```xml
<NumberListDec decName="Primes">
  <numbers>
    <li>2</li>
    <li>3</li>
    <li>5</li>
    <li>7</li>
    <li>11</li>
    <li>13</li>
    <li>17</li>
  </numbers>
</NumberListDec>
```

## Dictionaries

Dictionaries are normally defined using XML tags as keys.

```xml
<MaterialValueDec decName="Medieval">
  <materials>
    <Iron>1</Iron>
    <Steel>3</Steel>
    <Uranium>0.1</Uranium>
    <Aluminum>20</Aluminum>
  </materials>
</MaterialValueDec>
```

 Using this format, you can make dictionaries only with key types that conform to the XML tag requirements; practically, this currently means `enum`, `bool`, `Dec`-derived types, and most (but not all) `string`s.

 If you need to make a dictionary with looser requirements, you can use &lt;li&gt; sequences and &lt;key&gt;/&lt;value&gt; tags. This allows keys of any type that dec can parse.

```xml
<MaterialValueDec decName="Medieval">
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
</MaterialValueDec>
```

## Decs

Decs are defined using the decName as a key.

```xml
<LevelDec decName="Introduction">
  <nextLevel>Caves</nextLevel>
</LevelDec>

<LevelDec decName="Caves">
  <nextLevel>Swamp</nextLevel>
</LevelDec>

<LevelDec decName="Swamp">
  <nextLevel>MountainPeak</nextLevel>
</LevelDec>
```

## Types

Types can currently be defined using either the class name alone (if unambiguous), or the entire class name with namespaces. Generic types are currently not supported.

This will likely change in future versions, though mostly in the direction of providing better tools to resolve ambiguities.