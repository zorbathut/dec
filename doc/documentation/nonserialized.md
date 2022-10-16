# Nonserialized fields

```cs
class ExampleDec : Dec.Dec
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
<ExampleDec decName="Example">
  <value>3</value>
  
  <!-- Thanks to the [NonSerialized] attribute, this will cause an error! -->
  <cachedDerivedValue>0</cachedDerivedValue>
</ExampleDec>
```

It's somewhat common to have cached or otherwise derived data within decs. Use the System.NonSerialized attribute to signal that certain fields should not be defined from within XML; doing so will cause an error and prevent the assignment.

The Recorder game serialization system doesn't use reflection to figure out what fields to record, it records only the exact fields you tell it to. NonSerialized doesn't apply to this; it's useful only for the initial Dec load.