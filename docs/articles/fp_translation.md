# Translation

```xml
<ExampleDef defName="Example">
  <visibleName>cow</visibleName>
</ExampleDef>
```

```xml
<LanguageData language="es">
  <ExampleDef.Example.visibleName>vaca</ExampleDef.Example.visibleName>
</LanguageData>
```

```xml
<LanguageData language="ru">
  <ExampleDef.Example.visibleName>корова</ExampleDef.Example.visibleName>
</LanguageData>
```

Translation and internationalization are a big problem in game development. It can be assumed that people will, at some point, put English text in defs; a standard framework to support translation would be very beneficial.

It's possible this should be provided by a standalone library that connects to def via some API, but then the API needs to be developed too.