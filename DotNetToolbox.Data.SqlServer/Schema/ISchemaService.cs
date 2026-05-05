using Microsoft.Data.SqlClient;

namespace DotNetToolbox.Data.SqlServer.Schema;

/// <summary>
/// Provides schema inspection services for SQL Server tables.
/// </summary>
public interface ISchemaService
{
    /// <summary>
    /// Returns column metadata for <paramref name="tableName"/> keyed by column name (case-insensitive).
    /// Returns an empty dictionary if the table does not exist in the database or <paramref name="tableName"/>
    /// fails validation.
    /// </summary>
    /// <param name="connection">Open SQL connection. Caller owns the connection lifecycle.</param>
    /// <param name="tableName">Table name, optionally schema-qualified.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Column map keyed by column name (case-insensitive).</returns>
    Task<IReadOnlyDictionary<string, ColumnMeta>> GetColumnMapAsync(
        SqlConnection connection,
        string tableName,
        CancellationToken ct = default);

    /// <summary>Removes all cached schema entries.</summary>
    void ClearCache();
}

