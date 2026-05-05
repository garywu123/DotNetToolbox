# Spec: DotNetToolbox.Data.Csv

## Scope

This spec defines the public API contract for `DotNetToolbox.Data.Csv`.
The library has zero external dependencies and must compile on all .NET 8 platforms.

**Three components, intended use order:**

1. `CsvLineParser` — low-level line tokeniser (stateless utility)
2. `CsvWriter` — write typed data to CSV files
3. `CsvDataReader : IDataReader` — stream a CSV file as `IDataReader` (feeds `SqlBulkCopy`)

---

## Component 1: `CsvLineParser`

**Namespace:** `DotNetToolbox.Data.Csv`
**Type:** `public static class CsvLineParser`

### Purpose

Parse a single CSV line into an array of field values following RFC 4180.
Used internally by `CsvDataReader` and available as a public utility.

### Method Signature

```csharp
/// <summary>
/// Parses a single CSV line into an array of field strings following RFC 4180.
/// Quoted fields may contain commas, newlines, and escaped double-quotes (<c>""</c>).
/// </summary>
/// <param name="line">Raw line from a CSV file. Must not be null.</param>
/// <returns>
///   Array of field values with surrounding quotes stripped and <c>""</c> unescaped to <c>"</c>.
///   Returns a single-element array containing the empty string for an empty line.
/// </returns>
public static string[] Parse(string line)
```

### Behaviour Details

| Input | Output |
|---|---|
| `a,b,c` | `["a", "b", "c"]` |
| `"hello","world"` | `["hello", "world"]` |
| `"has,comma",b` | `["has,comma", "b"]` |
| `"say ""hi"""` | `["say \"hi\""]` — double-quote escape |
| `,b,` | `["", "b", ""]` — empty first and last |
| `""` | `[""]` — empty quoted field |
| `""` (empty string) | `[""]` |

### Constraints

- Does **not** handle multi-line fields (embedded newlines inside quotes). Callers that need multi-line
  must pre-join lines before calling `Parse`.
- Delimiter is always `,` (comma). Configurable delimiter is out of scope.
- Returns `string[]`, never `null`.

---

## Component 2: `CsvWriter`

**Namespace:** `DotNetToolbox.Data.Csv`
**Types:** `public static class CsvWriter`, `public sealed class CsvWriterOptions`

### Purpose

Write tabular data to a CSV file asynchronously.
Configurable `DateTime` format to preserve full precision for SQL Server `datetime2` columns.

### Method Signatures

```csharp
/// <summary>
/// Writes <paramref name="headers"/> and <paramref name="rows"/> to <paramref name="path"/> as CSV.
/// The file is created or overwritten. Encoding defaults to UTF-8 BOM.
/// </summary>
public static Task WriteAsync(
    string path,
    IEnumerable<string> headers,
    IEnumerable<IEnumerable<object?>> rows,
    CsvWriterOptions? options = null,
    CancellationToken ct = default)

/// <summary>
/// Writes from an <see cref="IDataReader"/>. Columns are taken from the reader schema.
/// The reader is not closed by this method.
/// </summary>
public static Task WriteFromReaderAsync(
    string path,
    IDataReader reader,
    CsvWriterOptions? options = null,
    CancellationToken ct = default)
```

### CsvWriterOptions

```csharp
public sealed class CsvWriterOptions
{
    /// <summary>Format applied to DateTime and DateTimeOffset values.</summary>
    /// <remarks>Default: <c>yyyy-MM-dd HH:mm:ss.fffffff</c></remarks>
    public string DateTimeFormat { get; init; } = "yyyy-MM-dd HH:mm:ss.fffffff";

    /// <summary>Encoding for the output file. Default: UTF-8 with BOM.</summary>
    public Encoding Encoding { get; init; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    /// <summary>Line terminator. Default: <c>\r\n</c> (Windows / RFC 4180).</summary>
    public string NewLine { get; init; } = "\r\n";
}
```

### Value Serialisation Rules

| CLR Type | Serialisation |
|---|---|
| `DBNull.Value` | Empty string (no quotes) |
| `null` | Empty string (no quotes) |
| `DateTime` | Formatted with `DateTimeFormat` option |
| `DateTimeOffset` | Formatted with `DateTimeFormat` option |
| All other | `value.ToString()` |

**Quoting rule:** A field is quoted with `"..."` when it contains a comma, a double-quote, a newline,
or a carriage return. Double-quotes inside a field are escaped as `""`.

### Validation Checks

1. Headers and row column counts do not need to match exactly — writer does not validate row width
2. `path` is validated by the OS — invalid path throws `IOException` from the underlying `StreamWriter`
3. Empty `rows` writes only the header line

---

## Component 3: `CsvDataReader`

**Namespace:** `DotNetToolbox.Data.Csv`
**Type:** `public sealed class CsvDataReader : IDataReader, IAsyncDisposable`

### Purpose

Streams a CSV file row-by-row as an `IDataReader` so it can be passed directly to `SqlBulkCopy`.
This avoids loading the entire file into memory.

### Constructor

```csharp
/// <summary>
/// Opens <paramref name="path"/> for streaming. The file is held open until
/// <see cref="Dispose"/> or <see cref="DisposeAsync"/> is called.
/// </summary>
/// <param name="path">Path to the CSV file.</param>
/// <param name="expectedHeaders">
///   Column names in the order they appear in the CSV header row.
///   Used to implement <see cref="GetName"/> and <see cref="GetOrdinal"/>.
/// </param>
public CsvDataReader(string path, IReadOnlyList<string> expectedHeaders)
```

### IDataReader Members

| Member | Contract |
|---|---|
| `bool Read()` | Advances to next row. Returns `false` at EOF. Parses current line via `CsvLineParser`. |
| `object GetValue(int i)` | Returns the raw `string` value for column `i`. Returns `DBNull.Value` if the field is empty. |
| `int FieldCount` | Number of columns from the header row. |
| `string GetName(int i)` | Returns the column name from `expectedHeaders[i]`. |
| `int GetOrdinal(string name)` | Case-insensitive lookup of column name → ordinal. |
| `bool IsDBNull(int i)` | Returns `true` when the field is empty string. |
| `void Close()` | Closes and disposes the underlying `StreamReader`. |
| `void Dispose()` | Calls `Close()`. |
| `ValueTask DisposeAsync()` | Async dispose of underlying file handle. |

### Members Returning `NotSupportedException`

The following `IDataReader` members throw `NotSupportedException` because
`SqlBulkCopy` does not call them:
`GetSchemaTable`, `NextResult`, `GetBoolean`, `GetByte`, `GetChar`, `GetDateTime`,
`GetDecimal`, `GetDouble`, `GetFloat`, `GetGuid`, `GetInt16`, `GetInt32`, `GetInt64`,
`GetString`, `GetData`, `GetValues`.

**Rationale:** `SqlBulkCopy` only calls `Read()`, `GetValue(int)`, `FieldCount`, `GetName(int)`.
Implementing the full type-specific Get* methods would require schema knowledge that belongs in
`DotNetToolbox.Data.SqlServer.Coerce.DbValueCoercer`.

### Behaviour Details

| Case | Behaviour |
|---|---|
| CSV first row is header | Header row consumed in constructor, not returned by `Read()` |
| Empty CSV field | `GetValue(i)` returns `DBNull.Value` |
| Non-empty field | `GetValue(i)` returns the raw `string` |
| `Read()` before file is opened | Always open; constructor opens the file |
| `Read()` after EOF | Returns `false` indefinitely |
| File not found | `FileNotFoundException` thrown in constructor |

### Usage Example

```csharp
// Used in DotNetToolbox.Data.SqlServer SqlBulkLoader:
var headers = schema.Keys.OrderBy(k => columnOrdinal[k]).ToList();

await using var csvReader = new CsvDataReader("import.csv", headers);
using var bulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.KeepIdentity, txn);
bulk.DestinationTableName = "dbo.HistoricalVehicles";

foreach (var col in headers)
    bulk.ColumnMappings.Add(col, col);

await bulk.WriteToServerAsync(csvReader);
```

### Validation Checks

1. `FieldCount` equals header column count after constructor
2. `Read()` returns `true` for each data row, `false` at EOF
3. `GetValue(i)` returns `DBNull.Value` for empty CSV field
4. `GetValue(i)` returns raw string for non-empty CSV field
5. `GetOrdinal("colname")` is case-insensitive
6. `DisposeAsync` does not throw even if already disposed
