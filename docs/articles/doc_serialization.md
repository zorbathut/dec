# Recorder

def has a serialization layer built-in, intended for savegames and configuration.

## Serializing and deserializing

```cs
string serializedString = Recorder.Write(anyObject);
var deserializedObject = Recorder.Read<ObjectType>(serializedString);
```

Recorder supports the same types that def does. Defs themselves will be serialized as a reference, not as a fully serialized class. This is true only when referencing the def itself, not objects contained within the def's structure; this [may be provided later](fp_serialization_interior.md).

Child objects will be serialized recursively. In addition, Recorder supports non-tree structures and circular references. Recorder doesn't require any explicit ownership semantics; multiply referenced objects will be handled automatically, without any extra effort required by the programmer.

## Custom serialization

Recorder does not support the automatic reflection-based parsing that Def.Parser does. This is partly for security reasons and partly for practicality reasons; the author's experience is that fully automatic reflection-based game saves rarely work well.

Recorder does support [`Def.Converter`](xref:Def.Converter), but this isn't the intended way to handle custom serialization. Most custom serialization should be taken care of by inheriting from [`Def.IRecordable`](xref:Def.IRecordable), then overriding [`Def.IRecordable.Record`](xref:Def.IRecordable.Record(Def.Recorder)). This function provides you with a [`Def.Recorder`](xref:Def.Recorder), whose most important function is Def.Recorder.Record. This function can be used to easily serialize any type that Recorder supports.

```cs
public class RecordableExample : Def.IRecorder
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

## Def Compatibility

While the savegame format is not guaranteed and may change without notice, we plan to support full backwards compatibility for all time. The save format includes a version number that Recorder will read and adjust for whenever necessary.

## Game Compatibility

At the moment, Recorder includes no features to assist with porting old savegames to new versions of a game. This [may be provided later](fp_serialization_compatibility.md).