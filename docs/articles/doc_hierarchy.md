# Def hierarchy roots

Every def type is registered in exactly one category. This is necessary because each def contains an index, and the index is intended to work as an array offset when listing only defs of that category. One def, one category; this rule is immutable.

```
Def.Def
  - MineralDef
  - RenderPassDef
  - ActorDef
    - MonsterDef
    - NPCDef
  - CosmeticDef
```

Anything directly inheriting from Def.Def defines a category. Anything inheriting from that is part of the same category. In the above inheritance chart, MineralDef, RenderPassDef, ActorDef, and CosmeticDef all define categories; MonsterDef and NPCDef become part of the ActorDef category.