using System.Data;

namespace DotNetToolbox.Data.Csv;

/// <summary>
/// Writes tabular data to a CSV file.
/// </summary>
public static class CsvWriter
{
    /// <summary>
    /// Writes <paramref name="headers"/> and <paramref name="rows"/> to <paramref name="path"/> as CSV.
    /// The file is created or overwritten.
    /// </summary>
    /// <param name="path">Destination path.</param>
    /// <param name="headers">Header row field values.</param>
    /// <param name="rows">Rows to write. Each row is a sequence of field values.</param>
    /// <param name="options">Writer options. When null, defaults are used.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task WriteAsync(
        string path,
        IEnumerable<string> headers,
        IEnumerable<IEnumerable<object?>> rows,
        CsvWriterOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(rows);

        options ??= new CsvWriterOptions();

        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, bufferSize: 65536, useAsync: true);
        await using var writer = new StreamWriter(stream, options.Encoding, bufferSize: 65536, leaveOpen: false)
        {
            NewLine = options.NewLine,
        };

        await writer.WriteLineAsync(SerialiseRow(headers.Cast<object?>(), options).AsMemory(), ct);

        foreach (var row in rows)
        {
            ct.ThrowIfCancellationRequested();
            await writer.WriteLineAsync(SerialiseRow(row, options).AsMemory(), ct);
        }

        await writer.FlushAsync(ct);
    }

    /// <summary>
    /// Writes from an <see cref="IDataReader"/>. Columns are taken from the reader schema.
    /// The reader is not closed by this method.
    /// </summary>
    /// <param name="path">Destination path.</param>
    /// <param name="reader">Data reader to stream from.</param>
    /// <param name="options">Writer options. When null, defaults are used.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task WriteFromReaderAsync(
        string path,
        IDataReader reader,
        CsvWriterOptions? options = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(reader);

        var headers = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();

        IEnumerable<IEnumerable<object?>> Rows()
        {
            while (reader.Read())
            {
                var row = new object?[reader.FieldCount];
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[i] = reader.GetValue(i);
                }

                yield return row;
            }
        }

        await WriteAsync(path, headers, Rows(), options, ct);
    }

    private static string SerialiseRow(IEnumerable<object?> row, CsvWriterOptions options)
    {
        return string.Join(",", row.Select(v => SerialiseField(v, options)));
    }

    private static string SerialiseField(object? value, CsvWriterOptions options)
    {
        string raw = value switch
        {
            null => string.Empty,
            DBNull => string.Empty,
            DateTime dt => dt.ToString(options.DateTimeFormat, System.Globalization.CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString(options.DateTimeFormat, System.Globalization.CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };

        if (!NeedsQuoting(raw))
        {
            return raw;
        }

        var escaped = raw.Replace("\"", "\"\"");
        return $"\"{escaped}\"";
    }

    private static bool NeedsQuoting(string s)
    {
        return s.IndexOfAny([',', '"', '\r', '\n']) >= 0;
    }
}

