# DotNetToolbox — Copilot Instructions

## Project Overview

DotNetToolbox is a set of reusable .NET 8 class libraries. There is no UI code in this repo.
The primary consumer is the SyncTool WinUI 3 application (separate repo/folder).

## Language & Framework

- **C# 12**, `.NET 8`
- Nullable reference types: **enabled** in all projects
- Implicit usings: **enabled**
- Treat warnings as errors: **enabled**
- Use modern C# features: primary constructors, collection expressions `[]`, `required` properties,
  `nameof()`, pattern matching, `is` type checks

## Code Style

- Private fields: `_camelCase`
- Properties: `PascalCase`
- Constants: `PascalCase` (not `ALL_CAPS`)
- Interfaces: `I` prefix (`ISchemaService`)
- Prefer `record` over `class` for immutable data carriers
- Prefer `static` classes for stateless utility methods
- Use `file`-scoped namespaces

## Async Rules

- All I/O operations must be `async/await` with `CancellationToken` parameter
- Never use `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` — these cause deadlocks
- `CancellationToken ct = default` as last parameter on all async public methods
- Pass `ct` through to all downstream async calls

## Error Handling

- Use `TryParse` — never catch `FormatException` or `InvalidCastException` to detect types
- Throw specific exceptions (`ArgumentNullException`, `InvalidOperationException`) with descriptive messages
- Do not swallow exceptions silently — log and rethrow, or return a typed result

## XML Documentation

Every public member requires XML doc:

```csharp
/// <summary>One-line description of what this does.</summary>
/// <param name="paramName">What this parameter represents.</param>
/// <returns>What is returned, including null/empty cases.</returns>
/// <exception cref="ArgumentNullException">When X is null.</exception>
/// <remarks>Optional: edge cases, performance notes, thread safety.</remarks>
```

## Dependency Rules

- `DotNetToolbox.Algorithms` — zero NuGet dependencies (BCL only)
- `DotNetToolbox.Data.Csv` — zero NuGet dependencies (BCL only)
- `DotNetToolbox.Data.SqlServer` — only `Microsoft.Data.SqlClient`
- **Do not add NuGet packages** without noting the reason in a comment

## Testing

- Framework: **xUnit** + **FluentAssertions** + **NSubstitute**
- Use `.Should()` exclusively — never `Assert.*`
- Test naming: `MethodName_Scenario_ExpectedResult`
- `[Theory]` + `[InlineData]` for parameterised cases
- Integration tests: `[Trait("Category", "Integration")]`
- Read `TOOLBOX_TEST_CONN` environment variable for DB connection string in tests

## What Not To Do

- Do not add UI libraries (WinUI, WPF, MAUI)
- Do not use `Thread.Sleep` — use `Task.Delay` in tests that need timing
- Do not use `Console.Write*` in library code — libraries must not produce console output
- Do not create God classes — one responsibility per class
- Do not use `dynamic` type
