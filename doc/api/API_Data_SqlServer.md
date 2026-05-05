# API: DotNetToolbox.Data.SqlServer

## Namespace: `DotNetToolbox.Data.SqlServer.Validation`

### `SqlIdentifierValidator`

```csharp
public static bool IsValid(string? identifier)
public static string Quote(string identifier)
public static string QuoteQualified(string qualifiedName)
```

Validates SQL identifiers and returns safely bracket-quoted forms.

## Namespace: `DotNetToolbox.Data.SqlServer.Schema`

### `ColumnMeta`

```csharp
public sealed record ColumnMeta(
    string TypeName,
    bool IsNullable,
    int MaxLength,
    byte Precision,
    byte Scale);
```

### `ISchemaService` / `SchemaService`

```csharp
public interface ISchemaService
{
    Task<IReadOnlyDictionary<string, ColumnMeta>> GetColumnMapAsync(
        SqlConnection connection,
        string tableName,
        CancellationToken ct = default);

    void ClearCache();
}
```

`SchemaService` caches results per connection string + table name.

## Namespace: `DotNetToolbox.Data.SqlServer.Coerce`

### `DbValueCoercer`

```csharp
public static object Coerce(
    string columnName,
    string? raw,
    IReadOnlyDictionary<string, ColumnMeta> schema)
```

Schema-first, `TryParse`-based type coercion. Null-like inputs return `DBNull.Value`.

## Namespace: `DotNetToolbox.Data.SqlServer.Bulk`

### `SqlBulkLoader`

```csharp
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

Wraps `SqlBulkCopy` with schema-aware column mapping.

