using System.Data;
using System.Globalization;

using DotNetToolbox.Data.Csv;

using FluentAssertions;

namespace DotNetToolbox.Tests.Data.Csv;

public class CsvWriterTests
{
    [Fact]
    public async Task WriteAsync_DateTimeField_FormatsWithFullPrecision()
    {
        var path = Path.GetTempFileName();
        try
        {
            var headers = new[] { "HistoryTime" };
            var rows = new[] { new object?[] { new DateTime(2024, 1, 15, 8, 30, 0, DateTimeKind.Unspecified) } };

            await CsvWriter.WriteAsync(path, headers, rows);

            var text = await File.ReadAllTextAsync(path);
            text.Should().Contain("2024-01-15 08:30:00.0000000", Exactly.Once());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task WriteAsync_DbNullValue_WritesEmptyField()
    {
        var path = Path.GetTempFileName();
        try
        {
            await CsvWriter.WriteAsync(path, ["A"], [new object?[] { DBNull.Value }]);

            var lines = await File.ReadAllLinesAsync(path);
            lines.Should().Equal(["A", ""]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task WriteAsync_NullValue_WritesEmptyField()
    {
        var path = Path.GetTempFileName();
        try
        {
            await CsvWriter.WriteAsync(path, ["A"], [new object?[] { null }]);

            var lines = await File.ReadAllLinesAsync(path);
            lines.Should().Equal(["A", ""]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task WriteAsync_FieldWithComma_IsQuoted()
    {
        var path = Path.GetTempFileName();
        try
        {
            await CsvWriter.WriteAsync(path, ["A"], [new object?[] { "hello, world" }]);

            var lines = await File.ReadAllLinesAsync(path);
            lines[1].Should().Be("\"hello, world\"");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task WriteAsync_FieldWithQuote_Escaped()
    {
        var path = Path.GetTempFileName();
        try
        {
            await CsvWriter.WriteAsync(path, ["A"], [new object?[] { "a\"b" }]);

            var lines = await File.ReadAllLinesAsync(path);
            lines[1].Should().Be("\"a\"\"b\"");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task WriteAsync_EmptyRows_WritesOnlyHeader()
    {
        var path = Path.GetTempFileName();
        try
        {
            await CsvWriter.WriteAsync(path, ["A", "B"], []);

            var lines = await File.ReadAllLinesAsync(path);
            lines.Should().Equal(["A,B"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task WriteAsync_CustomDateTimeFormat_Applied()
    {
        var path = Path.GetTempFileName();
        try
        {
            var options = new CsvWriterOptions { DateTimeFormat = "yyyy-MM-dd" };
            var dt = new DateTime(2024, 1, 15, 8, 30, 0, DateTimeKind.Unspecified);

            await CsvWriter.WriteAsync(path, ["D"], [new object?[] { dt }], options);

            var lines = await File.ReadAllLinesAsync(path);
            lines[1].Should().Be(dt.ToString(options.DateTimeFormat, CultureInfo.InvariantCulture));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task WriteFromReaderAsync_ReadsAllRows()
    {
        var path = Path.GetTempFileName();
        try
        {
            var table = new DataTable();
            table.Columns.Add("A", typeof(string));
            table.Columns.Add("B", typeof(int));
            table.Rows.Add("x", 1);
            table.Rows.Add("y", 2);

            using var reader = table.CreateDataReader();
            await CsvWriter.WriteFromReaderAsync(path, reader);

            var lines = await File.ReadAllLinesAsync(path);
            lines.Should().Equal(["A,B", "x,1", "y,2"]);
        }
        finally
        {
            File.Delete(path);
        }
    }
}

