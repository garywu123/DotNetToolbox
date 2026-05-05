namespace DotNetToolbox.Data.SqlServer.Schema;

/// <summary>
/// Immutable per-column schema descriptor returned by <see cref="SchemaService"/>.
/// </summary>
/// <param name="TypeName">SQL type name (lower-case, e.g. <c>int</c>, <c>nvarchar</c>).</param>
/// <param name="IsNullable">Whether the column is nullable.</param>
/// <param name="MaxLength">Max length in bytes (<c>-1</c> means MAX).</param>
/// <param name="Precision">Numeric precision.</param>
/// <param name="Scale">Numeric scale.</param>
public sealed record ColumnMeta(
    string TypeName,
    bool IsNullable,
    int MaxLength,
    byte Precision,
    byte Scale);

