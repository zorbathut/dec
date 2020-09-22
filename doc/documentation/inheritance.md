# Def inheritance

```xml
<MonsterDef defName="Tier4Monster" abstract="true">
  <gold>4d100</gold>
  <loot>Tier4LootTable</loot>
  <health>70</health>
  <damage>3d6</damage>
  <visuals>
    <icon>t4monster.png</icon>
    <color>#ff8080</color>
  </visuals>
</MonsterDef>

<MonsterDef defName="GoblinElite" parent="Tier4Monster">
  <!-- Derives from Tier4Monster, so we get all the existing members automatically -->
  <name>Goblin Elite</name>
</MonsterDef>

<MonsterDef defName="GreaterBlob" parent="Tier4Monster">
  <!-- Here, we override a few members piecemeal -->
  <health>120</health>
  <damage>2d6</damage>
</MonsterDef>

<MonsterDef defName="GoldBlob" parent="GreaterBlob">
  <!-- You can inherit from non-abstract defs too -->
  <gold>12d100</gold>

  <!-- Child composite types can be overridden piecemeal as well -->
  <visuals>
    <color>#ffff80</color>
  </visuals>
</MonsterDef>
```

When you need many things that are in some way similar, it can be useful to create a set of default values that can be applied on demand. Abstract defs, determined by the `abstract="true"` attribute, won't be included in the Def database.

A standard single-inheritance structure is not always sufficient, but [multiple inheritance](/future/multipleinheritance.md) is not yet implemented.

Most members are straight-out replaced if specified in child types. This includes collections, such as List and Dictionary; there is currently no way to append to a base type's collection, though [this functionality is planned](/future/mods.md). Composite types are modified on a per-element basis.