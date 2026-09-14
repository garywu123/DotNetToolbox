# AGENTS

## Project

DotNetToolbox — reusable .NET class libraries for internal tooling projects,
published as private GitHub Packages previews.

Consumers: **SyncTool** (WinUI 3 app for SGVM reporting database sync) and the
**vehicle-simulation** projects (path geometry and Vehicle COM).

## Libraries

| Library | Purpose | Dependencies | Targets |
|---|---|---|---|
| `DotNetToolbox.Algorithms` | Graph ordering and planar path geometry | None | `net8.0;net10.0` |
| `DotNetToolbox.Data.Csv` | Type-safe CSV reading and writing | None | `net8.0` |
| `DotNetToolbox.Data.SqlServer` | SQL Server schema inspection, type coercion, bulk loading | `Algorithms`, `Data.Csv`, `Microsoft.Data.SqlClient` | `net8.0` |

New and changed libraries target `net8.0;net10.0`, as the test project does;
add `net10.0` to `Data.Csv` or `Data.SqlServer` when a feature next changes it.
Code must compile with C# 12, the net8.0 default. A new library uses PackageId
`GaryWu123.DotNetToolbox.<Name>`, enables `GenerateDocumentationFile`, and adds
a README package row and a `doc/api` reference.

## Where To Look First

- `README.md` — packages, feed URL, consumer install commands
- `doc/Overview.md` — architecture, dependency graph, quick start
- `doc/spec/` — feature specifications and API contracts (**read before implementing**)
- `docs/features/` — Feature Plans, such as `F04-private-github-packages.md` for publishing
- `doc/api/` — consumer API references; read a library's contract before fixing a bug
- `DotNetToolbox.*/` — source code (there is no `src/` folder); `DotNetToolbox.Tests/` — test patterns

## Build & Test Commands

```powershell
dotnet build DotNetToolbox.slnx
dotnet test DotNetToolbox.slnx --filter "Category!=Integration"   # unit tests, no DB
dotnet test DotNetToolbox.slnx --filter "Category=Integration"    # needs TOOLBOX_TEST_CONN
dotnet pack DotNetToolbox.slnx -c Release -p:PackageVersion=<version>
```

Integration tests need SQL Server. Set the variable for the session, or at User
scope with `[Environment]::SetEnvironmentVariable(...)`:

```powershell
$env:TOOLBOX_TEST_CONN = "Server=localhost;Database=ToolboxTest;Integrated Security=true;TrustServerCertificate=true;"
```

## Branch Naming

- Name a consumer-driven branch `<app>/<function>` in lowercase kebab-case,
  for example `vehicle-simulator/clothoid` or `sync-tool/csv-import`.
- Complete these branches serially unless the user explicitly chooses concurrent
  development with separate worktrees or repository copies.
- `etl_runner` predates this rule and keeps its name.

## Preview Release Flow

Every consumer shares this flow; publishing details are in `docs/features/F04-private-github-packages.md`.

1. Finish and commit the work on the consumer branch.
2. Run the build and unit-test commands above.
3. Regenerate `doc/api/API_<Library>.md` for every changed library from its XML
   doc comments, using `.github/prompts/generate-api-doc.prompt.md`.
4. Use the `publish-preview` VS Code task from the consumer branch. It builds, runs
  non-integration unit tests, packs and publishes the private preview, then creates
  and pushes the version tag.
5. Use a new version `<x.y.z>-<app>-<function>.<n>`, for example
  `0.2.0-vehicle-simulator-clothoid.1`. Never reuse a published version;
  consumers reference that exact version, never a project path.

## Hard Rules

- All public members **must** have XML doc comments (`<summary>`, `<param>`, `<returns>`, `<remarks>` where helpful)
- **No hardcoded connection strings** in source — use the `TOOLBOX_TEST_CONN` environment variable in tests
- Every public method must have **at least one unit test**
- Use `TryParse` patterns — **never exception-driven type detection**
- `DotNetToolbox.Algorithms` and `DotNetToolbox.Data.Csv` must be **cross-platform** — no Windows-specific APIs, no P/Invoke
- `DotNetToolbox.Data.SqlServer` must not use a `-windows` target framework but may use `Microsoft.Data.SqlClient`
- Treat warnings as errors — `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` is set in all projects
- Nullable reference types are enabled — no `#nullable disable`
- Never commit feed credentials, tokens or production connection strings

## Testing Conventions

- Framework: **xUnit** + **FluentAssertions**, with **NSubstitute** for mocking
- Do not use `Assert.*` — use `.Should()` exclusively
- Integration tests: `[Trait("Category", "Integration")]`
- Test method naming: `MethodName_Scenario_ExpectedResult`
- Shared fixtures via `IClassFixture<T>` or `ICollectionFixture<T>`

## Working Rules

- Precedence: the current explicit user instruction, then the task's spec and
  Feature Plan, then repository evidence. Report conflicts instead of guessing.
- Record only commands and results that actually ran; report unknowns and risks.
- Reply to the user in Chinese; lead with the result and use plain, direct language.
