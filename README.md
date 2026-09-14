# DotNetToolbox

Reusable .NET 8 libraries for internal data and database tooling. The libraries contain no UI code and can be consumed independently through NuGet packages.

## Packages

The packages are published to the GitHub Packages feed for `garywu123`:

```text
https://nuget.pkg.github.com/garywu123/index.json
```

Install only the library you need. Use an exact version for repeatable builds.

```powershell
dotnet add package GaryWu123.DotNetToolbox.Algorithms --version "[0.1.0-etl-runner.1]"
dotnet add package GaryWu123.DotNetToolbox.Data.Csv --version "[0.1.0-etl-runner.1]"
dotnet add package GaryWu123.DotNetToolbox.Data.SqlServer --version "[0.1.0-etl-runner.1]"
```

`Data.SqlServer` brings `Algorithms` and `Data.Csv` as NuGet dependencies.
The commands show the first preview version; replace it with the exact version matching the tag you intend to consume.

### Private feed access

Packages are private by default. Each machine that restores them needs a GitHub personal access token (classic) with `read:packages` and access to the associated repository. A machine that manually publishes packages also needs `write:packages`.

Create the token in GitHub under **Settings > Developer settings > Personal access tokens > Tokens (classic)**. Do not commit it or place it in a repository `NuGet.config` file.

Configure the source for the current Windows user. The prompt hides the token while it is entered, and NuGet stores the source credential using Windows user encryption.

```powershell
$env:GITHUB_TOKEN = [System.Net.NetworkCredential]::new(
	"",
	(Read-Host "GitHub PAT" -AsSecureString)
).Password

dotnet nuget add source "https://nuget.pkg.github.com/garywu123/index.json" `
	--name "github-garywu123" `
	--username "garywu123" `
	--password $env:GITHUB_TOKEN `
	--valid-authentication-types "basic"

Remove-Item Env:\GITHUB_TOKEN
```

If `github-garywu123` already exists, replace `add source` with `update source`.

## Libraries

| Package | Purpose | Common API |
|---|---|---|
| `GaryWu123.DotNetToolbox.Algorithms` | Stable dependency ordering for graphs and foreign-key relationships. | [`TopologicalSorter<T>.Sort`](doc/api/API_Algorithms.md) |
| `GaryWu123.DotNetToolbox.Data.Csv` | RFC 4180 CSV parsing, writing, and row-by-row reading. | [`CsvLineParser`, `CsvWriter`, `CsvDataReader`](doc/api/API_Data_Csv.md) |
| `GaryWu123.DotNetToolbox.Data.SqlServer` | SQL identifier validation, schema lookup, value coercion, and bulk loading. | [`SqlIdentifierValidator`, `SchemaService`, `DbValueCoercer`, `SqlBulkLoader`](doc/api/API_Data_SqlServer.md) |

## API documentation

The API references are kept with the source code:

- [Algorithms API](doc/api/API_Algorithms.md)
- [CSV API](doc/api/API_Data_Csv.md)
- [SQL Server API](doc/api/API_Data_SqlServer.md)
- [Architecture overview](doc/Overview.md)

When using a released package, open this README from the tag that matches the package version. For example, package `0.1.0-etl-runner.1` corresponds to tag `v0.1.0-etl-runner.1`. The relative links above then open the API documentation from that same tagged source revision.

## Development

```powershell
dotnet test DotNetToolbox.slnx --filter "Category!=Integration"
```

### Preview release

Pushing a version tag triggers the GitHub Actions workflow at [`.github/workflows/publish-packages.yml`](.github/workflows/publish-packages.yml). It restores, builds, runs all non-integration unit tests on .NET 8 and .NET 10, packs the three library projects, and publishes them privately to GitHub Packages with the version taken from the tag. The workflow uses GitHub's short-lived `GITHUB_TOKEN`; no PAT secret is required in the repository.

Before creating the tag, complete the local release checks and regenerate the API references for every changed library. Use a new version in the format `<x.y.z>-<feature>.<n>`; published NuGet versions cannot be overwritten.

```powershell
$version = "0.2.0-vehicle-communication.1"

dotnet build DotNetToolbox.slnx
dotnet test DotNetToolbox.slnx --filter "Category!=Integration"
git tag "v$version"
git push origin "v$version"
```

The workflow intentionally excludes `[Trait("Category", "Integration")]` tests because they require a separately configured SQL Server connection. Run those locally when `TOOLBOX_TEST_CONN` is available:

```powershell
dotnet test DotNetToolbox.slnx --filter "Category=Integration"
```

See [AGENTS.md](AGENTS.md) for repository conventions and test constraints.
