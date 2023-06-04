# Recorder

dec has a serialization layer built-in, intended for savegames and configuration.

## Serializing and deserializing

```cs
string serializedString = Recorder.Write(anyObject);
var deserializedObject = Recorder.Read<ObjectType>(serializedString);
```

Recorder supports the same types that dec does. Decs themselves will be serialized as a reference, not as a fully serialized class. This is true only when referencing the dec itself, not objects contained within the dec's structure; this [may be provided later](~/future/serialization_interior.md).

Child objects will be serialized recursively. In addition, Recorder supports non-tree structures and circular references. Recorder doesn't require any explicit ownership semantics; multiply referenced objects will be handled automatically, without any extra effort required by the programmer.

## Custom serialization

Recorder does not support the automatic reflection-based parsing that Dec.Parser does. This is partly for security reasons and partly for practicality reasons; the author's experience is that fully automatic reflection-based game saves rarely work well.

Recorder does support [`Dec.Converter`](xref:Dec.Converter), but this isn't the intended way to handle serialization of classes that you authored. Most custom serialization should be taken care of by inheriting from [`Dec.IRecordable`](xref:Dec.IRecordable), then overriding [`Dec.IRecordable.Record`](xref:Dec.IRecordable.Record(Dec.Recorder)). This function provides you with a [`Dec.Recorder`](xref:Dec.Recorder), whose most important function is Dec.Recorder.Record. This function can be used to easily serialize any type that Recorder supports.

```cs
public class RecordableExample : Dec.IRecorder
{
    int intMember;
    SomeClass classMember;
    SomeStruct structMember;
    List<string> collectionMember;
    
    public void Record(Recorder recorder)
    {
        recorder.Record(ref integerMember, "integerMember");
        recorder.Record(ref classMember, "classMember");
        recorder.Record(ref structMember, "structMember");
        recorder.Record(ref collectionMember, "collectionMember");
    }
}
```

## Shared Instances

Recorder will, by default, re-use any existing data in a field. Unfortunately, this precludes class instances with multiple references to them, i.e. data structures that are not a pure tree. If you want instances with more than one reference, it is necessary to specify `.Shared()` for thse specific instances.

```cs
public class SharedRecordableExample : Dec.IRecorder
{
    SomeSharedClass sharedClassMember;
    
    public void Record(Recorder recorder)
    {
        recorder.Shared().Record(ref sharedClassMember, "sharedClassMember");
    }
}
```

When using this feature, classes *cannot* be pre-initialized; they must start as `null`.

## Dec Compatibility

While the savegame format is not guaranteed and may change without notice, we plan to support full backwards compatibility for all time. The save format includes a version number that Recorder will read and adjust for whenever necessary.

## Game Compatibility

At the moment, Recorder includes no features to assist with porting old savegames to new versions of a game. This [may be provided later](~/future/serialization_compatibility.md).