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

When doing inheritance, a standard single-inheritance structure is not always sufficient; it should allow arbitrary numbers of mixins with an arbitrary amount of depth.
