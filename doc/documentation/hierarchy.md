# Dec hierarchy roots

Every dec type is registered in exactly one category. One dec, one category; this rule is immutable. Each category has its own namespace where each Dec inside it must be named uniquely, but Decs in different categories can share names.

```
Dec.Dec
  - MineralDec
  - RenderPassDec
  - ActorDec
    - MonsterDec
    - NPCDec
  - CosmeticDec
```

By default, categories are defined as anything directly inheriting from Dec.Dec In the above inheritance chart, MineralDec, RenderPassDec, ActorDec, and CosmeticDec all define categories; MonsterDec and NPCDec become part of the ActorDec category.

If you want to define a Dec type that has several child categories, this can be done with the Dec.Abstract attribute. Classes with Dec.Abstract applied must also be abstract, with the C# `abstract` keyword.

```
Dec.Dec
  - [Dec.Abstract] MenuItemDec
    - MainMenuItemDec
    - GameOverMenuItemDec
    - [Dec.Abstract] IngameMenuItemDec
        - ShopMenuItemDec
        - BankerMenuItemDec
```

This example includes four categories; MainMenuItemDec, GameOverMenuItemDec, ShopMenuItemDec, and BankerMenuItemDec. You could have a four decs named `Exit`, one in each category. You can't make decs of type MenuItemDec or IngameMenuItemDec; they aren't in any category.