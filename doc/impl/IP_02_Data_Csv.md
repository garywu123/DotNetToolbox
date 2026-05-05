# IP-02: Toolbox.Data.Csv

## Overview

| Item | Value |
|---|---|
| Target library | `Toolbox.Data.Csv` |
| Project path | `src/Toolbox.Data.Csv/Toolbox.Data.Csv.csproj` |
| Test project | `src/Toolbox.Tests/Toolbox.Tests.csproj` (shared) |
| Spec | `doc/spec/Spec_Data_Csv.md` |
| NuGet deps | None (BCL only) |
| DB required | No |
| Depends on IP | None — implement after or in parallel with IP-01 |

---

## Implementation Order

`CsvLineParser` → `CsvWriterOptions` + `CsvWriter` → `CsvDataReader`

---

## Deliverables

### `src/Toolbox.Data.Csv/CsvLineParser.cs`

**Class:** `public static class CsvLineParser` — namespace `Toolbox.Data.Csv`

**Method:** `public static string[] Parse(string line)`

**Responsibilities:**
- State-machine parser: track `inQuotes` flag character by character
- When inside quotes: `""` → single `"` (unescape); closing `"` → exit quoted mode
- When outside quotes: `,` → field boundary; `"` → enter quoted mode
- Append each completed field to a list; add final field after loop ends
- Return `string[]`, never null

**Boundary conditions:**

| Condition | Behaviour |
|---|---|
| `null` input | Throw `ArgumentNullException` |
| Empty string `""` | Return `[""]` — one element, empty string |
| No commas, no quotes | Return single-element array |
| Field is `""` (quoted empty) | Return `[""]` — empty string, not null |
| Leading/trailing comma (e.g. `,b,`) | Empty strings at those positions |
| `""` inside quoted field | Unescape to single `"` |

---

### `src/Toolbox.Data.Csv/CsvWriterOptions.cs`

**Class:** `public sealed class CsvWriterOptions` — namespace `Toolbox.Data.Csv`

**Properties (all `init`-only with defaults):**

| Property | Type | Default |
|---|---|---|
| `DateTimeFormat` | `string` | `"yyyy-MM-dd HH:mm:ss.fffffff"` |
| `Encoding` | `Encoding` | `UTF8Encoding(emitBOM: true)` |
| `NewLine` | `string` | `"\r\n"` (RFC 4180 / Windows) |

---

### `src/Toolbox.Data.Csv/CsvWriter.cs`

**Class:** `public static class CsvWriter` — namespace `Toolbox.Data.Csv`

**Methods:**

```
Task WriteAsync(string path, IEnumerable<string> headers,
    IEnumerable<IEnumerable<object?>> rows,
    CsvWriterOptions? options, CancellationToken ct)

Task WriteFromReaderAsync(string path, IDataReader reader,
    CsvWriterOptions? options, CancellationToken ct)
```

**Responsibilities:**
- `WriteAsync`: open `StreamWriter` (bufferSize 65536, encoding from options), write header line, then each row; use `options.NewLine` as line terminator; do not close the StreamWriter before flushing
- `WriteFromReaderAsync`: delegate to `WriteAsync` by reading `FieldCount`/`GetName`/`GetValue` from reader; do not dispose the reader
- Private `SerialiseField(object? value, CsvWriterOptions)`: `null`/`DBNull.Value` → empty string; `DateTime`/`DateTimeOffset` → format with `DateTimeFormat`; all others → `.ToString()`
- Private `NeedsQuoting(string s)`: returns true when `s` contains `,`, `"`, `\r`, or `\n`
- When quoting: wrap with `"..."` and escape inner `"` as `""`

**Boundary conditions:**

| Condition | Behaviour |
|---|---|
| `null` value in row | Serialise as empty (no quotes) |
| `DBNull.Value` in row | Serialise as empty (no quotes) |
| `DateTime` value | Always use `DateTimeFormat`; never `.ToString()` default |
| Field contains comma | Quoted |
| Field contains double-quote | Quoted and inner `"` escaped as `""` |
| Zero rows | Write only the header line |
| `options` is null | Use `new CsvWriterOptions()` defaults |

---

### `src/Toolbox.Data.Csv/CsvDataReader.cs`

**Class:** `public sealed class CsvDataReader : IDataReader, IAsyncDisposable` — namespace `Toolbox.Data.Csv`

**Constructor:** `CsvDataReader(string path, IReadOnlyList<string> expectedHeaders)`

**Responsibilities:**
- Constructor: open `StreamReader`, read and discard the CSV header line, build `_ordinals` dictionary (`StringComparer.OrdinalIgnoreCase`)
- `Read()`: read next line from `StreamReader`, call `CsvLineParser.Parse`, store in `_current`; return `false` at EOF
- `GetValue(int i)`: return `DBNull.Value` when `_current[i]` is null or empty; otherwise return raw string
- `GetName(int i)`: return `expectedHeaders[i]`
- `GetOrdinal(string name)`: case-insensitive lookup in `_ordinals`; throw `KeyNotFoundException` on miss
- `IsDBNull(int i)`: return true when field is null or empty
- `Close()` / `Dispose()`: dispose `StreamReader`; safe to call multiple times
- `DisposeAsync()`: async dispose of `StreamReader`
- All other `IDataReader` typed getter methods (GetBoolean, GetInt32, etc.) and `GetSchemaTable`, `NextResult`: throw `NotSupportedException` — `SqlBulkCopy` does not call these

**Boundary conditions:**

| Condition | Behaviour |
|---|---|
| File not found | `FileNotFoundException` thrown in constructor |
| `Read()` before first call | Not valid — `_current` is null; only call after at least one `Read()` |
| `Read()` after EOF | Returns `false` consistently |
| Empty field in CSV | `GetValue(i)` returns `DBNull.Value` |
| `DisposeAsync` called twice | No exception (idempotent) |

---

## Tests

### Test class: `src/Toolbox.Tests/Data.Csv/CsvLineParserTests.cs`

| # | Test Name | Input | Expected |
|---|---|---|---|
| 1 | `Parse_SimpleThreeFields_ReturnsSplit` | `"a,b,c"` | `["a","b","c"]` |
| 2 | `Parse_QuotedComma_TreatedAsField` | `"\"a,b\",c"` | `["a,b","c"]` |
| 3 | `Parse_EscapedQuote_Unescaped` | `"\"say \"\"hi\"\"\""` | `["say \"hi\""]` |
| 4 | `Parse_EmptyFirstAndLast_ReturnsEmptyStrings` | `",b,"` | `["","b",""]` |
| 5 | `Parse_EmptyLine_ReturnsSingleEmptyString` | `""` | `[""]` |
| 6 | `Parse_QuotedEmptyField_ReturnsEmptyString` | `"\"\""` | `[""]` |
| 7 | `Parse_NullInput_ThrowsArgumentNull` | `null` | throws `ArgumentNullException` |

### Test class: `src/Toolbox.Tests/Data.Csv/CsvWriterTests.cs`

| # | Test Name | Scenario |
|---|---|---|
| 1 | `WriteAsync_DateTimeField_FormatsWithFullPrecision` | DateTime `2024-01-15 08:30:00` → `"2024-01-15 08:30:00.0000000"` |
| 2 | `WriteAsync_DbNullValue_WritesEmptyField` | `DBNull.Value` → empty (no quotes) |
| 3 | `WriteAsync_NullValue_WritesEmptyField` | `null` → empty |
| 4 | `WriteAsync_FieldWithComma_IsQuoted` | `"hello, world"` → `"\"hello, world\""` |
| 5 | `WriteAsync_FieldWithQuote_Escaped` | `a"b` → `"a""b"` |
| 6 | `WriteAsync_EmptyRows_WritesOnlyHeader` | zero rows → file contains only header line |
| 7 | `WriteAsync_CustomDateTimeFormat_Applied` | Options with format `"yyyy-MM-dd"` applied to DateTime |

### Test class: `src/Toolbox.Tests/Data.Csv/CsvDataReaderTests.cs`

| # | Test Name | Scenario |
|---|---|---|
| 1 | `CsvDataReader_FieldCount_MatchesHeaders` | 3-column CSV → `FieldCount == 3` |
| 2 | `Read_DataRow_ReturnsTrue` | Row exists → `Read()` returns `true` |
| 3 | `Read_AtEof_ReturnsFalse` | After last row → `Read()` returns `false` |
| 4 | `GetValue_EmptyField_ReturnsDbNull` | Empty CSV field → `DBNull.Value` |
| 5 | `GetValue_NonEmpty_ReturnsRawString` | `"hello"` field → `"hello"` |
| 6 | `GetOrdinal_CaseInsensitive` | `GetOrdinal("NAME")` == `GetOrdinal("name")` |
| 7 | `GetName_ReturnsHeader` | `GetName(0)` returns first header |
| 8 | `DisposeAsync_CalledTwice_NoThrow` | Double-dispose safe |

Test helper: write a temp CSV to a `Path.GetTempFileName()` file, read with `CsvDataReader`, delete in test cleanup (`IAsyncLifetime`).

---

## Definition of Done

- [ ] `CsvLineParser.cs`, `CsvWriterOptions.cs`, `CsvWriter.cs`, `CsvDataReader.cs` created
- [ ] All test classes and cases implemented and passing
- [ ] `dotnet build Toolbox.sln` — zero errors, zero warnings
- [ ] `dotnet test --filter "Category!=Integration"` — all pass
- [ ] Cross-platform: no Windows-specific APIs used (verify with `dotnet build -r linux-x64`)
