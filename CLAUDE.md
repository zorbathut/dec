# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Interaction Guidelines

**Answer questions before coding**: When asked a question, provide an actual answer first. Don't leap straight to writing code.

**Evaluate, don't assume**: "Why don't we X?" is a request for evaluation, not a suggestion to do X. Explain the tradeoffs, potential issues, or reasons why X might or might not be a good idea.

**Don't hand-wrap lines**: Never split a sentence across multiple comment lines (`//`, `/// `, `* `, `#`) to fit a column limit. Let each sentence run its natural length on a single line. Paragraph breaks (a blank comment line between sentences) are the only legitimate reason to start a new comment line — a mid-sentence newline is always wrong. Same rule for code: break only when it genuinely improves readability (e.g. a structurally-motivated break in a long expression), never for character count. Good editors soft-wrap for display.

If a comment you're about to write has a newline before the sentence ends, delete that newline. Concretely:
```
// Wrong:
// Dec's Converter types must not surface through discovery: abstract bases
// produce errors in Serialization's scan.

// Right:
// Dec's Converter types must not surface through discovery: abstract bases produce errors in Serialization's scan.
```

## Workflow

**Step 1 — Plan.** Enter plan mode (the actual `EnterPlanMode` tool — not a freeform text plan) and research the task and produce a plan. Skippable for trivial changes (under ~a dozen lines). Include unit tests in the plan whenever they're plausible to add — UI generally can't be tested, most other things can.

**Step 2 — Hostile-review the plan.** Before leaving plan mode, spawn a hostile-review agent against the plan itself. Brief it like a design reviewer: explain the problem being solved, point it at CLAUDE.md (and the rest of the tree — it can read whatever it needs to research), give it the plan, but do not justify the plan's choices. Give it enough feedback space to actually push back on the approach. Apply the same adjudication rules as the final review (below). Fold valid objections into the plan, then exit plan mode.

**Step 3 — Tests first (when applicable).** For bugfixes, or any feature whose tests can be sensibly written before the implementation exists, write the tests first and verify they fail. Then complete the implementation.

**Step 4 — Run all tests.** Always, even when the change seems unrelated. If anything breaks, return to step 3 — or step 1 if the fix requires significant redesign. For UI changes that can't be unit-tested, explicitly say so rather than claiming success.

**Step 5 — Update CHANGELOG.** For every even-slightly-user-facing change — new/changed/removed APIs, behavior changes, bugfixes, diagnostics the user sees, xmldoc on public members, performance characteristics — add an entry under `[unreleased]` in the appropriate section (Added / Breaking / Improved / Fixed). Purely internal cleanup with no outward effect (private helpers, test-only code, internal comments) can be skipped. When in doubt, add the entry.

**Step 6 — Hostile review.** Spawn a hostile-review agent. Brief it like a PR reviewer: explain the problem being solved, point it at CLAUDE.md (and the rest of the tree — it can read whatever it needs to research), but do not explain or justify the implementation. Explicitly ask it to **review the general architecture** too, not just the diff — does the chosen approach fit the surrounding code, are there cleaner factorings, does it introduce abstractions that don't pay rent, etc. Give it enough feedback space to cover both the local change and the architectural read effectively (don't cap it to a terse response). Then:
  - If it raises valid objections, fix them. Significant redesign → back to step 1; code changes → back to step 3.
  - If I disagree with an objection, push back once. If it still objects and I'm still confident, surface the disagreement to the user for adjudication rather than looping.
  - Either way — adjudication needed or not — give the user a quick summary of the review at the end.

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

## Testing Structure

- `/test/unit/`: NUnit-based unit tests
- `/test/integration/`: Integration tests
- `/test/integration_unified/`: Additional integration tests
- Test naming convention: `dec-test-{type}.csproj`
- Uses NUnit 3.12.0 with Coverlet for code coverage

## Important Notes

- The project follows standard .NET conventions
- Warning suppressions in .csproj files: CS0649, NU1902, NU1903
- Development is demand-driven - the repo may be idle between feature requests
- Dual-licensed under MIT and Unlicense
- Example game "Legend of the Amethyst Futon" in `/example/loaf/`