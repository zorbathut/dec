# Def hierarchy roots

Every def type is registered in exactly one category. One def, one category; this rule is immutable. Each category has its own namespace where each Def inside it must be named uniquely, but Defs in different categories can share names.

```
Def.Def
  - MineralDef
  - RenderPassDef
  - ActorDef
    - MonsterDef
    - NPCDef
  - CosmeticDef
```

By default, categories are defined as anything directly inheriting from Def.Def In the above inheritance chart, MineralDef, RenderPassDef, ActorDef, and CosmeticDef all define categories; MonsterDef and NPCDef become part of the ActorDef category.

If you want to define a Def type that has several child categories, this can be done with the Def.Abstract attribute. Classes with Def.Abstract applied must also be abstract, with the C# `abstract` keyword.

```
Def.Def
  - [Def.Abstract] MenuItemDef
    - MainMenuItemDef
    - GameOverMenuItemDef
    - [Def.Abstract] IngameMenuItemDef
        - ShopMenuItemDef
        - BankerMenuItemDef
```

This example includes four categories; MainMenuItemDef, GameOverMenuItemDef, ShopMenuItemDef, and BankerMenuItemDef. You could have a four defs named `Exit`, one in each category. You can't make defs of type MenuItemDef or IngameMenuItemDef; they aren't in any category.