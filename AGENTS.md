# AGENTS

## Project

Toolbox — a collection of reusable .NET 8 class libraries for internal tooling projects.

Primary consumer: **SyncTool** (WinUI 3 app for SGVM reporting database sync).

## Libraries

| Library | Purpose | Dependencies |
|---|---|---|
| `Toolbox.Algorithms` | Generic graph algorithms (topological sort) | None |
| `Toolbox.Data.Csv` | Type-safe CSV reading and writing | None |
| `Toolbox.Data.SqlServer` | SQL Server schema inspection, type coercion, bulk loading | `Toolbox.Algorithms` |

## Where To Look First

- `doc/Overview.md` — architecture, dependency graph, quick start
- `doc/spec/` — feature specifications and API contracts (**read before implementing**)
- `doc/impl/` — implementation plans with test cases (your work orders)
- `doc/api/` — API reference guides (populated after each IP completes)
- `src/` — source code

## Build & Test Commands

```powershell
# Build entire solution
dotnet build Toolbox.sln

# Run all unit tests (no DB required)
dotnet test Toolbox.sln --filter "Category!=Integration"

# Run integration tests (requires SQL Server — set TOOLBOX_TEST_CONN first)
dotnet test Toolbox.sln --filter "Category=Integration"

# Run everything
dotnet test Toolbox.sln
```

## Environment Variables

Integration tests require a SQL Server instance. Set before running:

```powershell
# Windows — current session
$env:TOOLBOX_TEST_CONN = "Server=localhost;Database=ToolboxTest;Integrated Security=true;TrustServerCertificate=true;"

# Windows — permanent (user scope)
[Environment]::SetEnvironmentVariable("TOOLBOX_TEST_CONN", "Server=localhost;Database=ToolboxTest;Integrated Security=true;TrustServerCertificate=true;", "User")
```

## Implementation Order

Libraries have a strict dependency chain. Always implement in this order:

```
IP_01_Algorithms  →  IP_02_Data_Csv  →  IP_03_Data_SqlServer
```

`IP_03` depends on both `IP_01` (for FK topological sorting) and `IP_02` (for CsvDataReader in bulk load).

## Task Routing

| Task | Action |
|---|---|
| Implementing a new feature | Read `doc/spec/Spec_*.md` first, then `doc/impl/IP_*.md` |
| Fixing a bug | Read `doc/api/API_*.md` for the contract, locate source in `src/` |
| Adding tests | Follow patterns in `src/Toolbox.Tests/` |
| Understanding a library's public API | Read `doc/api/API_*.md` |
| Reviewing architecture | Read `doc/Overview.md` |

## Hard Rules

- All public members **must** have XML doc comments (`<summary>`, `<param>`, `<returns>`, `<remarks>` where helpful)
- **No hardcoded connection strings** in source — use the `TOOLBOX_TEST_CONN` environment variable in tests
- Every public method must have **at least one unit test**
- Use `TryParse` patterns — **never exception-driven type detection**
- `Toolbox.Algorithms` and `Toolbox.Data.Csv` must be **cross-platform** — no Windows-specific APIs, no P/Invoke
- `Toolbox.Data.SqlServer` targets `net8.0` (not `net8.0-windows`) but may use `Microsoft.Data.SqlClient`
- Treat warnings as errors — `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` is set in all projects
- Nullable reference types are enabled — no `#nullable disable`

## Testing Conventions

- Framework: **xUnit** + **FluentAssertions**
- Do not use `Assert.*` — use `.Should()` exclusively
- Integration tests: `[Trait("Category", "Integration")]`
- Test method naming: `MethodName_Scenario_ExpectedResult`
- Shared fixtures via `IClassFixture<T>` or `ICollectionFixture<T>`
