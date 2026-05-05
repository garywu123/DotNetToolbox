# IP-03: DotNetToolbox.Data.SqlServer

## Overview

| Item | Value |
|---|---|
| Target library | `DotNetToolbox.Data.SqlServer` |
| Project path | `src/DotNetToolbox.Data.SqlServer/DotNetToolbox.Data.SqlServer.csproj` |
| Test project | `src/DotNetToolbox.Tests/DotNetToolbox.Tests.csproj` (shared) |
| Spec | `doc/spec/Spec_Data_SqlServer.md` |
| NuGet deps | `Microsoft.Data.SqlClient` 5.* |
| DB required | Integration tests only (`TOOLBOX_TEST_CONN` env var) |
| Depends on IP | IP-01 (`TopologicalSorter`), IP-02 (`CsvDataReader`) |

---

## Implementation Order

`SqlIdentifierValidator` → `ColumnMeta` + `ISchemaService` + `SchemaService` → `DbValueCoercer` → `SqlBulkLoader`

---

## Deliverables

### `src/DotNetToolbox.Data.SqlServer/Validation/SqlIdentifierValidator.cs`

**Class:** `public static partial class SqlIdentifierValidator` — namespace `DotNetToolbox.Data.SqlServer.Validation`

**Methods:**
```
bool   IsValid(string identifier)
string Quote(string identifier)           → throws ArgumentException when invalid
string QuoteQualified(string qualifiedName) → throws ArgumentException when invalid
```

**Responsibilities:**
- Use a source-generated `[GeneratedRegex]` for a single-part pattern: plain identifier `^[A-Za-z_][A-Za-z0-9_]*$` or bracket-quoted `^\[[^\]]+\]$`
- `IsValid`: split on `.`, accept 1 or 2 parts, each part must match the single-part regex; return `false` for null/whitespace without throwing
- `Quote`: if already starts with `[`, return unchanged; otherwise wrap in `[...]`
- `QuoteQualified`: split on `.`, `Quote` each part, rejoin with `.`

**Boundary conditions:**

| Condition | Behaviour |
|---|---|
| `null` or whitespace | `IsValid` returns `false`; `Quote`/`QuoteQualified` throw `ArgumentException` |
| Starts with digit (`"1Table"`) | `IsValid` returns `false` |
| Contains injection chars (`;`, `--`, `'`) | `IsValid` returns `false` |
| Three-part name (`a.b.c`) | `IsValid` returns `false` |
| Already bracket-quoted (`"[My Table]"`) | `IsValid` returns `true`; `Quote` returns as-is |
| Schema-qualified plain (`"dbo.MyTable"`) | `IsValid` returns `true`; `QuoteQualified` returns `"[dbo].[MyTable]"` |

---

### `src/DotNetToolbox.Data.SqlServer/Schema/ColumnMeta.cs`

**Type:** `public sealed record ColumnMeta` — namespace `DotNetToolbox.Data.SqlServer.Schema`

**Positional properties:** `string TypeName`, `bool IsNullable`, `int MaxLength`, `byte Precision`, `byte Scale`

`TypeName` must be stored in lowercase (normalise on construction or at the read site in `SchemaService`).

---

### `src/DotNetToolbox.Data.SqlServer/Schema/ISchemaService.cs`

**Interface:** `public interface ISchemaService` — namespace `DotNetToolbox.Data.SqlServer.Schema`

**Methods:**
```
Task<IReadOnlyDictionary<string, ColumnMeta>> GetColumnMapAsync(
    SqlConnection connection, string tableName, CancellationToken ct)

void ClearCache()
```

---

### `src/DotNetToolbox.Data.SqlServer/Schema/SchemaService.cs`

**Class:** `public sealed class SchemaService : ISchemaService` — namespace `DotNetToolbox.Data.SqlServer.Schema`

**Responsibilities:**
- Hold a `ConcurrentDictionary` keyed by `"{connectionString}::{tableName.ToUpperInvariant()}"`
- On cache miss: run the `sys.columns`/`sys.types` query (see spec for exact SQL), build result with `StringComparer.OrdinalIgnoreCase`, store in cache
- Normalise `TypeName` to lowercase before storing in `ColumnMeta`
- `ClearCache()`: call `_cache.Clear()`

**Boundary conditions:**

| Condition | Behaviour |
|---|---|
| `tableName` fails `IsValid` | Skip query; return empty `IReadOnlyDictionary` immediately |
| Table does not exist in DB | `OBJECT_ID` returns null → zero rows → return empty dict (cached) |
| Same `(conn, table)` called twice | Second call returns cached value; no DB round-trip |
| Table name casing differs (`dbo.TestTable` vs `dbo.TESTTABLE`) | Same cache hit (key is uppercased) |
| Connection is closed | Caller responsibility; behaviour of closed connection is undefined |

**Note:** `ClearCache` makes the *next* call re-query; it does not invalidate mid-flight calls.

---

### `src/DotNetToolbox.Data.SqlServer/Coerce/DbValueCoercer.cs`

**Class:** `public static class DbValueCoercer` — namespace `DotNetToolbox.Data.SqlServer.Coerce`

**Method:**
```
object Coerce(string columnName, string? raw, IReadOnlyDictionary<string, ColumnMeta> schema)
```

**Responsibilities:**
- **NULL guard first** (before any schema lookup): if `raw` is `null`, empty, or equals `"NULL"` (case-insensitive) → return `DBNull.Value`
- Lookup `columnName` in `schema` (case-insensitive); if not found → return `raw` unchanged
- Dispatch on `meta.TypeName` using a `switch` expression; use `TryParse` for every numeric/date/bool type; on `TryParse` failure → return `raw` unchanged (never throw)
- `varchar`/`nvarchar`/`char`/`nchar`/`text`/`ntext` and any unrecognised type → return `raw` unchanged (no coercion attempted)
- All numeric `TryParse` calls use `CultureInfo.InvariantCulture`

**Boundary conditions:**

| Condition | Behaviour |
|---|---|
| `raw` is `null` | `DBNull.Value` — even for non-nullable columns |
| `raw` is `""` | `DBNull.Value` |
| `raw` is `"NULL"` (any case) | `DBNull.Value` |
| `varchar` column, `raw` is `"007"` | Returns `"007"` — **never** coerced to int |
| `nvarchar` column, `raw` is `"42"` | Returns `"42"` — string pass-through |
| `int` column, `raw` is `"not-a-number"` | Returns `"not-a-number"` — `TryParse` fails, return raw |
| Column not in schema | Returns `raw` as string |
| `bit` column, `raw` is `"1"` | Returns `true` (bool) |
| `bit` column, `raw` is `"0"` | Returns `false` (bool) |
| `bit` column, `raw` is `"true"` | `bool.TryParse` → returns `true` |

---

### `src/DotNetToolbox.Data.SqlServer/Bulk/SqlBulkLoader.cs`

**Class:** `public static class SqlBulkLoader` — namespace `DotNetToolbox.Data.SqlServer.Bulk`

**Method:**
```
Task LoadAsync(
    SqlConnection connection,
    SqlTransaction? transaction,
    string tableName,
    IDataReader dataReader,
    IReadOnlyDictionary<string, ColumnMeta> schema,
    IEnumerable<string>? ignoreColumns = null,
    bool keepIdentity = false,
    int batchSize = 5000,
    CancellationToken ct = default)
```

**Responsibilities:**
- Validate `batchSize > 0` — throw `ArgumentOutOfRangeException` immediately if not
- Call `SqlIdentifierValidator.QuoteQualified(tableName)` — throws `ArgumentException` on invalid name
- Build column mappings: iterate `dataReader.GetName(i)` for all columns, skip those in `ignoreColumns` (case-insensitive) and those not in `schema`
- Set `SqlBulkCopyOptions.KeepIdentity` when `keepIdentity = true`; use `SqlBulkCopyOptions.Default` otherwise
- Set `EnableStreaming = true` on the `SqlBulkCopy` instance
- Call `WriteToServerAsync(dataReader, ct)` — propagate cancellation and exceptions to caller

**Boundary conditions:**

| Condition | Behaviour |
|---|---|
| `batchSize = 0` | Throw `ArgumentOutOfRangeException` before any DB operation |
| Invalid `tableName` | `ArgumentException` thrown by `QuoteQualified` |
| `ignoreColumns` is null | Treated as empty — no columns excluded |
| Reader column not in `schema` | Silently excluded from column mappings |
| `keepIdentity = false` | Do NOT issue `SET IDENTITY_INSERT` — `SqlBulkCopyOptions.Default` only |
| `keepIdentity = true` | `SqlBulkCopyOptions.KeepIdentity`; caller must have issued `SET IDENTITY_INSERT ON` in the same transaction |
| `transaction` is null | `SqlBulkCopy` operates without a transaction (auto-commit per batch) |
| Cancellation mid-stream | `OperationCanceledException` propagated; transaction state is caller's responsibility |

---

## Test Setup

**File:** `src/DotNetToolbox.Tests/TestHelpers/TestSchema.sql`

Run once against the test DB (or auto-apply in `SqlServerFixture.InitializeAsync`):

```sql
IF OBJECT_ID('dbo.TestDimTable', 'U') IS NOT NULL DROP TABLE dbo.TestDimTable;

CREATE TABLE dbo.TestDimTable (
    Id             INT              NOT NULL IDENTITY(1,1),
    BigId          BIGINT           NOT NULL,
    Name           NVARCHAR(200)    NULL,
    Code           VARCHAR(10)      NULL,
    Amount         DECIMAL(18, 4)   NULL,
    Rate           FLOAT            NULL,
    IsActive       BIT              NOT NULL DEFAULT 1,
    CreatedAt      DATETIME2(7)     NOT NULL,
    EffectiveDate  DATE             NULL,
    RowGuid        UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
    CustomerId     VARCHAR(50)      NOT NULL,
    CONSTRAINT PK_TestDimTable PRIMARY KEY (CustomerId, Id)
);
```

**File:** `src/DotNetToolbox.Tests/TestHelpers/SqlServerFixture.cs`

**Class:** `public sealed class SqlServerFixture : IAsyncLifetime`

**Responsibilities:**
- `InitializeAsync`: read `TOOLBOX_TEST_CONN` env var (throw `InvalidOperationException` when missing), open `SqlConnection`, run `TestSchema.sql` DDL if the table does not exist
- `DisposeAsync`: dispose the connection
- Expose `SqlConnection Connection { get; }` for test classes
- Used via `IClassFixture<SqlServerFixture>` on each integration test class

---

## Tests

### Unit: `src/DotNetToolbox.Tests/Data.SqlServer/DbValueCoercerTests.cs`

Test helper — build a single-column schema inline:
```
Schema(colName, typeName) → Dictionary<string, ColumnMeta> with one entry
```

| # | Test Name | Schema type | Input | Expected result |
|---|---|---|---|---|
| 1 | `Coerce_NullRaw_ReturnsDbNull` | `int` | `null` | `DBNull.Value` |
| 2 | `Coerce_EmptyString_ReturnsDbNull` | `int` | `""` | `DBNull.Value` |
| 3 | `Coerce_LiteralNULL_ReturnsDbNull` | `int` | `"NULL"` | `DBNull.Value` |
| 4 | `Coerce_LiteralNullLowercase_ReturnsDbNull` | `int` | `"null"` | `DBNull.Value` |
| 5 | `Coerce_IntColumn_ValidRaw_ReturnsInt` | `int` | `"42"` | `42` (type `int`) |
| 6 | `Coerce_BigIntColumn_ReturnsLong` | `bigint` | `"9876543210"` | `9876543210L` |
| 7 | `Coerce_BitColumn_One_ReturnsTrue` | `bit` | `"1"` | `true` |
| 8 | `Coerce_BitColumn_Zero_ReturnsFalse` | `bit` | `"0"` | `false` |
| 9 | `Coerce_DecimalColumn_ReturnsDecimal` | `decimal` | `"1234.5678"` | `1234.5678m` |
| 10 | `Coerce_DateTime2Column_ReturnsDateTime` | `datetime2` | `"2024-01-15 08:30:00.0000000"` | `DateTime` with correct value |
| 11 | `Coerce_DateColumn_ReturnsDateOnly` | `date` | `"2024-01-15"` | `DateTime` with time `00:00:00` |
| 12 | `Coerce_GuidColumn_ReturnsGuid` | `uniqueidentifier` | valid GUID string | `Guid` |
| 13 | `Coerce_VarcharColumn_LeadingZero_PassThrough` | `varchar` | `"007"` | `"007"` (string, not int) |
| 14 | `Coerce_NvarcharColumn_NumericString_PassThrough` | `nvarchar` | `"42"` | `"42"` (string) |
| 15 | `Coerce_UnknownColumn_ReturnsRawString` | (not in schema) | `"anything"` | `"anything"` |
| 16 | `Coerce_IntColumn_InvalidRaw_ReturnsRawString` | `int` | `"not-a-number"` | `"not-a-number"` |

### Unit: `src/DotNetToolbox.Tests/Data.SqlServer/SqlIdentifierValidatorTests.cs`

| # | Test Name | Input | Expected |
|---|---|---|---|
| 1 | `IsValid_SimpleName_ReturnsTrue` | `"TableName"` | `true` |
| 2 | `IsValid_BracketQuoted_ReturnsTrue` | `"[Table Name]"` | `true` |
| 3 | `IsValid_SchemaQualified_ReturnsTrue` | `"dbo.TableName"` | `true` |
| 4 | `IsValid_StartsWithDigit_ReturnsFalse` | `"1Table"` | `false` |
| 5 | `IsValid_ContainsSemicolon_ReturnsFalse` | `"a;b"` | `false` |
| 6 | `IsValid_Empty_ReturnsFalse` | `""` | `false` |
| 7 | `IsValid_NullInput_ReturnsFalse` | `null` | `false` |
| 8 | `Quote_PlainName_WrapsBrackets` | `"TableName"` | `"[TableName]"` |
| 9 | `Quote_AlreadyQuoted_ReturnsSame` | `"[Table]"` | `"[Table]"` |
| 10 | `QuoteQualified_TwoParts_QuotesBoth` | `"dbo.HistoricalVehicles"` | `"[dbo].[HistoricalVehicles]"` |
| 11 | `Quote_InvalidIdentifier_ThrowsArgumentException` | `"a;b"` | throws `ArgumentException` |

### Integration: `src/DotNetToolbox.Tests/Data.SqlServer/Integration/SchemaServiceIntegrationTests.cs`

All marked `[Trait("Category", "Integration")]`. Use `IClassFixture<SqlServerFixture>`.

| # | Test Name | Assertion |
|---|---|---|
| 1 | `GetColumnMapAsync_KnownTable_ReturnsAllColumns` | result has ≥ 10 entries for `dbo.TestDimTable` |
| 2 | `GetColumnMapAsync_UnknownTable_ReturnsEmptyDict` | result is empty for `dbo.NonExistentTable_XYZ` |
| 3 | `GetColumnMapAsync_IntColumn_TypeNameIsInt` | `result["Id"].TypeName` equals `"int"` |
| 4 | `GetColumnMapAsync_NvarcharColumn_MaxLengthIsBytes` | `result["Name"].MaxLength` equals `400` (NVARCHAR(200) = 400 bytes) |
| 5 | `GetColumnMapAsync_CalledTwice_ReturnsSameReference` | second call returns reference-equal result (cache hit) |
| 6 | `ClearCache_ThenCall_ReturnsFreshResult` | after `ClearCache()`, next call re-queries and returns a new dict reference |

### Integration: `src/DotNetToolbox.Tests/Data.SqlServer/Integration/SqlBulkLoaderIntegrationTests.cs`

All marked `[Trait("Category", "Integration")]`. Use `IClassFixture<SqlServerFixture>`.
Each test must clean up (`DELETE FROM dbo.TestDimTable WHERE CustomerId = 'TEST'`) in `IAsyncLifetime.DisposeAsync`.

| # | Test Name | Setup | Assertion |
|---|---|---|---|
| 1 | `LoadAsync_BasicInsert_RowsPresent` | 100-row `CsvDataReader` for `TestDimTable` | `SELECT COUNT(*)` returns 100 |
| 2 | `LoadAsync_KeepIdentity_PreservesIdValues` | 5 rows with explicit `Id` values, `keepIdentity=true` | Inserted rows have the specified `Id` values |
| 3 | `LoadAsync_Cancellation_RollsBack` | Cancel `CancellationToken` before load finishes | Row count remains 0 (inside explicit transaction, rolled back) |
| 4 | `LoadAsync_InvalidBatchSize_ThrowsImmediately` | `batchSize=0` | `ArgumentOutOfRangeException` before any DB operation |

---

## Definition of Done

- [ ] All 6 source files created in correct namespace subfolders
- [ ] `TestSchema.sql` and `SqlServerFixture.cs` created in `TestHelpers/`
- [ ] Unit tests (27 cases): all pass without a DB connection
- [ ] `dotnet build DotNetToolbox.sln` — zero errors, zero warnings
- [ ] `dotnet test --filter "Category!=Integration"` — all pass
- [ ] Integration tests (10 cases): all pass when `TOOLBOX_TEST_CONN` is set

## Validation (manual spot-checks)

1. `DbValueCoercer` with `varchar` schema + input `"007"` → result is the string `"007"`, not `7`
2. `SchemaService` called with `"dbo.TestDimTable"` and `"DBO.TESTDIMTABLE"` → same cache entry, single DB query
3. `SqlBulkLoader` with `keepIdentity=false` → no `SET IDENTITY_INSERT` statement in SQL Profiler trace
4. `SqlBulkLoader` with a cancelled token → `OperationCanceledException` propagates to caller
