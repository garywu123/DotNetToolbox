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
dotnet pack DotNetToolbox.slnx -c Release -p:PackageVersion=0.1.0-etl-runner.1
```

See [AGENTS.md](AGENTS.md) for repository conventions and test constraints.
