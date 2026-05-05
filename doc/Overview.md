# Toolbox — Architecture Overview

## Purpose

Toolbox is a set of focused, reusable .NET 8 class libraries extracted from the SyncTool project.
They contain no UI code and can be referenced by any .NET 8+ application.

## Library Dependency Graph

```
Toolbox.Algorithms
│
└─ (used by) Toolbox.Data.SqlServer
                  │
                  ├─ Toolbox.Algorithms  (FK topological ordering)
                  └─ Toolbox.Data.Csv    (CsvDataReader for SqlBulkCopy)

Toolbox.Data.Csv
│
└─ (used by) Toolbox.Data.SqlServer  (CsvDataReader)
             SyncTool.App            (CsvWriter for export)
```

`Toolbox.Algorithms` and `Toolbox.Data.Csv` have **zero NuGet dependencies**.
`Toolbox.Data.SqlServer` depends only on `Microsoft.Data.SqlClient`.

## Solution Structure

```
Toolbox/
├── Toolbox.sln
├── AGENTS.md
├── doc/
│   ├── Overview.md                 ← this file
│   ├── spec/
│   │   ├── Spec_Algorithms.md      ← API contract: TopologicalSorter
│   │   ├── Spec_Data_Csv.md        ← API contract: CsvLineParser, CsvWriter, CsvDataReader
│   │   └── Spec_Data_SqlServer.md  ← API contract: SchemaService, DbValueCoercer, SqlBulkLoader
│   ├── impl/
│   │   ├── IP_01_Algorithms.md     ← work order: code + tests for Toolbox.Algorithms
│   │   ├── IP_02_Data_Csv.md       ← work order: code + tests for Toolbox.Data.Csv
│   │   └── IP_03_Data_SqlServer.md ← work order: code + tests for Toolbox.Data.SqlServer
│   └── api/                        ← generated after implementation (do not edit by hand)
│       ├── API_Algorithms.md
│       ├── API_Data_Csv.md
│       └── API_Data_SqlServer.md
└── src/
    ├── Toolbox.Algorithms/
    │   └── Sorting/
    │       └── TopologicalSorter.cs
    ├── Toolbox.Data.Csv/
    │   ├── CsvLineParser.cs
    │   ├── CsvWriter.cs
    │   ├── CsvWriterOptions.cs
    │   └── CsvDataReader.cs
    ├── Toolbox.Data.SqlServer/
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
    └── Toolbox.Tests/
        ├── Algorithms/
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

### Toolbox.Algorithms

Generic graph algorithms with no runtime dependencies.

**Key type:** `TopologicalSorter<T>` (static helper class)

```csharp
using Toolbox.Algorithms.Sorting;

var tables = new[] { "Orders", "Customers", "Items" };
var fks    = new[] { ("Orders", "Customers"), ("Orders", "Items") };

// Result: ["Customers", "Items", "Orders"] — dependencies before dependents
var sorted = TopologicalSorter<string>.Sort(tables, fks);
```

Use when you need to order entities by dependency (FK delete order, task sequencing).

---

### Toolbox.Data.Csv

RFC 4180 compliant CSV parsing and writing with zero external dependencies.

**Key types:**

| Type | Use Case |
|---|---|
| `CsvLineParser` | Parse a single CSV line to `string[]` |
| `CsvWriter` | Write rows to a CSV file with configurable DateTime format |
| `CsvDataReader` | Stream a CSV file as `IDataReader` (for `SqlBulkCopy`) |

```csharp
using Toolbox.Data.Csv;

// Write
var options = new CsvWriterOptions { DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fffffff" };
await CsvWriter.WriteAsync("output.csv", headers, rows, options);

// Stream into SqlBulkCopy
await using var reader = new CsvDataReader("input.csv", headers);
// pass reader to SqlBulkLoader
```

---

### Toolbox.Data.SqlServer

SQL Server schema inspection, schema-first type coercion, and high-throughput bulk loading.

**Key types:**

| Type | Use Case |
|---|---|
| `SchemaService` | Query `sys.columns` once, cache `Dictionary<string, ColumnMeta>` |
| `DbValueCoercer` | Convert raw CSV `string` values to the correct CLR type per schema |
| `SqlBulkLoader` | Wrap `SqlBulkCopy` with schema-aware column mapping and KeepIdentity support |
| `SqlIdentifierValidator` | Validate and quote SQL identifiers; blocks injection |

```csharp
using Toolbox.Data.SqlServer.Schema;
using Toolbox.Data.SqlServer.Coerce;
using Toolbox.Data.SqlServer.Bulk;

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

All libraries share a single test project `Toolbox.Tests`. Tests are organised by library in subfolders.

Integration tests are isolated by `[Trait("Category", "Integration")]` and require the `TOOLBOX_TEST_CONN`
environment variable pointing to a SQL Server Developer Edition or LocalDB instance.

See `AGENTS.md` for test run commands.
