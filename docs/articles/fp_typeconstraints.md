# Type constraints

```cs
class MonsterDef : Def.Def
{
  [TypeConstraint(MonsterAI)]
  Type aiWorker;
}
```

It's common to use Type members in Defs as a way of selecting complicated behaviors. However, right now *any* Type can be entered into there, requiring further manual validation.

It would be easy to introduce a TypeConstraintAttribute to solve this common issue.