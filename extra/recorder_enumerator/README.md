recorder_enumerator
---

This provides a wide set of converters and other functionality to serialize and deserialize enumerators, both for built-in containers and Linq functions and for user-created enumerators.

This entire system relies on deep reflection into classes and structures whose consistent behavior is not guaranteed by .NET. As of right now, it's supported only on .NET 6.0; it may partially work on other versions, and it should be possible to make it work on other versions, but there are definitely going to be issues right now. If you try using it, let me know!

### Installation instructions

Add the source code to your project, and initialize it before using Record:

```cs
Dec.Config.ConverterFactory = Dec.RecorderEnumerator.Config.ConverterFactory;
```

(Have you noticed these instructions are more lackluster than the ones for Dec? If these instructions are not enough for you, *you should not be using this package*.)

### Usage instructions

Most of it will work automatically.

If you want to be able to serialize an enumerator, add the `Dec.RecorderEnumerator.RecordableEnumerable` attribute to the function creating it:

```cs
[Dec.RecorderEnumerator.RecordableEnumerable]
public IEnumerable<int> CountToTwenty()
{
	for (int i = 0; i < 20; ++i)
	{
		yield return i;
	}
}
```

Instances of this enumerator will be automatically picked up and processed.

If you want to be able to serialize closures, add the `Dec.RecorderEnumerator.RecordableClosures` attribute to the function containing them:

```cs
[Dec.RecorderEnumerator.RecordableClosures]
public Goblin CreateGoblin(Faction faction)
{
	return new Goblin(enemyDetector: targetFaction => targetFaction != faction);
}
```

### Caveats

* This is currently designed for .NET 7.0. Earlier versions will gradually break more things as changes happen. This is fixable on request.
* This is not designed for Mono. Mono will probably be totally broken. This is fixable on request.
* Porting records between different .NET runtimes will break catastrophically, without good recovery. Better recovery may be fixable on request; reducing breakage may be fixable on request; *preventing* breakage will probably not be completely solvable.
* Modifying any member of a class containing RecordableEnumerable or RecordableClosures may break things catastrophically. I have basic ideas for how to improve this, though I may not have everything pinned down, and it will likely never be 100%; your code may have to recover from enumerables and closures getting silently changed to `null`.
* Removing previously-recorded closures or enumerables will result in broken recorded files. This is not saving IL, just object names. This may be somewhat recoverable, in the sense of changing things cleanly to `null`; your code will have to recover further from there.
* Changing previously-recorded closures or enumerables may result in broken recorded files. Worse, it may not be aware when something is thrown into a completely broken state. I have some ideas for how to improve this and for how to change things cleanly to `null`; again, your code will have to recover further from there.
* The default GetHashCode() is intrinsically unstable across runs. Don't rely on it.
* Recording enumerables midway through HashSet or Dictionary is unavailable and may never be available, since their traversal order is unstable between executions. I might make a StableHashSet or StableDictionary at some point, but note that this will not be able to default to the standard GetHashCode(), you'll need to write one yourself.
* As a consequence, this is also unable to record any enumerable that relies on HashSet/Dictionary enumerables frozen mid-iteration. This includes GroupBy and Join. StableGroupBy() and StableJoin() may be a thing that gets rigged up someday.

* **You should probably not use this unless you really know what you're doing.** It may gain stability in time; today is not that time.