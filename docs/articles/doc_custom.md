# Custom deserialization

```cs
public class ColorConverter : Def.Converter
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
<ExampleDef defName="Example">
  <tintA>
    <r>0.3</r>
    <g>0.5</g>
    <b>0.8</b>
  </tintA>
  <tintB>(0.3, 0.5, 0.8)</tintB>
  <tintC>#4C7FCC</tintC>
</ExampleDef>
```

Providing custom deserializers for your own classes, or third-party classes, is as easy as defining classes that derive from [`Def.Converter`](xref:Def.Converter). [`Def.Parser`](xref:Def.Parser) will automatically find them during startup and register them. Please check [`Def.Converter`](xref:Def.Converter) documentation for more details.