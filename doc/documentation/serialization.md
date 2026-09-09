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

Dictionary keys and hash set elements can be shared only when their type hashes by identity - a class that doesn't override `GetHashCode()`. Keys are hashed while being read, before anything they refer to has been filled in, so a key that hashes on its contents would land in the container under a hash that changes out from under it. Sharing an object of such a type while also using it as a key is an error, as is reading a file that holds such a key as a reference.

Shared instances are written once into a reference block at the top of the file, under a generated name like `ref00000`, and referenced from wherever they appear. Two interfaces let you take control of that. `Dec.IRefName` lets you name an instance's entry, which makes the file much easier to read and to diff; `Dec.IRefForce` puts an instance in the reference block even when only one reference to it exists, giving it a stable home instead of being written inline wherever it happens to be reached first.

```cs
public class NamedSharedClass : Dec.IRecordable, Dec.IRefName, Dec.IRefForce
{
    string id;

    // Return null to accept a generated name.
    public string RefName(Recorder.IUserSettings userSettings)
    {
        return id;
    }

    public void Record(Recorder recorder)
    {
        recorder.Record(ref id, "id");
    }
}
```

```cs
public class Holder : Dec.IRecordable
{
    NamedSharedClass member;

    public void Record(Recorder recorder)
    {
        // Still needed; IRefForce doesn't override the sharing rules, it just uses them.
        recorder.Shared().Record(ref member, "member");
    }
}
```

Names must be unique within a file and must not collide with a Dec path; an invalid name produces a warning and falls back on a generated name. `IRefForce` still obeys the `.Shared()` rules above - an instance reached only through positions that don't allow sharing is written inline with a warning, because a reference in such a position cannot be read back.

## Dec Compatibility

While the savegame format is not guaranteed and may change without notice, we plan to support full backwards compatibility for all time. The save format includes a version number that Recorder will read and adjust for whenever necessary.

## Game Compatibility

At the moment, Recorder includes no features to assist with porting old savegames to new versions of a game. This [may be provided later](~/future/serialization_compatibility.md).