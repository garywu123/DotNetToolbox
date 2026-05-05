using DotNetToolbox.Data.Csv;

using FluentAssertions;

namespace DotNetToolbox.Tests.Data.Csv;

public class CsvDataReaderTests
{
    [Fact]
    public async Task CsvDataReader_FieldCount_MatchesHeaders()
    {
        var path = await WriteTempCsvAsync("A,B,C\r\n1,2,3\r\n");
        try
        {
            await using var reader = new CsvDataReader(path, ["A", "B", "C"]);
            reader.FieldCount.Should().Be(3);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Read_DataRow_ReturnsTrue()
    {
        var path = await WriteTempCsvAsync("A\r\nhello\r\n");
        try
        {
            await using var reader = new CsvDataReader(path, ["A"]);
            reader.Read().Should().BeTrue();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Read_AtEof_ReturnsFalse()
    {
        var path = await WriteTempCsvAsync("A\r\nhello\r\n");
        try
        {
            await using var reader = new CsvDataReader(path, ["A"]);
            reader.Read().Should().BeTrue();
            reader.Read().Should().BeFalse();
            reader.Read().Should().BeFalse();
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task GetValue_EmptyField_ReturnsDbNull()
    {
        var path = await WriteTempCsvAsync("A,B\r\n,hi\r\n");
        try
        {
            await using var reader = new CsvDataReader(path, ["A", "B"]);
            reader.Read().Should().BeTrue();
            reader.GetValue(0).Should().Be(DBNull.Value);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task GetValue_NonEmpty_ReturnsRawString()
    {
        var path = await WriteTempCsvAsync("A\r\nhello\r\n");
        try
        {
            await using var reader = new CsvDataReader(path, ["A"]);
            reader.Read().Should().BeTrue();
            reader.GetValue(0).Should().Be("hello");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task GetOrdinal_CaseInsensitive()
    {
        var path = await WriteTempCsvAsync("Name\r\nhello\r\n");
        try
        {
            await using var reader = new CsvDataReader(path, ["Name"]);
            reader.GetOrdinal("NAME").Should().Be(reader.GetOrdinal("name"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task GetName_ReturnsHeader()
    {
        var path = await WriteTempCsvAsync("A,B\r\n1,2\r\n");
        try
        {
            await using var reader = new CsvDataReader(path, ["A", "B"]);
            reader.GetName(0).Should().Be("A");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task DisposeAsync_CalledTwice_NoThrow()
    {
        var path = await WriteTempCsvAsync("A\r\nhello\r\n");
        var reader = new CsvDataReader(path, ["A"]);

        await reader.DisposeAsync();
        var action = async () => await reader.DisposeAsync();
        await action.Should().NotThrowAsync();

        File.Delete(path);
    }

    private static async Task<string> WriteTempCsvAsync(string content)
    {
        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path, content);
        return path;
    }
}

