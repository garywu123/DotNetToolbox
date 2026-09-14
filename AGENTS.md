# DotNetToolbox Agent Instructions

This software repository contains reusable, UI-free .NET libraries published as
private NuGet previews for consumers such as SyncTool and Vehicle Simulator.

## Documents

| Need | Read |
|---|---|
| Packages, consumers, feed setup, and install examples | `README.md` |
| Approved library behavior and API contracts | `doc/spec/`, then the matching `doc/api/` reference |
| Preview publishing workflow | `docs/features/F04-private-github-packages.md` |

Read only the route required by the task. Source projects are in
`DotNetToolbox.*/`; tests and their patterns are in `DotNetToolbox.Tests/`.

## Precedence

Resolve conflicts in this order: the current explicit user instruction, the
applicable specification or Feature Plan, then repository evidence. Report a
conflict instead of promoting observed behavior into intended behavior.

## Library Surfaces

| Library | Responsibility | Current targets |
|---|---|---|
| `DotNetToolbox.Algorithms` | Graph ordering and planar path geometry; BCL only | `net8.0;net10.0` |
| `DotNetToolbox.Data.Csv` | Type-safe CSV reading and writing; BCL only | `net8.0` |
| `DotNetToolbox.Data.SqlServer` | SQL Server schema, coercion, and bulk loading; depends on the other libraries and `Microsoft.Data.SqlClient` | `net8.0` |

New and changed libraries target `net8.0;net10.0`; add `net10.0` to either
data library when a feature next changes it.

## Verified Commands And Checks

- Build: `dotnet build DotNetToolbox.slnx`
- Unit tests: `dotnet test DotNetToolbox.slnx --filter "Category!=Integration"`
- Integration tests, when `TOOLBOX_TEST_CONN` is configured:
  `dotnet test DotNetToolbox.slnx --filter "Category=Integration"`

## Conventions

- Nullable reference types, implicit usings, and warnings-as-errors are enabled
  in every project. Compile changes for every configured target.
- A new library uses package ID `GaryWu123.DotNetToolbox.<Name>`, generates XML
  documentation, and adds its package and API-reference routes to `README.md`.
- Document every public member meaningfully. Keep public APIs minimal, validate
  their inputs, propagate cancellation through asynchronous I/O, and dispose
  owned resources correctly.
- Use `TryParse`-style control flow rather than exception-driven type detection.
- Keep Algorithms and Data.Csv cross-platform. Data.SqlServer may depend on
  `Microsoft.Data.SqlClient` but must not use a Windows-only target framework.
- Never commit feed credentials, tokens, hardcoded connection strings, or
  production connection strings.

## Branches And Preview Releases

- Name consumer-driven branches `<app>/<function>` in lowercase kebab-case,
  such as `vehicle-simulator/clothoid` or `sync-tool/csv-import`.
  `etl_runner` is a legacy exception.
- Complete branches serially unless the user explicitly chooses separate
  worktrees or repository copies for concurrent development.
- Before publishing, commit a clean tree, run build and unit tests, and refresh
  each changed library's `doc/api/API_<Library>.md` using
  `.github/prompts/generate-api-doc.prompt.md`.
- Run the verified VS Code `publish-preview` task. It delegates to
  `scripts/publish-preview.ps1`, publishes packages, then creates and pushes
  the tag; it does not push the branch.
- Use a new immutable `<x.y.z>-<app>-<function>.<n>` version and tag it
  `v<version>`. Consumers use the exact NuGet version, never a project path.

## Testing Conventions

- Use xUnit, FluentAssertions, and NSubstitute; use `.Should()` rather than
  `Assert.*`.
- Name tests `MethodName_Scenario_ExpectedResult`, mark database tests with
  `[Trait("Category", "Integration")]`, and read `TOOLBOX_TEST_CONN`.
- Add a happy-path test before relevant failure paths. Every public method has at
  least one unit test.

## Communication Style

- Lead with the result in concise, clear Chinese.
- Include the verification, assumptions, conflicts, and remaining risks needed
  to act.

## Working Rules

1. Make the smallest bounded change and reuse existing libraries and tools.
2. Read the applicable specification and API contract before changing behavior.
3. Keep package dependencies minimal and respect the library boundaries above.
4. Report only commands and results actually observed; do not guess around
   missing configuration, credentials, services, or tests.
