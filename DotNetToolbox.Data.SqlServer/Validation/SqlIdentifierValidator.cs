using System.Text.RegularExpressions;

namespace DotNetToolbox.Data.SqlServer.Validation;

/// <summary>
/// Validates and quotes SQL Server identifiers to guard against SQL injection via identifier manipulation.
/// </summary>
public static partial class SqlIdentifierValidator
{
    /// <summary>
    /// Returns true when <paramref name="identifier"/> is a valid SQL Server identifier.
    /// Accepts both plain identifiers (<c>TableName</c>) and bracket-quoted identifiers
    /// (<c>[Table Name]</c>). Optionally accepts schema-qualified forms (<c>dbo.Table</c>).
    /// </summary>
    /// <param name="identifier">Identifier to validate.</param>
    /// <returns>True when valid; otherwise false.</returns>
    public static bool IsValid(string? identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return false;
        }

        var parts = identifier.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length is < 1 or > 2)
        {
            return false;
        }

        return parts.All(p => SinglePartRegex().IsMatch(p));
    }

    /// <summary>
    /// Returns the bracket-quoted form of <paramref name="identifier"/>.
    /// Plain identifiers are wrapped: <c>TableName</c> → <c>[TableName]</c>.
    /// Already-quoted identifiers are returned as-is.
    /// </summary>
    /// <param name="identifier">Identifier to quote.</param>
    /// <returns>Bracket-quoted identifier.</returns>
    /// <exception cref="ArgumentException">When <paramref name="identifier"/> is invalid.</exception>
    public static string Quote(string identifier)
    {
        if (!IsValid(identifier))
        {
            throw new ArgumentException("Invalid SQL identifier.", nameof(identifier));
        }

        return identifier.StartsWith("[", StringComparison.Ordinal) ? identifier : $"[{identifier}]";
    }

    /// <summary>
    /// Splits a schema-qualified name into its parts, validates each part, and returns the
    /// bracket-quoted full name.
    /// </summary>
    /// <param name="qualifiedName">Qualified name with one or two parts.</param>
    /// <returns>Quoted qualified name (e.g. <c>[dbo].[MyTable]</c>).</returns>
    /// <exception cref="ArgumentException">When any part is invalid.</exception>
    public static string QuoteQualified(string qualifiedName)
    {
        if (!IsValid(qualifiedName))
        {
            throw new ArgumentException("Invalid SQL identifier.", nameof(qualifiedName));
        }

        var parts = qualifiedName.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join('.', parts.Select(Quote));
    }

    [GeneratedRegex("^(?:[A-Za-z_][A-Za-z0-9_]*|\\[[^\\]]+\\])$", RegexOptions.CultureInvariant)]
    private static partial Regex SinglePartRegex();
}
