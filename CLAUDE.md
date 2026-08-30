# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Interaction Guidelines

**Answer questions before coding**: When asked a question, provide an actual answer first. Don't leap straight to writing code.

**Never commit unless explicitly told to**: Complete the work and leave it uncommitted in the working tree. Only `git commit` when the current request explicitly asks for it ("commit this", "make a checkin"); a phrase like "let's make that a separate checkin" describes how the work should eventually be grouped, not permission to commit it yourself, and permission granted for one task never carries over to the next.

**Split significant work into small self-contained commits**: A significant change lands as a sequence of minimal commits, not one lump. Split along seams that carry meaning, each commit with a one-sentence story — never mechanically per-file or per-layer. The seams that matter:

- A pure refactor of existing code that the feature merely motivated (extracting an interface, collapsing duplicated lookups) is its own commit, landing *before* the feature that wanted it.
- A pre-existing bug fixed along the way is its own commit, however small.
- A behavior change to an existing system is separate from both the refactor that enabled it and the feature that exposed it — behavior changes are the commits people hunt for later.
- A vendored third-party drop stands alone.
- Conversely, keep together what only works together: the halves of a feature that can't be exercised separately, data plus the code that loads it.

Tests go in the commit that makes them meaningful, written against subjects the series doesn't later mutate. Every commit must build and pass the suite on its own — the history should be bisectable. Late fixes (review feedback included) get folded into the commit they belong to via fixup/autosquash, not appended as cleanup commits.

When I've told you to commit the work, apply this by default. When the work stays uncommitted, still build it as one unit in the working tree — but when you finish, point out that it's a good candidate for splitting and propose the commit sequence.

**Evaluate, don't assume**: "Why don't we X?" is a request for evaluation, not a suggestion to do X. Explain the tradeoffs, potential issues, or reasons why X might or might not be a good idea.

**Debug by evidence, not by guess**: When investigating a bug you don't fully understand, prefer adding diagnostic instrumentation or asking focused questions over making speculative changes. A confident theory backed by reading the code is fine to act on; a vibe is not. If a fix doesn't solve the user's problem, that's a signal that the theory was wrong — gather more data before trying again. Two consecutive failed fixes mean stop guessing entirely: pause, instrument, and ask. Rapid-fire blind changes waste the user's attention and erode trust.

**Err on the side of more diagnostic data, not less**: When you ask the user to run something — a probe build, a manual test, a copy-paste session — the expensive part is the round trip itself. The marginal cost of one more printed value, one more covered code path, one more chapter to click is small. So when you instrument, instrument generously: log every variable that could plausibly disambiguate the bug, exercise every endpoint of the parameter space (V=0, V=0.5, V=1, not just whichever was easy), include both the suspected-correct prediction *and* the alternatives so residuals are immediately visible. A diagnostic that prints 30 lines and answers the question on the first try is far cheaper than three diagnostics that each print 3 lines. Make the round trip pay for itself.

**Don't write creative human-facing text unless explicitly told to** (relevant to the example game in `/example/loaf/`): That includes dialogue, flavor text, story text, and similar authored prose. When building something that needs such text, use really obvious placeholder text — `[Braider Greeting]` or the like. First impressions of written copy are sticky, and the author wants to write it themselves. This is about creative writing, not utilitarian copy — error messages, labels, and log text are fine to write.

**Waiting on background work is not a tool call**: When you've backgrounded a long command (e.g. the full test suite, which exceeds the foreground timeout) and have nothing else productive to do, just end the turn — its completion notification will re-invoke you automatically. Don't emit no-op commands (`echo "waiting"`, re-reads, status pings) to stay "active"; ending on plain text is the correct way to wait, not a hand-off. Conversely, if a command fits the foreground timeout and you'd only wait for it anyway, run it in the foreground so the result returns in the same call. Backgrounding *and* polling is the worst of both.

## Workflow

**Step 1 — Plan.** Enter plan mode (the actual `EnterPlanMode` tool — not a freeform text plan) and research the task and produce a plan. Skippable for trivial changes (under ~a dozen lines). Include unit tests in the plan whenever they're plausible to add — UI generally can't be tested, most other things can.

**Step 2 — Hostile-review the plan.** Before leaving plan mode, spawn a hostile-review agent (run it on Opus — `model: "opus"`) against the plan itself. Brief it like a design reviewer: explain the problem being solved, point it at CLAUDE.md (and the rest of the tree — it can read whatever it needs to research), give it the plan, but do not justify the plan's choices. Give it enough feedback space to actually push back on the approach. Apply the same adjudication rules as the final review (below). Fold valid objections into the plan, then exit plan mode.

**Step 3 — Tests first (when applicable).** For bugfixes, or any feature whose tests can be sensibly written before the implementation exists, write the tests first and verify they fail. Then complete the implementation.

**Step 4 — Run all tests.** Always, even when the change seems unrelated. If anything breaks, return to step 3 — or step 1 if the fix requires significant redesign. For UI changes that can't be unit-tested, explicitly say so rather than claiming success.

Don't treat a failing test as a hard veto on the change. Tests exist to catch *unintentional* drift — a test that pins behavior the change deliberately replaced should be updated alongside the code, not worked around to preserve the old behavior. Fix the test to match the new intent; only fall back to step 3 / step 1 when the failure exposes an actual regression.

**Step 5 — Update CHANGELOG.** For every even-slightly-user-facing change — new/changed/removed APIs, behavior changes, bugfixes, diagnostics the user sees, xmldoc on public members, performance characteristics — add an entry under `[unreleased]` in the appropriate section (Added / Breaking / Improved / Fixed). Purely internal cleanup with no outward effect (private helpers, test-only code, internal comments) can be skipped. When in doubt, add the entry.

**Step 6 — Hostile review.** Spawn a hostile-review agent (run it on Opus — `model: "opus"`). Brief it like a PR reviewer: explain the problem being solved, point it at CLAUDE.md (and the rest of the tree — it can read whatever it needs to research), but do not explain or justify the implementation. Explicitly ask it to **review the general architecture** too, not just the diff — does the chosen approach fit the surrounding code, are there cleaner factorings, does it introduce abstractions that don't pay rent, etc. Give it enough feedback space to cover both the local change and the architectural read effectively (don't cap it to a terse response). Then:
  - If it raises valid objections, fix them. Significant redesign → back to step 1; code changes → back to step 3.
  - If I disagree with an objection, push back once. If it still objects and I'm still confident, surface the disagreement to the user for adjudication rather than looping.
  - Either way — adjudication needed or not — give the user a quick summary of the review at the end.

## Coding Guidelines

**KISS / YAGNI / MVP**: Keep it simple. Write the simplest code that solves the current problem. Include what's necessary, not more. Don't build abstractions, features, or speculative generality that aren't immediately needed. Three similar lines is better than a premature abstraction.

**No backwards compatibility for its own sake**: Remove stubs and dead code completely. If something is unused or being replaced, delete it outright — don't leave shims, renamed `_unused` vars, `// removed` comments, or compatibility re-exports behind. The git history is the backwards compatibility. (This is about internal code; removing or changing *published API surface* is a real breaking change for downstream users and belongs under Breaking in the CHANGELOG.)

**Error handling**:
- Don't add excessive or preemptive error handling. Don't validate everything before it's ever been an issue. Trust internal code and framework guarantees; only validate at system boundaries (user input, external APIs) — which, for Dec, is exactly where most of the validation lives: XML input, user-supplied types, and Recorder data.
- **Silent error handling is banned.** Never swallow exceptions or ignore error conditions. If something fails, it must be reported (via `Dbg.Err`/the project's logging facility) or thrown. An empty `catch` is a bug.
- Distinguish bugs from user mistakes in the diagnostics you emit. A user feeding Dec bad XML or a bad type should get a specific, helpful, recoverable error — not an assertion about Dec's internals. When adding new features, ask: "Can a user trigger this through normal usage?" If yes, report it as a user-facing error with a helpful message.

**Don't hand-wrap lines**: One thought, one line — however long. Editors soft-wrap; you don't need to. Never split a sentence across multiple comment lines (`//`, `/// `, `* `, `#`) to fit a column limit. The only exceptions are:
- **Distinct paragraphs** in a comment: separate with a **blank line** (true paragraph break), not just a `\n`.
- **Structurally-aligned expressions**: one argument per line, one chained call per line, etc. — a structurally-motivated break in a long expression, never a break for character count.

A multi-sentence single-thought comment is still one line. "It reads better wrapped" is not an exception — that's the rule talking. If a comment you're about to write has a newline before the sentence ends, delete that newline. Concretely:
```
// Wrong:
// Dec's Converter types must not surface through discovery: abstract bases
// produce errors in Serialization's scan.

// Right:
// Dec's Converter types must not surface through discovery: abstract bases produce errors in Serialization's scan.
```

**Composition over inheritance**: Prefer building behavior out of small composable pieces (functions, components, properties, modules) over deep class hierarchies. Inheritance is a tool, not a default.

**Data-driven where it pays**: When a category of behavior is open-ended (content, configuration, content variants), prefer data files and a small interpreter over hardcoded code paths. When it's closed and unlikely to grow, just write the code.

## Commenting

A comment earns its place by saying something the code cannot. That's usually one of: a non-obvious "why", a subtle constraint, a surprising choice or tradeoff — or signposting the flow of a long linear process. A one-line summary of what the next chunk of a long function is doing ("Accumulate the asymptotes" over ten lines of dense math; "Resolve overlaps, nearest first" over a loop) is genuinely useful, and often cleaner than extracting that chunk into a function called exactly once. A comment that restates what the name or a single line of code already makes plain is noise.

**Write for a reader who never saw the old code.** A comment that earns its place only by contrast with a previous version — reassuring that a value isn't what it once meant, noting the code no longer does X, explaining that something is "now" done differently — is history in disguise. The tell: it answers a question a fresh reader would never think to ask (nobody wonders whether a `0` expectation is "really a skip" unless they know it once was). State only what's true now, and delete the rest. The one exception is history that constrains the present — a warning against a change someone might actually make ("don't revert this to the double-precision form; it loses the low bits at fixed-point scale") or a deliberate deviation to reconcile later (a clearly-marked local patch to vendored code). That is a load-bearing *why*, not nostalgia; the test is whether the history guards against a real regression or merely explains away a non-question. Police this mainly in your own new comments — by the removal asymmetry below, don't strip others' on suspicion alone.

**Adding — be conservative, but signpost freely.** Don't narrate the obvious: a `bool allowFlips` field needs no `// when false, flipping is disabled`; a `// set the flip` above `flip = …` adds nothing. Reach first for self-explanatory code (good names, clear structure), and comment the part code can't carry — usually the *why*, not the *what*. The exception is flow signposting in long procedures, where a sparse trail of one-line "what next" headers is a real readability win; use them. When you explain a "why", keep it tight; one good line beats a paragraph. Don't reference the current task, fix, or callers ("used by X", "added for the Y flow", "handles the case from issue #123") — those belong in the commit message and rot as the codebase evolves.

**Removing — be generous about keeping.** Existing comments are there for a reason. If one is out of date or actively misleading, fix or remove it. Otherwise leave it alone — don't strip a comment just because it explains the "what", or because you wouldn't have written it yourself. The asymmetry is deliberate: conservative about adding your own, generous about keeping others'.

## Naming

- **PascalCase**: Public members, types, static fields (including private statics — `Database.Lookup`).
- **camelCase**: Private/protected instance fields, parameters, locals.
- **Interface prefix**: `I` (e.g., `IRecordable`).
- **Category-instance prefix**: When a name combines a category with an instance, put the category first so related names group alphabetically and the category reads as the classification. `ConverterString`, `ReaderXmlDec`, `WriterNode`, `AttackStart()` — not `StringConverter`, `XmlDecReader`, `StartAttack`. The category is the "kind of thing"; the instance is the specific variant. Apply this to types, functions, files, and config keys alike.

## C# Style

`.editorconfig` is authoritative and already encodes most of this; these are the rules worth stating out loud.

**Always use braces**: Always include `{}` for `if`, `else`, `for`, `foreach`, `while`, etc., even for single-line bodies.
```csharp
// Good
if (condition)
{
    return;
}

// Bad
if (condition) return;
if (condition)
    return;
```

**Avoid expression-bodied members (`=>`)**: Prefer block bodies with explicit `return` statements for methods and properties. Expression bodies obscure control flow. (Some older code uses `=>` for trivial property forwards; don't propagate it, and don't churn it out either.)
```csharp
// Good
public int GetValue()
{
    return value;
}

// Bad
public int GetValue() => value;
```

**Usings**: All `using` directives must be explicit and alphabetized at the top of each file.

### Default Parameters and Overloads

The deciding axis is the *nature of the parameter*, not the mechanism. A default parameter is the right tool for a conceptually optional thing; an overload is for a signature that is genuinely different, not "a default parameter wearing a funny hat."

- **A new parameter the function genuinely needs is mandatory.** Add it without a default and update the call sites. Don't reflexively give every new parameter a default value just to avoid touching callers — that's the main thing this rule exists to prevent.
- **A default value is for a *conceptually optional* parameter** — one with a principled "absent" value: a nullable callback or override (`Action onDone = null`, `Func<Type, object> factory = null`), or a natural identity like `double steepness = 1.0`. It is *not* for an arbitrary tuning constant that merely happens to suit most callers — something like `attemptsPerIteration = 30` should be mandatory or a named constant, not a default.
- **Overloads are for genuinely different signatures** — different parameter *types* or *shapes* that can't collapse into one signature, or a meaningfully different operation. Do not write an overload pair whose only difference is that one omits a trailing optional argument — use a default parameter instead.
- **A behavior-switching bool may be a default-`false` parameter only when it's a rider on the same operation** — the result is the same kind of thing, the flag just tweaks a side aspect, and it's almost always off. When the flag changes *what the function fundamentally means* — the question it answers — it shouldn't be a flag: make it a separate, differently-named function, or handle it at the call site. Name any split function category first, per Naming.
- **Hard exception**: compiler-attribute parameters (`[CallerFilePath]`, `[CallerLineNumber]`, `[CallerMemberName]`) must be default parameters — there is no overload form, so these don't count against the rule.

## Critical Rules

1. **Always use absolute paths** in file operations. Relative paths break under tooling that runs from a different working directory than expected.
2. **Tests live next to the code they test** in spirit even if not in directory layout. New code without tests is a debt that compounds.

## Executing Actions with Care

Carefully consider the reversibility and blast radius of actions. Local, reversible actions (editing files, running tests, running ephemeral scripts) are fine to take freely. But for actions that are hard to reverse, affect shared systems beyond the local environment, or touch production, **confirm first**.

Examples that warrant confirmation:
- Destructive: deleting files/branches, dropping tables, killing processes, `rm -rf`, overwriting uncommitted changes.
- Hard to reverse: force-push, `git reset --hard`, amending published commits, dependency downgrades, CI/CD changes.
- Visible to others: pushing code, creating/closing PRs or issues, sending messages, posting to external services.
- Uploading content to third-party tools (pastebins, diagram renderers): may be cached or indexed even if later deleted.

When you encounter an obstacle, do not use destructive actions as a shortcut to make it go away. Identify root causes; don't bypass safety checks (`--no-verify`, `--no-gpg-sign`) unless the user has explicitly asked. If you discover unexpected state — unfamiliar files, branches, locks — investigate before deleting; it may be the user's in-progress work.

## Tone for Updates

Match response length to the task. A simple question gets a direct answer, not headers and sections. End-of-turn summaries should be one or two sentences — what changed and what's next. Don't narrate internal deliberation; state results and decisions directly. Brief is good; silent is not.

## Project Overview

Dec is a C# library for declaring game asset types in XML. It includes:
- Extensive error reporting and recovery
- Reflection-based design to minimize boilerplate
- A serialization system for savegames
- Support for circular references and complex object graphs
- Multi-target support (.NET Core 2.1, 3.1, .NET 6.0, 7.0, 8.0, 9.0)

## Key Commands

### Building
```bash
# Build entire solution
dotnet build
dotnet build -c Release

# Build for specific framework
dotnet build -f net6.0
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests for specific framework
dotnet test -f net6.0

# Run with code coverage
dotnet test -f net6.0 --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

# Run a single test
dotnet test --filter "FullyQualifiedName~TestClassName.TestMethodName"
```

### Documentation
```bash
# Install docfx globally (first time only)
dotnet tool install --global docfx

# Build documentation
docfx doc/docfx.json

# Build and serve locally
python doc/doc.py --serve
```

### Package Creation
```bash
dotnet pack src/dec.csproj -c Release
```

## Architecture Overview

`ARCHITECTURE.md` has the detailed serialization walkthrough and the threading contract; read it before touching Serialization, Recorder, or anything concurrency-adjacent.

### Core Components

1. **Dec Base Class** (`src/Dec.cs`): Base class for all declarative data types. Each Dec has a unique `DecName` identifier.

2. **Parser System** (`src/Parser.cs`, `src/ParserModular.cs`): Loads XML data into Dec instances. The public `Parser` class is a facade over `ParserModular` which supports modules for modding.

3. **Database System** (`src/Database.cs`): Global registry for all Dec instances. Provides type-safe access via `Database<T>`.

4. **Serialization Core** (`src/Serialization.cs`): Converts between XML and C# objects using recursive descent pattern. Uses `ReaderNode`/`WriterNode` abstraction to separate format from logic.

5. **Recorder System** (`src/Recorder.cs`): Unified API for serialization/deserialization of complex objects. Supports shared references and custom factories.

6. **Converter System** (`src/converter/`): Handles serialization of third-party types:
   - `ConverterString`: Simple string-based conversion
   - `ConverterRecord`: Uses Recorder API for complex types
   - `ConverterFactory`: Full control over object creation

7. **Static References** (`src/attributes/StaticReferences.cs`): Auto-populates static fields with Dec references after parsing.

8. **Index System** (`src/attributes/Index.cs`): High-performance integer-based indexing for collections.

### Key Design Patterns

- **Type-Driven Design**: Heavy use of reflection for automatic serialization
- **Recursive Descent**: Serialization system recursively processes nested structures
- **Static Registry**: Database classes act as global registries
- **Factory Pattern**: Multiple levels of factory support for custom object construction

### Data Flow

1. XML files → Parser → Serialization → Dec instances → Database registration
2. After loading: Static references filled → setup DAG (ConfigErrors/PostLoad plus [Dec.Setup] functions, ordered by [Dec.SetupAfter]/[Dec.SetupBefore])
3. For serialization: Objects → Recorder → Serialization → XML output

## Testing

Run the full test suite on every change, not just the tests you think are related — that's the whole point of having a suite.

Write tests against the *seam* you actually want to defend — pure functions, deterministic state machines, parsers, classifiers — and don't try to retrofit unit tests around UI, rendering, or process-orchestration code that has no testable seam. For those, say so explicitly when reporting status, and rely on a manual smoke instead of pretending coverage you don't have.

Integration tests that spin up real dependencies are usually worth the slowness over mocks: mocks pass when the contract drifts, real dependencies fail loudly. When mocking is unavoidable, mock at the *outermost* boundary you reasonably can.

**Don't pin user-facing copy in tests.** Test text-producing logic relationally instead of asserting exact strings: capture outputs and assert relations between them — non-empty, distinct across states that should read differently, equal across states that should read the same (priority/stability), exact match only for sentinels like `""`. This lets copy be rewritten without touching tests. Exact-match assertions remain correct when the exact output *is* the contract — which covers most of Dec, since serialized XML and parse results are exactly that — or when the string is fixture data the test itself authored; this rule is about production copy shown to end users, which for Dec mostly means error-message text. Apply when writing or modifying tests; don't mass-convert existing tests unless asked.

### Testing Structure

- `/test/unit/`: NUnit-based unit tests
- `/test/integration/`: Integration tests
- `/test/integration_unified/`: Additional integration tests
- Test naming convention: `dec-test-{type}.csproj`
- Uses NUnit 3.12.0 with Coverlet for code coverage

## Important Notes

- Warning suppressions in .csproj files: CS0649, NU1902, NU1903
- Development is demand-driven - the repo may be idle between feature requests
- Dual-licensed under MIT and Unlicense
- Example game "Legend of the Amethyst Futon" in `/example/loaf/`
