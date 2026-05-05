using System.Globalization;

using DotNetToolbox.Data.SqlServer.Schema;

namespace DotNetToolbox.Data.SqlServer.Coerce;

/// <summary>
/// Coerces raw string values (typically from CSV) into CLR values based on a SQL Server schema map.
/// </summary>
public static class DbValueCoercer
{
    /// <summary>
    /// Coerces <paramref name="raw"/> to the CLR type implied by <paramref name="schema"/> for <paramref name="columnName"/>.
    /// </summary>
    /// <param name="columnName">Column name.</param>
    /// <param name="raw">Raw string value from CSV. May be null or empty.</param>
    /// <param name="schema">Schema map from <see cref="SchemaService.GetColumnMapAsync"/>.</param>
    /// <returns>
    /// The coerced CLR value, or <see cref="DBNull.Value"/> for null-like inputs.
    /// If parsing fails or the column is not present in the schema, returns the raw string unchanged.
    /// </returns>
    public static object Coerce(
        string columnName,
        string? raw,
        IReadOnlyDictionary<string, ColumnMeta> schema)
    {
        ArgumentNullException.ThrowIfNull(columnName);
        ArgumentNullException.ThrowIfNull(schema);

        if (IsNullLike(raw))
        {
            return DBNull.Value;
        }

        if (!schema.TryGetValue(columnName, out var meta))
        {
            return raw!;
        }

        var type = meta.TypeName;
        return type switch
        {
            "int" or "smallint" or "tinyint" =>
                int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i) ? i : raw!,

            "bigint" =>
                long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l) ? l : raw!,

            "bit" =>
                raw == "1" ? true : raw == "0" ? false : (bool.TryParse(raw, out var b) ? b : raw!),

            "decimal" or "numeric" or "money" or "smallmoney" =>
                decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : raw!,

            "float" or "real" =>
                double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var dbl) ? dbl : raw!,

            "datetime" or "datetime2" or "smalldatetime" =>
                DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dt) ? dt : raw!,

            "datetimeoffset" =>
                DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto) ? dto : raw!,

            "date" =>
                DateTime.TryParseExact(raw, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : raw!,

            "uniqueidentifier" =>
                Guid.TryParse(raw, out var g) ? g : raw!,

            "varchar" or "nvarchar" or "char" or "nchar" or "text" or "ntext" => raw!,

            _ => raw!,
        };
    }

    private static bool IsNullLike(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return true;
        }

        return raw.Equals("NULL", StringComparison.OrdinalIgnoreCase);
    }
}

