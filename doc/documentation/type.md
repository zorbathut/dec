# Explicit type specification

Sometimes you may want to explicitly specify the type of a member. This can be done with the "class" attribute.

```cs
class ExampleDec : Dec.Dec
{
  BaseClass data;
}
```
```xml
<ExampleDec decName="Example">
  <data class="DerivedClass">
    <innerData>Kittens</innerData>
  </data>
</ExampleDec>
```

Your class must match the already-provided instance if you've provided a default value:

```cs
class ExampleDec : Dec.Dec
{
  BaseClass data = new BaseClass();
}
```
```xml
<ExampleDec decName="Example">
  <data class="DerivedClass"> <!-- this is an error; must be BaseClass, or omitted -->
  </data>
</ExampleDec>
```

When a dec inherits from a parent, or a mod patches it, several layers can each give a `class` for the same member. A later layer may narrow an earlier layer's class to a subclass, and the most-derived class wins; naming an unrelated or more general class is an error, and the earlier class is kept. A layer with `mode="replace"` starts over, so it may name any class.

Types are described using the same conventions listed under [type member parsing](parsing.md).