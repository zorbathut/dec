# Nonserialized fields

```cs
class ExampleDef : Def.Def
{
  int value;
  
  [NonSerialized] private int cachedDerivedValue = -1;
  
  int GetCachedDerivedValue()
  {
    if (cachedDerivedValue == -1)
    {
      cachedDerivedValue = Util.CalculateComplicatedDerivedValue(value);
    }
    
    return cachedDerivedValue;
  }
}
```

```xml
<ExampleDef defName="Example">
  <value>3</value>
  
  <!-- Thanks to the [NonSerialized] attribute, this will cause an error! -->
  <cachedDerivedValue>0</cachedDerivedValue>
</ExampleDef>
```

It's somewhat common to have cached or otherwise derived data within defs. Right now, you can accidentally (or maliciously) define that data within your XML. We could use the existing C# System.NonSerialized attribute to signal that certain fields should not be defined from within XML.