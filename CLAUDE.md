# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

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
2. After loading: Static references filled → ConfigErrors() → PostLoad()
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