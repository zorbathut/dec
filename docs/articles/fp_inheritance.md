# Def inheritance

```xml
<ObjectDef defName="FurnitureBase" abstract="true">
  <price>50</price>
  <ownership>PerCharacter</ownership>
</ObjectDef>

<ObjectDef defName="Table" parent="FurnitureBase">
  <!-- Derives from FurnitureBase, so we get the price and ownership automatically -->
  <usableFor>Eating</usableFor>
</ObjectDef>


<ObjectDef defName="StorageContainerBase" abstract="true">
  <price>20</price>
  <storageType>RandomAccess</storageType>
</ObjectDef>

<ObjectDef defName="Shelves" parent="StorageContainerBase">
  <!-- Derives from StorageContainerBase, so we get the price and storageType automatically -->
  <capacity>4</capacity>
</ObjectDef>


<ObjectDef defName="Closet" parent="FurnitureBase, StorageContainerBase">
  <!-- Closets are both furniture and storage containers, so we inherit from both; we get price, ownership, and storageType automatically -->
  <!-- It's unclear which price we should inherit; we'd need to make a decision -->
  <capacity>6</capacity>
</ObjectDef>
```

When you need many things that are in some way similar, it can be useful create a set of default values that can be applied on demand. The base types must not be included in the actual def databases, as they don't represent real defs. In addition, a standard single-inheritance structure is not always sufficient; this should allow arbitrary numbers of mixins.

Once [dynamic def creation](fp_dynamic.md) is supported, this must also provide the necessary hooks to instantiate defs with code-defined lists of parents.

Some functionality required for inheritance to work properly is shared by [the mod features Splicing and Patching](fp_mods.md).