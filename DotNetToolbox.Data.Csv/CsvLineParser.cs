namespace DotNetToolbox.Data.Csv;

/// <summary>
/// Parses a single CSV line into an array of field strings following RFC 4180.
/// Quoted fields may contain commas, and escaped double-quotes (<c>""</c>).
/// </summary>
public static class CsvLineParser
{
    /// <summary>
    /// Parses a single CSV line into an array of field strings following RFC 4180.
    /// Quoted fields may contain commas, and escaped double-quotes (<c>""</c>).
    /// </summary>
    /// <param name="line">Raw line from a CSV file. Must not be null.</param>
    /// <returns>
    /// Array of field values with surrounding quotes stripped and <c>""</c> unescaped to <c>"</c>.
    /// Returns a single-element array containing the empty string for an empty line.
    /// </returns>
    /// <exception cref="ArgumentNullException">When <paramref name="line"/> is null.</exception>
    public static string[] Parse(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        if (line.Length == 0)
        {
            return [string.Empty];
        }

        var fields = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var index = 0; index < line.Length; index++)
        {
            var ch = line[index];

            if (inQuotes)
            {
                if (ch == '"')
                {
                    var nextIsQuote = index + 1 < line.Length && line[index + 1] == '"';
                    if (nextIsQuote)
                    {
                        current.Append('"');
                        index++;
                        continue;
                    }

                    inQuotes = false;
                    continue;
                }

                current.Append(ch);
                continue;
            }

            if (ch == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
                continue;
            }

            if (ch == '"')
            {
                inQuotes = true;
                continue;
            }

            current.Append(ch);
        }

        fields.Add(current.ToString());
        return fields.ToArray();
    }
}

