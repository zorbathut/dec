# Mod functionality

As a text-based format, dec XMLs are very conducive to making user-authored mods. However, at the moment there's no good way for those user-authored mods to interact with core functionality.

Assume these examples are an attempt to mod this XML:

```xml
<ExampleDec decName="Example">
  <value>4</value>
  <listOfStrings>
    <li>Yabba</li>
    <li>Dabba</li>
    <li>Doo</li>
  </listOfStrings>
</ExampleDec>

## Overriding

```xml
<ExampleDec decName="Example" mode="replace">
  <value>10</value>
  <listOfStrings>
    <li>Abra</li>
    <li>Cadabra</li>
  </listOfStrings>
</ExampleDec>
```

It's easy to allow mods to simply nuke-and-rewrite existing decs. This isn't the most powerful technique available, but it's effective in many situations.

## Splicing

```xml
<ExampleDec decName="Example" mode="modify">
  <value>10</value>
</ExampleDec>
```

Alternatively, or additionally, we could allow for per-field overriding. This would behave similar to if the original dec is a Parent.

Lists make things more difficult. Some behaviors seem easy:

```xml
<ExampleDec decName="Example" mode="modify">
  <listOfStrings mode="append">
    <li>And more!</li>
  </listOfStrings>
</ExampleDec>
```

```xml
<ExampleDec decName="Example" mode="modify">
  <listOfStrings mode="replace">
    <li>Alla</li>
    <li>Khazam</li>
  </listOfStrings>
</ExampleDec>
```

but others may require a heavier-weight approach.

## Patching

No matter how many features we add, someone will always want to modify existing data in a way that we haven't foreseen. A good solution to this is unclear. Both Rimworld and Starbound have solutions, but both of these solutions have proven flawed, both from a power perspective and a performance perspective.

I currently have no better ideas here.