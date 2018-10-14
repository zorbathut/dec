# Explicit type specification

Sometimes you may want to explicitly specify the type of a member. This can be done with the "class" attribute.

```cs
class ExampleDef : Def.Def
{
  BaseClass data;
}
```
```xml
<ExampleDef defType="Example">
  <data class="DerivedClass">
    <innerData>Kittens</innerData>
  </data>
</ExampleDef>
```

Your class must match the already-provided instance if you've provided a default value:

```cs
class ExampleDef : Def.Def
{
  BaseClass data = new BaseClass();
}
```
```xml
<ExampleDef defType="Example">
  <data class="DerivedClass"> <!-- this is an error; must be BaseClass, or omitted -->
  </data>
</ExampleDef>
```

Types are described using the same format listed under [type member parsing](doc_parsing.md).