# Spec: DotNetToolbox.Data.SqlServer

## Scope

This spec defines the public API contract for `DotNetToolbox.Data.SqlServer`.
The library depends on `DotNetToolbox.Algorithms` (FK ordering) and `DotNetToolbox.Data.Csv` (bulk load streaming).

**Four components:**

1. `SqlIdentifierValidator` — validate and safely quote SQL identifiers
2. `ColumnMeta` — immutable schema descriptor per column
3. `SchemaService` — inspect `sys.columns`, cache results
4. `DbValueCoercer` — schema-first, `TryParse`-based type coercion
5. `SqlBulkLoader` — wrap `SqlBulkCopy` with schema-aware column mapping

---

## Component 1: `SqlIdentifierValidator`

**Namespace:** `DotNetToolbox.Data.SqlServer.Validation`
**Type:** `public static class SqlIdentifierValidator`

### Purpose

Validate SQL identifiers supplied by configuration files or user input before embedding in
dynamic SQL strings. Guards against SQL injection via identifier manipulation.

### Method Signatures

```csharp
/// <summary>
/// Returns true when <paramref name="identifier"/> is a valid SQL Server identifier.
/// Accepts both plain identifiers (<c>TableName</c>) and bracket-quoted identifiers
/// (<c>[Table Name]</c>). Optionally accepts schema-qualified forms (<c>dbo.Table</c>).
/// </summary>
public static bool IsValid(string identifier)

/// <summary>
/// Returns the bracket-quoted form of <paramref name="identifier"/>.
/// Plain identifiers are wrapped: <c>TableName</c> → <c>[TableName]</c>.
/// Already-quoted identifiers are returned as-is.
/// </summary>
/// <exception cref="ArgumentException">
///   When <paramref name="identifier"/> fails <see cref="IsValid"/>.
/// </exception>
public static string Quote(string identifier)

/// <summary>
/// Splits a schema-qualified name into its parts, validates each part, and returns the
/// bracket-quoted full name.
/// </summary>
/// <example><c>"dbo.HistoricalVehicles"</c> → <c>"[dbo].[HistoricalVehicles]"</c></example>
/// <exception cref="ArgumentException">When any part is invalid.</exception>
public static string QuoteQualified(string qualifiedName)
```

### Validation Rules

Valid plain identifier pattern: `^[A-Za-z_][A-Za-z0-9_]*$`
Valid bracket-quoted pattern: `^\[[^\]]+\]$`
Valid schema-qualified: up to two `.`-separated parts, each matching one of the above patterns.

### Rejection Examples

| Input | Reason |
|---|---|
| `"1Table"` | Starts with digit |
| `"a;b"` | Contains semicolon |
| `"a--b"` | Contains comment marker |
| `""` | Empty string |
| `null` | Null reference |

---

## Component 2: `ColumnMeta`

**Namespace:** `DotNetToolbox.Data.SqlServer.Schema`
**Type:** `public sealed record ColumnMeta`

### Purpose

Immutable per-column schema descriptor returned by `SchemaService`.

### Properties

| Property | Type | Source column in `sys.columns` / `sys.types` |
|---|---|---|
| `TypeName` | `string` | `sys.types.name` (lower-case, e.g. `"int"`, `"varchar"`, `"datetime2"`) |
| `IsNullable` | `bool` | `sys.columns.is_nullable` |
| `MaxLength` | `int` | `sys.columns.max_length` (`-1` = MAX) |
| `Precision` | `byte` | `sys.columns.precision` |
| `Scale` | `byte` | `sys.columns.scale` |

```csharp
public sealed record ColumnMeta(
    string TypeName,
    bool   IsNullable,
    int    MaxLength,
    byte   Precision,
    byte   Scale);
```

---

## Component 3: `SchemaService`

**Namespace:** `DotNetToolbox.Data.SqlServer.Schema`
**Types:** `public interface ISchemaService`, `public sealed class SchemaService : ISchemaService`

### Purpose

Query `sys.columns` for a table and return a column-name-to-`ColumnMeta` dictionary.
Results are cached per `(connectionString, tableName)` key using a `ConcurrentDictionary`
to avoid redundant round-trips during a multi-table operation.

### Interface

```csharp
public interface ISchemaService
{
    /// <summary>
    /// Returns column metadata for <paramref name="tableName"/> keyed by column name
    /// (case-insensitive). Returns an empty dictionary if the table does not exist in the
    /// database or <paramref name="tableName"/> fails validation.
    /// </summary>
    Task<IReadOnlyDictionary<string, ColumnMeta>> GetColumnMapAsync(
        SqlConnection connection,
        string tableName,
        CancellationToken ct = default);

    /// <summary>Removes all cached schema entries.</summary>
    void ClearCache();
}
```

### SQL Query

```sql
SELECT
    c.name                        AS ColumnName,
    t.name                        AS TypeName,
    c.is_nullable                 AS IsNullable,
    c.max_length                  AS MaxLength,
    c.precision                   AS Precision,
    c.scale                       AS Scale
FROM sys.columns c
JOIN sys.types   t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID(@tableName);
```

### Behaviour Details

| Case | Behaviour |
|---|---|
| Table exists | Returns `IReadOnlyDictionary<string, ColumnMeta>` (case-insensitive `StringComparer.OrdinalIgnoreCase`) |
| Table does not exist | Returns `IReadOnlyDictionary<string, ColumnMeta>` with zero entries |
| `tableName` fails validation | Returns empty dictionary (no query issued) |
| Same `(conn, table)` called twice | Second call returns cached result without DB round-trip |
| Connection is closed | Caller must pass an open connection; behaviour of a closed connection is undefined |

### Cache Key

Cache key is `$"{connection.ConnectionString}::{tableName.ToUpperInvariant()}"`.

---

## Component 4: `DbValueCoercer`

**Namespace:** `DotNetToolbox.Data.SqlServer.Coerce`
**Type:** `public static class DbValueCoercer`

### Purpose

Convert a raw `string` value read from CSV into the correct CLR type for a given SQL Server column.
Uses a schema-first dispatch table — **never relies on exceptions for type detection**.

### Method Signature

```csharp
/// <summary>
/// Coerces <paramref name="raw"/> to the CLR type appropriate for
/// <paramref name="columnName"/> according to <paramref name="schema"/>.
/// Returns <see cref="DBNull.Value"/> when the value is null, empty, or "NULL" (case-insensitive).
/// Returns the raw string unchanged when the column is not in <paramref name="schema"/>.
/// </summary>
/// <param name="columnName">Column name (case-insensitive lookup).</param>
/// <param name="raw">Raw string from CSV. May be null or empty.</param>
/// <param name="schema">Schema map from <see cref="SchemaService.GetColumnMapAsync"/>.</param>
public static object Coerce(
    string columnName,
    string? raw,
    IReadOnlyDictionary<string, ColumnMeta> schema)
```

### NULL Guard (first check)

If `raw` is `null`, empty string `""`, or `"NULL"` (case-insensitive) → return `DBNull.Value`.

### Type Dispatch Table

| SQL Type(s) | CLR target | Method |
|---|---|---|
| `int`, `smallint`, `tinyint` | `int` | `int.TryParse` |
| `bigint` | `long` | `long.TryParse` |
| `bit` | `bool` | `raw == "1"` or `bool.TryParse` |
| `decimal`, `numeric`, `money`, `smallmoney` | `decimal` | `decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out _)` |
| `float`, `real` | `double` | `double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out _)` |
| `datetime`, `datetime2`, `smalldatetime` | `DateTime` | `DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _)` |
| `datetimeoffset` | `DateTimeOffset` | `DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out _)` |
| `date` | `DateTime` (Date only) | `DateTime.TryParseExact(raw, "yyyy-MM-dd", ...)` |
| `uniqueidentifier` | `Guid` | `Guid.TryParse` |
| `varchar`, `nvarchar`, `char`, `nchar`, `text`, `ntext` | `string` | Pass-through — **no numeric coercion attempted** |
| All other types | `string` | Pass-through |

**TryParse failure behaviour:** If `TryParse` returns `false`, return the raw string unchanged.
Do not throw. Let `SqlBulkCopy` surface the type mismatch as a proper error with column context.

### Critical: varchar/nvarchar Pass-Through

String column values are **never** inspected for numeric or boolean content.
This preserves values like `"007"`, `"0001"`, `" "` (space-only), `"TRUE"` exactly as written.

### STAGE_APPLY Note

When coercing for a staging table (e.g. `dbo.StageHistoricalVehicles`), pass the **staging table's**
schema, not the target table's schema. The staging table definition may differ from the target.

---

## Component 5: `SqlBulkLoader`

**Namespace:** `DotNetToolbox.Data.SqlServer.Bulk`
**Type:** `public static class SqlBulkLoader`

### Purpose

Wrap `SqlBulkCopy` with schema-aware column mapping, optional identity insert, configurable batch size,
and cancellation support.

### Method Signature

```csharp
/// <summary>
/// Bulk loads rows from <paramref name="dataReader"/> into <paramref name="tableName"/>.
/// Column mappings are derived from columns present in both <paramref name="schema"/> and the reader.
/// </summary>
/// <param name="connection">Open SqlConnection. Caller owns the connection lifecycle.</param>
/// <param name="transaction">Active transaction, or null for auto-commit.</param>
/// <param name="tableName">Destination table (bracket-quoted via SqlIdentifierValidator).</param>
/// <param name="dataReader">Source data. Caller owns disposal.</param>
/// <param name="schema">Column map from SchemaService for the destination table.</param>
/// <param name="ignoreColumns">Column names to exclude from the bulk copy mapping.</param>
/// <param name="keepIdentity">When true, uses SqlBulkCopyOptions.KeepIdentity.</param>
/// <param name="batchSize">Row count per batch. Default: 5000.</param>
/// <param name="ct">Cancellation token.</param>
public static Task LoadAsync(
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

### Column Mapping Logic

1. Get all column names from `dataReader.GetName(i)` for `i in [0, FieldCount)`
2. Intersect with `schema.Keys` (case-insensitive)
3. Exclude any names in `ignoreColumns`
4. Add one `SqlBulkCopyColumnMapping` per remaining column (source name → destination name)

### Identity Insert

When `keepIdentity = true`:
- `SqlBulkCopyOptions.KeepIdentity` is passed to the `SqlBulkCopy` constructor
- Caller must ensure `SET IDENTITY_INSERT [table] ON` is issued within the same transaction
  before calling `LoadAsync` (this method does not issue the SET statement)

### Transaction Handling

`SqlBulkLoader` does not manage transactions. Callers must:
- Pass an active `SqlTransaction` to participate in the caller's transaction
- Or pass `null` for auto-commit (each batch commits independently — use with caution)

### Validation Checks

1. Empty schema → no column mappings → `SqlBulkCopy` throws (caller error, not suppressed)
2. `batchSize <= 0` → `ArgumentOutOfRangeException`
3. `tableName` failing `SqlIdentifierValidator.IsValid` → `ArgumentException`
4. Cancellation mid-stream → `OperationCanceledException` propagated from `WriteToServerAsync`
5. After cancellation, the `SqlTransaction` is in a rolled-back state (caller must not commit)
