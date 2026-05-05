using DotNetToolbox.Data.SqlServer.Validation;

using FluentAssertions;

namespace DotNetToolbox.Tests.Data.SqlServer;

public class SqlIdentifierValidatorTests
{
    [Fact]
    public void IsValid_SimpleName_ReturnsTrue()
    {
        SqlIdentifierValidator.IsValid("TableName").Should().BeTrue();
    }

    [Fact]
    public void IsValid_BracketQuoted_ReturnsTrue()
    {
        SqlIdentifierValidator.IsValid("[Table Name]").Should().BeTrue();
    }

    [Fact]
    public void IsValid_SchemaQualified_ReturnsTrue()
    {
        SqlIdentifierValidator.IsValid("dbo.TableName").Should().BeTrue();
    }

    [Fact]
    public void IsValid_StartsWithDigit_ReturnsFalse()
    {
        SqlIdentifierValidator.IsValid("1Table").Should().BeFalse();
    }

    [Fact]
    public void IsValid_ContainsSemicolon_ReturnsFalse()
    {
        SqlIdentifierValidator.IsValid("a;b").Should().BeFalse();
    }

    [Fact]
    public void IsValid_Empty_ReturnsFalse()
    {
        SqlIdentifierValidator.IsValid("").Should().BeFalse();
    }

    [Fact]
    public void IsValid_NullInput_ReturnsFalse()
    {
        SqlIdentifierValidator.IsValid(null).Should().BeFalse();
    }

    [Fact]
    public void Quote_PlainName_WrapsBrackets()
    {
        SqlIdentifierValidator.Quote("TableName").Should().Be("[TableName]");
    }

    [Fact]
    public void Quote_AlreadyQuoted_ReturnsSame()
    {
        SqlIdentifierValidator.Quote("[Table]").Should().Be("[Table]");
    }

    [Fact]
    public void QuoteQualified_TwoParts_QuotesBoth()
    {
        SqlIdentifierValidator.QuoteQualified("dbo.HistoricalVehicles")
            .Should()
            .Be("[dbo].[HistoricalVehicles]");
    }

    [Fact]
    public void Quote_InvalidIdentifier_ThrowsArgumentException()
    {
        var action = () => SqlIdentifierValidator.Quote("a;b");
        action.Should().Throw<ArgumentException>();
    }
}

