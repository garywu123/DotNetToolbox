using System.Collections.Concurrent;

using DotNetToolbox.Data.SqlServer.Validation;

using Microsoft.Data.SqlClient;

namespace DotNetToolbox.Data.SqlServer.Schema;

/// <summary>
/// Queries SQL Server system tables for column metadata and caches results.
/// </summary>
/// <remarks>Thread-safe: the internal cache is a <see cref="ConcurrentDictionary{TKey,TValue}"/>.</remarks>
public sealed class SchemaService : ISchemaService
{
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, ColumnMeta>> _cache = new();

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, ColumnMeta>> GetColumnMapAsync(
        SqlConnection connection,
        string tableName,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(tableName);

        if (!SqlIdentifierValidator.IsValid(tableName))
        {
            return new Dictionary<string, ColumnMeta>(StringComparer.OrdinalIgnoreCase);
        }

        var key = $"{connection.ConnectionString}::{tableName.ToUpperInvariant()}";
        if (_cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        const string sql = @"
SELECT
    c.name                        AS ColumnName,
    t.name                        AS TypeName,
    c.is_nullable                 AS IsNullable,
    c.max_length                  AS MaxLength,
    c.precision                   AS Precision,
    c.scale                       AS Scale
FROM sys.columns c
JOIN sys.types   t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID(@tableName);";

        using var cmd = new SqlCommand(sql, connection);
        _ = cmd.Parameters.AddWithValue("@tableName", tableName);

        var map = new Dictionary<string, ColumnMeta>(StringComparer.OrdinalIgnoreCase);
        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var columnName = reader.GetString(0);
            var typeName = reader.GetString(1).ToLowerInvariant();
            var isNullable = reader.GetBoolean(2);
            var maxLength = reader.GetInt16(3);
            var precision = reader.GetByte(4);
            var scale = reader.GetByte(5);

            map[columnName] = new ColumnMeta(typeName, isNullable, maxLength, precision, scale);
        }

        var result = (IReadOnlyDictionary<string, ColumnMeta>)map;
        _cache[key] = result;
        return result;
    }

    /// <inheritdoc />
    public void ClearCache()
    {
        _cache.Clear();
    }
}

