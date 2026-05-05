using System.Data;

using DotNetToolbox.Data.SqlServer.Schema;
using DotNetToolbox.Data.SqlServer.Validation;

using Microsoft.Data.SqlClient;

namespace DotNetToolbox.Data.SqlServer.Bulk;

/// <summary>
/// Wraps <see cref="SqlBulkCopy"/> with schema-aware column mapping.
/// </summary>
public static class SqlBulkLoader
{
    /// <summary>
    /// Bulk loads rows from <paramref name="dataReader"/> into <paramref name="tableName"/>.
    /// Column mappings are derived from columns present in both <paramref name="schema"/> and the reader.
    /// </summary>
    /// <param name="connection">Open SqlConnection. Caller owns the connection lifecycle.</param>
    /// <param name="transaction">Active transaction, or null for auto-commit.</param>
    /// <param name="tableName">Destination table name (validated by <see cref="SqlIdentifierValidator"/>).</param>
    /// <param name="dataReader">Source data. Caller owns disposal.</param>
    /// <param name="schema">Column map for the destination table.</param>
    /// <param name="ignoreColumns">Column names to exclude from mapping.</param>
    /// <param name="keepIdentity">When true, uses <see cref="SqlBulkCopyOptions.KeepIdentity"/>.</param>
    /// <param name="batchSize">Row count per batch. Default: 5000.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="batchSize"/> is not positive.</exception>
    /// <exception cref="ArgumentException">When <paramref name="tableName"/> is invalid.</exception>
    public static async Task LoadAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string tableName,
        IDataReader dataReader,
        IReadOnlyDictionary<string, ColumnMeta> schema,
        IEnumerable<string>? ignoreColumns = null,
        bool keepIdentity = false,
        int batchSize = 5000,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(connection);
        ArgumentNullException.ThrowIfNull(tableName);
        ArgumentNullException.ThrowIfNull(dataReader);
        ArgumentNullException.ThrowIfNull(schema);

        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize, "Batch size must be positive.");
        }

        if (!SqlIdentifierValidator.IsValid(tableName))
        {
            throw new ArgumentException("Invalid SQL identifier.", nameof(tableName));
        }

        var ignore = ignoreColumns is null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(ignoreColumns, StringComparer.OrdinalIgnoreCase);

        var options = keepIdentity ? SqlBulkCopyOptions.KeepIdentity : SqlBulkCopyOptions.Default;
        using var bulk = new SqlBulkCopy(connection, options, transaction)
        {
            DestinationTableName = SqlIdentifierValidator.QuoteQualified(tableName),
            BatchSize = batchSize,
        };

        for (var i = 0; i < dataReader.FieldCount; i++)
        {
            var name = dataReader.GetName(i);
            if (ignore.Contains(name))
            {
                continue;
            }

            if (schema.ContainsKey(name))
            {
                bulk.ColumnMappings.Add(name, name);
            }
        }

        await bulk.WriteToServerAsync(dataReader, ct);
    }
}

