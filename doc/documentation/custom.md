# Custom deserialization

Providing custom deserializers for your own classes, or third-party classes, requires defining classes that derive from [`Dec.Converter`](xref:Dec.Converter). [`Dec.Parser`](xref:Dec.Parser) will automatically find them during startup and register them. (Note: At this time, it is *required* to run Parser first. If you want to use Converters in a pre-parser Recorder, come pester me on Discord and I'll get it working for you.)

There are three converter types available. In increasing order of complexity, [`Dec.ConverterString`](xref:Dec.ConverterString`1) is most useful for simple objects that fit inside a string, [`Dec.ConverterRecord`](xref:Dec.ConverterRecord`1) is suitable for larger hierarchies that have a default constructor, [`Dec.ConverterFactory`](xref:Dec.ConverterFactory`1) is best for anything that needs to insert complicated objects created outside the Dec system. It is recommended that you consider them in that order.

## ConverterString

[`Dec.ConverterString`](xref:Dec.ConverterString`1) is designed for converting to and from simple string objects. It's not suitable for objects that reference other objects, nor does it allow you to do per-field default overrides. If your data plausibly fits in a single short human-readable string, ConverterString is a great choice.

It is most often used for simple small data classes; vectors, colors, quaternions, die rolls such as "3d6". Unlike ConverterRecord, it also allows you to return your own objects, created using constructors or provided by other libraries. This means you can use it for references to assets or handles to other objects; "/monster/goblin/goblinTexture.png" could plausibly resolve to an externally-created Texture object.

```cs
public class ColorConverter : Dec.ConverterString<Color>
{
    public override Color Read(string input, Dec.InputContext inputContext)
    {
        // Implementation of ParseColorFromString left as an exercise for the reader
        return Util.ParseColorFromString(input);
    }

    public override string Write(Color val)
    {
        return val.ToString();
    }
}

public class ExampleDec : Dec.Dec
{
    Color tintA;
    Color tintB;
}
```

```xml
<ExampleDec decName="Example">
  <tintA>(0.3, 0.5, 0.8)</tintA>
  <tintB>#4C7FCC</tintB>
</ExampleDec>
```

## ConverterRecord

[`Dec.ConverterRecord`](xref:Dec.ConverterRecord`1) is designed for larger and more complicated objects, possibly including entity hierarchies. It is able to reference other objects in non-tree arrangements, even with circular dependencies. Its major limitation is that ConverterRecord-created objects *must* be creatable directly from the Dec library with a default constructor.

It essentially provides the [`Dec.IRecordable`](xref:Dec.IRecordable) interface to things that you cannot attach that interface to (but given the choice, we recommend using IRecordable instead). This means that both serialization and deserialization are packed into a single general-purpose function.

This is suitable for larger structured items such as matrices, quaternions, or meshes. If you didn't make the class yourself, and it doesn't fit into a string, this should be your first stop.

In most cases this is probably going to be used as part of savegames, not embedded in Decs.

```cs
public class MeshConverter : Dec.ConverterString<Mesh>
{
    public override void Record(Mesh input, Dec.Recorder recorder)
    {
        // Pretend "vertices" here is a List<Point2>
        // Point2 itself is serialized by a ConverterString.
        recorder.Record(ref input.vertices, "vertices");

        // Pretend "triangles" is a List<TriangleIdx>
        // TriangleIdx is also serialized by a ConverterString.
        recorder.Record(ref input.triangles, "triangles");
    }
}

public class ExampleDec : Dec.Dec
{
    Mesh mesh;
}
```

```xml
<ExampleDec decName="Square">
  <mesh>
    <vertices>
      <li>0, 0</li>
      <li>0, 1</li>
      <li>1, 0</li>
      <li>1, 1</li>
    </vertices>
    <indices>
      <li>0, 1, 2</li>
      <li>1, 3, 2</li>
    </indices>
  </mesh>
</ExampleDec>
```

## ConverterFactory

[`Dec.ConverterFactory`](xref:Dec.ConverterFactory`1) is the most powerful and expressive converter, coupled inevitably with the most complicated interface. In most cases, you should probably not use this! It's available for the special situations where you really need it.

ConverterFactory is able to reference other objects in non-tree arrangements, even with circular dependencies. It can override default-provided parameters piecemeal. It can also provide preconstructed or custom-constructed objects as needed. Its biggest downside is complexity; it splits functionality into three separate functions, [Create()](xref:Dec.ConverterFactory`1.Create*), [Read()](xref:Dec.ConverterFactory`1.Read*), and [Write()](xref:Dec.ConverterFactory`1.Write*).

Create() *may* be called first if an instance needs to be generated. If it has already been provided with an instance, Create() will be skipped entirely. Create() has a Recorder interface, but attempting to make a Shared reference is an error; don't do that. We recommend putting only the minimum required to construct an object in here.

Read() is called next, and it also has Recorder interface, this time fully-functional. Put all the rest of your data parsing in here.

Write() is used for serialization. This will probably look very similar to Read(), except plus whatever data will need to be stashed away for Create() to work during loading.

This example is for a hypothetical entity system that requires Entity objects to be constructed in specific buckets.

```cs
public class EntityConverter : Dec.ConverterFactory<Entity>
{
    public override Entity Create(Recorder recorder)
    {
        EntityBucket bucket = EntityBucket.Default;
        recorder.Record(ref bucket, "bucket");

        return EntityManager.CreateInBucket(bucket);
    }

    public override void Read(ref Entity input, Recorder recorder)
    {
        recorder.Record(ref input.health, "health");
    }

    public override void Write(Entity input, Recorder recorder)
    {
        recorder.Record(ref input.bucket, "bucket");
        recorder.Record(ref input.health, "health");
    }
}
```

```xml
<!-- Assume this exists inside a Recorder xml file -->
<entity>
  <bucket>Priority</bucket>
  <health>12</health>
</entity>
```
