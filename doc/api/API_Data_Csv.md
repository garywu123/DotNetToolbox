# API: DotNetToolbox.Data.Csv

## Namespace: `DotNetToolbox.Data.Csv`

### `CsvLineParser`

`public static class CsvLineParser`

```csharp
public static string[] Parse(string line)
```

Parses a single CSV line into a `string[]` following RFC 4180 quoting rules.

### `CsvWriterOptions`

`public sealed class CsvWriterOptions`

- `string DateTimeFormat { get; init; }` (default `yyyy-MM-dd HH:mm:ss.fffffff`)
- `Encoding Encoding { get; init; }` (default UTF-8 BOM)
- `string NewLine { get; init; }` (default `\r\n`)

### `CsvWriter`

`public static class CsvWriter`

```csharp
public static Task WriteAsync(
    string path,
    IEnumerable<string> headers,
    IEnumerable<IEnumerable<object?>> rows,
    CsvWriterOptions? options = null,
    CancellationToken ct = default)

public static Task WriteFromReaderAsync(
    string path,
    IDataReader reader,
    CsvWriterOptions? options = null,
    CancellationToken ct = default)
```

Writes CSV with quoting/escaping and configurable DateTime formatting.

### `CsvDataReader`

`public sealed class CsvDataReader : IDataReader, IAsyncDisposable`

```csharp
public CsvDataReader(string path, IReadOnlyList<string> expectedHeaders)
```

Streams a CSV file row-by-row as `IDataReader` (intended for `SqlBulkCopy`).

