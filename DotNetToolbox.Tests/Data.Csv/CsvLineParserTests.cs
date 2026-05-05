using DotNetToolbox.Data.Csv;

using FluentAssertions;

namespace DotNetToolbox.Tests.Data.Csv;

public class CsvLineParserTests
{
    [Fact]
    public void Parse_SimpleThreeFields_ReturnsSplit()
    {
        CsvLineParser.Parse("a,b,c").Should().Equal(["a", "b", "c"]);
    }

    [Fact]
    public void Parse_QuotedComma_TreatedAsField()
    {
        CsvLineParser.Parse("\"a,b\",c").Should().Equal(["a,b", "c"]);
    }

    [Fact]
    public void Parse_EscapedQuote_Unescaped()
    {
        CsvLineParser.Parse("\"say \"\"hi\"\"\"").Should().Equal(["say \"hi\""]);
    }

    [Fact]
    public void Parse_EmptyFirstAndLast_ReturnsEmptyStrings()
    {
        CsvLineParser.Parse(",b,").Should().Equal(["", "b", ""]);
    }

    [Fact]
    public void Parse_EmptyLine_ReturnsSingleEmptyString()
    {
        CsvLineParser.Parse(string.Empty).Should().Equal([""]);
    }

    [Fact]
    public void Parse_QuotedEmptyField_ReturnsEmptyString()
    {
        CsvLineParser.Parse("\"\"").Should().Equal([""]);
    }

    [Fact]
    public void Parse_NullInput_ThrowsArgumentNull()
    {
        var action = () => CsvLineParser.Parse(null!);
        action.Should().Throw<ArgumentNullException>();
    }
}

