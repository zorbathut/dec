# Translation

```xml
<ExampleDec decName="Example">
  <visibleName>cow</visibleName>
</ExampleDec>
```

```xml
<LanguageData language="es">
  <ExampleDec.Example.visibleName>vaca</ExampleDec.Example.visibleName>
</LanguageData>
```

```xml
<LanguageData language="ru">
  <ExampleDec.Example.visibleName>корова</ExampleDec.Example.visibleName>
</LanguageData>
```

Translation and internationalization are a big problem in game development. It can be assumed that people will, at some point, put English text in decs; a standard framework to support translation would be very beneficial.

It's possible this should be provided by a standalone library that connects to dec via some API, but then the API needs to be developed too.