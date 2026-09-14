# F04: Private GitHub Packages

**Status:** complete

**Sources:** User request; [repository guidance](../../AGENTS.md); [API overview](../../doc/Overview.md)

**Storyboard:** None

## Outcome

An operator can manually publish one versioned, private preview of the three DotNetToolbox libraries, and a consumer can restore those exact package versions without a local project path.

## Scope

**In:** Package metadata, one manual preview release, a repository README, exact consumer references, and version-tagged API-document access.

**Out:** A documentation web site and stable-package release policy.

## Implementation

1. Add minimal package metadata and a repository README; retain compiler XML documentation and current `doc/api` reference files.
2. Build, test, pack, and inspect the preview packages locally; `v*` tag pushes also run the non-integration unit tests, pack the version derived from the tag, and publish with GitHub Actions' `GITHUB_TOKEN`.
3. Tag the same committed source, configure the consumer's private source locally with a PAT, and restore exact preview versions without project references.

## Happy Paths

| Scenario | Test | Expected result | Actual result |
|---|---|---|---|
| Toolbox preview pack | `dotnet test` then `dotnet pack -c Release` | Three versioned `.nupkg` files are created after unit tests pass. | 61 tests passed; three `0.1.0-etl-runner.1` packages created. |
| Consumer restore | `dotnet restore` and build | Consumer uses the tagged preview packages without a Toolbox path. | not run |

## Failure Paths

| Scenario | Test | Expected result | Actual result |
|---|---|---|---|
| Existing package version | Manual push repeats a version | GitHub rejects the duplicate; the previously published package remains unchanged. | not run |
| Missing package access | Restore without valid private-feed access | Restore fails clearly; no fallback to a local Toolbox project occurs. | not run |

## Validation

| Command | Purpose | Result |
|---|---|---|
| `dotnet test DotNetToolbox.slnx --filter "Category!=Integration"` | Toolbox unit regression | Passed: 61 tests. |
| `dotnet pack DotNetToolbox.slnx -c Release -p:PackageVersion=0.1.0-etl-runner.1` | Preview package creation | Passed: three packages; SQL package dependencies use the same preview version. |
| `dotnet build SyncTool.slnx -c Release` | Consumer release build | not run |

## Blockers

- Database Sync Tool has pre-existing uncommitted changes; its dependency files will not be edited until their owner confirms the intended state.
