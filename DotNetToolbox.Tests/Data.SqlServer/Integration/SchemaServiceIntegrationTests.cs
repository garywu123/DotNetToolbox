using DotNetToolbox.Data.SqlServer.Schema;
using DotNetToolbox.Tests.TestHelpers;

using FluentAssertions;

namespace DotNetToolbox.Tests.Data.SqlServer.Integration;

public sealed class SchemaServiceIntegrationTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;

    public SchemaServiceIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact, Trait("Category", "Integration")]
    public async Task GetColumnMapAsync_KnownTable_ReturnsAllColumns()
    {
        _fixture.SkipIfUnavailable();
        await using var conn = await _fixture.OpenConnectionAsync();
        var svc = new SchemaService();

        var result = await svc.GetColumnMapAsync(conn, "dbo.TestDimTable");

        result.Count.Should().BeGreaterThanOrEqualTo(10);
    }

    [Fact, Trait("Category", "Integration")]
    public async Task GetColumnMapAsync_UnknownTable_ReturnsEmptyDict()
    {
        _fixture.SkipIfUnavailable();
        await using var conn = await _fixture.OpenConnectionAsync();
        var svc = new SchemaService();

        var result = await svc.GetColumnMapAsync(conn, "dbo.NonExistentTable_XYZ");

        result.Should().BeEmpty();
    }

    [Fact, Trait("Category", "Integration")]
    public async Task GetColumnMapAsync_IntColumn_TypeNameIsInt()
    {
        _fixture.SkipIfUnavailable();
        await using var conn = await _fixture.OpenConnectionAsync();
        var svc = new SchemaService();

        var result = await svc.GetColumnMapAsync(conn, "dbo.TestDimTable");

        result["Id"].TypeName.Should().Be("int");
    }

    [Fact, Trait("Category", "Integration")]
    public async Task GetColumnMapAsync_NvarcharColumn_MaxLengthIsBytes()
    {
        _fixture.SkipIfUnavailable();
        await using var conn = await _fixture.OpenConnectionAsync();
        var svc = new SchemaService();

        var result = await svc.GetColumnMapAsync(conn, "dbo.TestDimTable");

        result["Name"].MaxLength.Should().Be(400);
    }

    [Fact, Trait("Category", "Integration")]
    public async Task GetColumnMapAsync_CalledTwice_ReturnsSameReference()
    {
        _fixture.SkipIfUnavailable();
        await using var conn = await _fixture.OpenConnectionAsync();
        var svc = new SchemaService();

        var first = await svc.GetColumnMapAsync(conn, "dbo.TestDimTable");
        var second = await svc.GetColumnMapAsync(conn, "dbo.TestDimTable");

        ReferenceEquals(first, second).Should().BeTrue();
    }

    [Fact, Trait("Category", "Integration")]
    public async Task ClearCache_ThenCall_ReturnsFreshResult()
    {
        _fixture.SkipIfUnavailable();
        await using var conn = await _fixture.OpenConnectionAsync();
        var svc = new SchemaService();

        var first = await svc.GetColumnMapAsync(conn, "dbo.TestDimTable");
        svc.ClearCache();
        var second = await svc.GetColumnMapAsync(conn, "dbo.TestDimTable");

        ReferenceEquals(first, second).Should().BeFalse();
    }
}
