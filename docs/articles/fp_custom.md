# Custom deserialization

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

There are plenty of situations where a specific deserialization solution would come in handy. `tintA` in the above code is readable, but ugly; in most cases, tintB or tintC would likely be better.

A general framework for registering custom deserializers would solve a lot of problems.
