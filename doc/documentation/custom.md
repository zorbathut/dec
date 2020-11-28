# Custom deserialization

```cs
public class ColorConverter : Dec.Converter
{
    public override HashSet<Type> HandledTypes()
    {
        return new HashSet<Type>() { typeof(Color32) };
    }

    public override object FromString(string input, Type type, string inputName, int lineNumber)
    {
        return Util.ParseColorFromString(input);
    }

    public override object FromXml(XElement input, Type type, string inputName)
    {
        return Util.ParseColorFromXml(input);
    }
}
```

```xml
<ExampleDec decName="Example">
  <tintA>
    <r>0.3</r>
    <g>0.5</g>
    <b>0.8</b>
  </tintA>
  <tintB>(0.3, 0.5, 0.8)</tintB>
  <tintC>#4C7FCC</tintC>
</ExampleDec>
```

Providing custom deserializers for your own classes, or third-party classes, is as easy as defining classes that derive from [`Dec.Converter`](xref:Dec.Converter). [`Dec.Parser`](xref:Dec.Parser) will automatically find them during startup and register them. Please check [`Dec.Converter`](xref:Dec.Converter) documentation for more details.