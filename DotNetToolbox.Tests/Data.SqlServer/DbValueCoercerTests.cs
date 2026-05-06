using DotNetToolbox.Data.SqlServer.Coerce;
using DotNetToolbox.Data.SqlServer.Schema;

using FluentAssertions;

namespace DotNetToolbox.Tests.Data.SqlServer;

public class DbValueCoercerTests
{
    private readonly IReadOnlyDictionary<string, ColumnMeta> _schema = new Dictionary<string, ColumnMeta>(StringComparer.OrdinalIgnoreCase)
    {
        ["IntCol"] = new ColumnMeta("int", true, 4, 0, 0),
        ["BigCol"] = new ColumnMeta("bigint", true, 8, 0, 0),
        ["BitCol"] = new ColumnMeta("bit", true, 1, 0, 0),
        ["DecCol"] = new ColumnMeta("decimal", true, 0, 18, 4),
        ["Dt2Col"] = new ColumnMeta("datetime2", true, 0, 0, 0),
        ["DateCol"] = new ColumnMeta("date", true, 0, 0, 0),
        ["GuidCol"] = new ColumnMeta("uniqueidentifier", true, 0, 0, 0),
        ["VarCol"] = new ColumnMeta("varchar", true, 100, 0, 0),
        ["NVarCol"] = new ColumnMeta("nvarchar", true, 200, 0, 0),
    };

    [Fact]
    public void Coerce_NullRaw_ReturnsDbNull()
    {
        DbValueCoercer.Coerce("IntCol", null, _schema).Should().Be(DBNull.Value);
    }

    [Fact]
    public void Coerce_EmptyString_ReturnsDbNull()
    {
        DbValueCoercer.Coerce("IntCol", "", _schema).Should().Be(DBNull.Value);
    }

    [Theory]
    [InlineData("NULL")]
    [InlineData("null")]
    public void Coerce_LiteralNULL_ReturnsDbNull(string raw)
    {
        DbValueCoercer.Coerce("IntCol", raw, _schema).Should().Be(DBNull.Value);
    }

    [Fact]
    public void Coerce_IntColumn_ValidRaw_ReturnsInt()
    {
        DbValueCoercer.Coerce("IntCol", "42", _schema).Should().Be(42);
    }

    [Fact]
    public void Coerce_BigIntColumn_ReturnsLong()
    {
        DbValueCoercer.Coerce("BigCol", "9876543210", _schema).Should().Be(9876543210L);
    }

    [Fact]
    public void Coerce_BitColumn_One_ReturnsTrue()
    {
        DbValueCoercer.Coerce("BitCol", "1", _schema).Should().Be(true);
    }

    [Fact]
    public void Coerce_BitColumn_Zero_ReturnsFalse()
    {
        DbValueCoercer.Coerce("BitCol", "0", _schema).Should().Be(false);
    }

    [Fact]
    public void Coerce_DecimalColumn_ReturnsDecimal()
    {
        DbValueCoercer.Coerce("DecCol", "1234.5678", _schema).Should().Be(1234.5678m);
    }

    [Fact]
    public void Coerce_DateTime2Column_ReturnsDateTime()
    {
        var result = DbValueCoercer.Coerce("Dt2Col", "2024-01-15 08:30:00.0000000", _schema);
        result.Should().BeOfType<DateTime>();
    }

    [Fact]
    public void Coerce_DateColumn_ReturnsDateOnly()
    {
        var result = DbValueCoercer.Coerce("DateCol", "2024-01-15", _schema).Should().BeOfType<DateTime>().Subject;
        result.TimeOfDay.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Coerce_GuidColumn_ReturnsGuid()
    {
        var guid = Guid.NewGuid().ToString();
        DbValueCoercer.Coerce("GuidCol", guid, _schema).Should().BeOfType<Guid>();
    }

    [Fact]
    public void Coerce_VarcharColumn_LeadingZero_PassThrough()
    {
        DbValueCoercer.Coerce("VarCol", "007", _schema).Should().Be("007");
    }

    [Fact]
    public void Coerce_NvarcharColumn_NumericString_PassThrough()
    {
        DbValueCoercer.Coerce("NVarCol", "42", _schema).Should().Be("42");
    }

    [Fact]
    public void Coerce_UnknownColumn_ReturnsRawString()
    {
        DbValueCoercer.Coerce("Missing", "anything", _schema).Should().Be("anything");
    }

    [Fact]
    public void Coerce_IntColumn_InvalidRaw_ReturnsRawString()
    {
        DbValueCoercer.Coerce("IntCol", "not-a-number", _schema).Should().Be("not-a-number");
    }
}

