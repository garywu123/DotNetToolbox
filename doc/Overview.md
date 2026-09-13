# DotNetToolbox — Architecture Overview

## Purpose

DotNetToolbox is a set of focused, reusable .NET class libraries extracted from internal projects.
They contain no UI code. `DotNetToolbox.Algorithms` and its tests target .NET 8 and .NET 10.

## Library Dependency Graph

```
DotNetToolbox.Algorithms
│
└─ (used by) DotNetToolbox.Data.SqlServer
                  │
                  ├─ DotNetToolbox.Algorithms  (FK topological ordering)
                  └─ DotNetToolbox.Data.Csv    (CsvDataReader for SqlBulkCopy)

DotNetToolbox.Data.Csv
│
└─ (used by) DotNetToolbox.Data.SqlServer  (CsvDataReader)
             SyncTool.App            (CsvWriter for export)
```

`DotNetToolbox.Algorithms` and `DotNetToolbox.Data.Csv` have **zero NuGet dependencies**.
`DotNetToolbox.Data.SqlServer` depends only on `Microsoft.Data.SqlClient`.

## Solution Structure

```
DotNetToolbox/
├── DotNetToolbox.slnx
├── DotNetToolbox.sln                 ← optional (when created)
├── AGENTS.md
├── doc/
│   ├── Overview.md                 ← this file
│   ├── spec/
│   │   ├── Spec_Algorithms.md      ← API contract: TopologicalSorter
│   │   ├── Spec_Data_Csv.md        ← API contract: CsvLineParser, CsvWriter, CsvDataReader
│   │   └── Spec_Data_SqlServer.md  ← API contract: SchemaService, DbValueCoercer, SqlBulkLoader
│   ├── impl/
│   │   ├── IP_01_Algorithms.md     ← work order: code + tests for DotNetToolbox.Algorithms
│   │   ├── IP_02_Data_Csv.md       ← work order: code + tests for DotNetToolbox.Data.Csv
│   │   └── IP_03_Data_SqlServer.md ← work order: code + tests for DotNetToolbox.Data.SqlServer
│   └── api/                        ← generated after implementation (do not edit by hand)
│       ├── API_Algorithms.md
│       ├── API_Data_Csv.md
│       └── API_Data_SqlServer.md
├── DotNetToolbox.Algorithms/
│   ├── Geometry/
│   │   ├── Angles.cs
│   │   ├── ClothoidEvaluator.cs
│   │   ├── ClothoidFitter.cs
│   │   ├── G1FitTypes.cs
│   │   └── GeometryTypes.cs
│   └── Sorting/
│       └── TopologicalSorter.cs
├── DotNetToolbox.Data.Csv/
│   ├── CsvLineParser.cs
│   ├── CsvWriter.cs
│   ├── CsvWriterOptions.cs
│   └── CsvDataReader.cs
├── DotNetToolbox.Data.SqlServer/
│   ├── Schema/
│   │   ├── ColumnMeta.cs
│   │   ├── ISchemaService.cs
│   │   └── SchemaService.cs
│   ├── Coerce/
│   │   └── DbValueCoercer.cs
│   ├── Bulk/
│   │   └── SqlBulkLoader.cs
│   └── Validation/
│       └── SqlIdentifierValidator.cs
└── DotNetToolbox.Tests/
    ├── Algorithms/
    │   ├── Geometry/
    │   │   └── Clothoid*Tests.cs
    │   └── TopologicalSorterTests.cs
    ├── Data.Csv/
    │   ├── CsvLineParserTests.cs
    │   ├── CsvWriterTests.cs
    │   └── CsvDataReaderTests.cs
    ├── Data.SqlServer/
    │   ├── DbValueCoercerTests.cs
    │   ├── SqlIdentifierValidatorTests.cs
    │   └── Integration/
    │       ├── SchemaServiceIntegrationTests.cs
    │       └── SqlBulkLoaderIntegrationTests.cs
    └── TestHelpers/
        └── SqlServerFixture.cs
```

## Library Summaries

### DotNetToolbox.Algorithms

Generic graph and planar path-geometry algorithms with no runtime dependencies.

**Key types:**

| Type | Use Case |
|---|---|
| `TopologicalSorter<T>` | Stable dependency ordering |
| `ClothoidEvaluator` | Evaluate and sample known clothoid parameters |
| `ClothoidFitter` | Fit one canonical G1 clothoid from two directed tangent poses |

```csharp
using DotNetToolbox.Algorithms.Sorting;

var tables = new[] { "Orders", "Customers", "Items" };
var fks    = new[] { ("Orders", "Customers"), ("Orders", "Items") };

// Result: ["Customers", "Items", "Orders"] — dependencies before dependents
var sorted = TopologicalSorter<string>.Sort(tables, fks);
```

Use when you need to order entities by dependency (FK delete order, task sequencing).

The geometry API uses metres and radians and stays independent of vehicle and UI
semantics. A caller must convert vehicle body heading plus forward/reverse direction
into the directed path tangent before calling `ClothoidFitter.TryFitG1`.

```csharp
using DotNetToolbox.Algorithms.Geometry;

var start = new Pose2D(0d, 0d, 0d);
var end = new Pose2D(2d, 2d, Math.PI / 2d);

if (ClothoidFitter.TryFitG1(start, end, out var fit, out var failure))
{
    var samples = ClothoidEvaluator.Sample(fit.Parameters, stepLength: 0.05d);
}
```

The fitter uses fixed 2048-subinterval composite Simpson quadrature. This is a
small deterministic implementation, not an adaptive integrator with a certified
error bound. See [Algorithms API](api/API_Algorithms.md) for its contract and limits.

---

### DotNetToolbox.Data.Csv

RFC 4180 compliant CSV parsing and writing with zero external dependencies.

**Key types:**

| Type | Use Case |
|---|---|
| `CsvLineParser` | Parse a single CSV line to `string[]` |
| `CsvWriter` | Write rows to a CSV file with configurable DateTime format |
| `CsvDataReader` | Stream a CSV file as `IDataReader` (for `SqlBulkCopy`) |

```csharp
using DotNetToolbox.Data.Csv;

// Write
var options = new CsvWriterOptions { DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fffffff" };
await CsvWriter.WriteAsync("output.csv", headers, rows, options);

// Stream into SqlBulkCopy
await using var reader = new CsvDataReader("input.csv", headers);
// pass reader to SqlBulkLoader
```

---

### DotNetToolbox.Data.SqlServer

SQL Server schema inspection, schema-first type coercion, and high-throughput bulk loading.

**Key types:**

| Type | Use Case |
|---|---|
| `SchemaService` | Query `sys.columns` once, cache `Dictionary<string, ColumnMeta>` |
| `DbValueCoercer` | Convert raw CSV `string` values to the correct CLR type per schema |
| `SqlBulkLoader` | Wrap `SqlBulkCopy` with schema-aware column mapping and KeepIdentity support |
| `SqlIdentifierValidator` | Validate and quote SQL identifiers; blocks injection |

```csharp
using DotNetToolbox.Data.SqlServer.Schema;
using DotNetToolbox.Data.SqlServer.Coerce;
using DotNetToolbox.Data.SqlServer.Bulk;

var schemaSvc = new SchemaService();
var schema    = await schemaSvc.GetColumnMapAsync(conn, "dbo.HistoricalVehicles");

// Coerce each CSV value before insert
var clrValue  = DbValueCoercer.Coerce("HistoryTime", "2024-01-15 08:30:00.0000000", schema);

// Bulk load from CsvDataReader
await SqlBulkLoader.LoadAsync(conn, txn, "dbo.HistoricalVehicles",
    csvDataReader, schema, keepIdentity: true, batchSize: 5000);
```

---

## Test Project Layout

All libraries share a single test project `DotNetToolbox.Tests`. Tests are organised by library in subfolders.

Integration tests are isolated by `[Trait("Category", "Integration")]` and require the `TOOLBOX_TEST_CONN`
environment variable pointing to a SQL Server Developer Edition or LocalDB instance.

See `AGENTS.md` for test run commands.
