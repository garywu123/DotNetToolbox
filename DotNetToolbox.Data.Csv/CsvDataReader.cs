using System.Collections;
using System.Data;

namespace DotNetToolbox.Data.Csv;

/// <summary>
/// Streams a CSV file row-by-row as an <see cref="IDataReader"/>.
/// </summary>
#pragma warning disable CS1591
public sealed class CsvDataReader : IDataReader, IAsyncDisposable
{
    private readonly StreamReader _reader;
    private readonly IReadOnlyList<string> _expectedHeaders;
    private readonly Dictionary<string, int> _ordinals;

    private bool _isClosed;
    private string[]? _current;

    /// <summary>
    /// Opens <paramref name="path"/> for streaming. The file is held open until <see cref="Dispose"/>
    /// or <see cref="DisposeAsync"/> is called.
    /// </summary>
    /// <param name="path">Path to the CSV file.</param>
    /// <param name="expectedHeaders">Column names in the order they appear in the CSV header row.</param>
    public CsvDataReader(string path, IReadOnlyList<string> expectedHeaders)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(expectedHeaders);

        _expectedHeaders = expectedHeaders;
        _ordinals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < expectedHeaders.Count; i++)
        {
            _ordinals[expectedHeaders[i]] = i;
        }

        _reader = new StreamReader(File.OpenRead(path));

        // Consume header row.
        _ = _reader.ReadLine();
    }

    /// <inheritdoc />
    public int FieldCount => _expectedHeaders.Count;

    /// <inheritdoc />
    public bool Read()
    {
        ThrowIfClosed();

        var line = _reader.ReadLine();
        if (line is null)
        {
            _current = null;
            return false;
        }

        _current = CsvLineParser.Parse(line);
        return true;
    }

    /// <inheritdoc />
    public object GetValue(int i)
    {
        ThrowIfClosed();
        if (_current is null)
        {
            throw new InvalidOperationException("Read() must be called before accessing values.");
        }

        var value = i < _current.Length ? _current[i] : string.Empty;
        return string.IsNullOrEmpty(value) ? DBNull.Value : value;
    }

    /// <inheritdoc />
    public bool IsDBNull(int i)
    {
        return GetValue(i) is DBNull;
    }

    /// <inheritdoc />
    public string GetName(int i)
    {
        return _expectedHeaders[i];
    }

    /// <inheritdoc />
    public int GetOrdinal(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return _ordinals.TryGetValue(name, out var ordinal)
            ? ordinal
            : throw new KeyNotFoundException($"Column '{name}' was not found.");
    }

    /// <inheritdoc />
    public void Close()
    {
        if (_isClosed)
        {
            return;
        }

        _isClosed = true;
        _reader.Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Close();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isClosed)
        {
            return;
        }

        _isClosed = true;
        // StreamReader does not implement IAsyncDisposable on all TFMs; keep async signature but dispose synchronously.
        _reader.Dispose();
        await ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public bool IsClosed => _isClosed;

    /// <inheritdoc />
    public int RecordsAffected => -1;

    /// <inheritdoc />
    public int Depth => 0;

    public DataTable GetSchemaTable() => throw new NotSupportedException();
    public bool NextResult() => throw new NotSupportedException();

    public bool GetBoolean(int i) => throw new NotSupportedException();
    public byte GetByte(int i) => throw new NotSupportedException();
    public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
    public char GetChar(int i) => throw new NotSupportedException();
    public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => throw new NotSupportedException();
    public IDataReader GetData(int i) => throw new NotSupportedException();
    public string GetDataTypeName(int i) => throw new NotSupportedException();
    public DateTime GetDateTime(int i) => throw new NotSupportedException();
    public decimal GetDecimal(int i) => throw new NotSupportedException();
    public double GetDouble(int i) => throw new NotSupportedException();
    public Type GetFieldType(int i) => throw new NotSupportedException();
    public float GetFloat(int i) => throw new NotSupportedException();
    public Guid GetGuid(int i) => throw new NotSupportedException();
    public short GetInt16(int i) => throw new NotSupportedException();
    public int GetInt32(int i) => throw new NotSupportedException();
    public long GetInt64(int i) => throw new NotSupportedException();
    public string GetString(int i) => throw new NotSupportedException();
    public int GetValues(object[] values) => throw new NotSupportedException();

    public object this[int i] => GetValue(i);
    public object this[string name] => GetValue(GetOrdinal(name));

    public IEnumerator GetEnumerator() => throw new NotSupportedException();

    private void ThrowIfClosed()
    {
        if (_isClosed)
        {
            throw new InvalidOperationException("The reader is closed.");
        }
    }
}
#pragma warning restore CS1591
