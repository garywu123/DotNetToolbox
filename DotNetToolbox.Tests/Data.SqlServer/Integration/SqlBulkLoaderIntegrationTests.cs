using System.Globalization;

using DotNetToolbox.Data.Csv;
using DotNetToolbox.Data.SqlServer.Bulk;
using DotNetToolbox.Data.SqlServer.Schema;
using DotNetToolbox.Tests.TestHelpers;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace DotNetToolbox.Tests.Data.SqlServer.Integration;

public sealed class SqlBulkLoaderIntegrationTests : IClassFixture<SqlServerFixture>
{
    private readonly SqlServerFixture _fixture;

    public SqlBulkLoaderIntegrationTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact, Trait("Category", "Integration")]
    public async Task LoadAsync_BasicInsert_RowsPresent()
    {
        _fixture.SkipIfUnavailable();
        var prefix = $"TEST_{Guid.NewGuid():N}";
        var path = CreateTempCsv(
            headers:
            [
                "CustomerId",
                "Name",
                "IsActive",
                "Amount",
                "CreatedOn",
                "UpdatedOffset",
                "SomeInt",
                "SomeBigInt",
                "SomeDate",
                "SomeFloat",
                "RowGuid",
                "Notes",
            ],
            rows: Enumerable.Range(0, 100).Select(i => new object?[]
            {
                $"{prefix}_{i:D3}",
                $"Name {i}",
                i % 2 == 0 ? "1" : "0",
                (1234.5678m + i).ToString(CultureInfo.InvariantCulture),
                "2024-01-15 08:30:00.0000000",
                "2024-01-15 08:30:00.0000000+00:00",
                i.ToString(CultureInfo.InvariantCulture),
                (9876543210L + i).ToString(CultureInfo.InvariantCulture),
                "2024-01-15",
                (12.34 + i).ToString(CultureInfo.InvariantCulture),
                Guid.NewGuid().ToString(),
                "note",
            }));

        await using var conn = await _fixture.OpenConnectionAsync();
        try
        {
            var schemaSvc = new SchemaService();
            var schema = await schemaSvc.GetColumnMapAsync(conn, "dbo.TestDimTable");

            await using var reader = new CsvDataReader(path, CsvHeadersFromFile(path));

            await SqlBulkLoader.LoadAsync(conn, transaction: null, "dbo.TestDimTable", reader, schema);

            var count = await CountByPrefixAsync(conn, prefix);
            count.Should().Be(100);
        }
        finally
        {
            await CleanupAsync(conn, prefix);
            File.Delete(path);
        }
    }

    [Fact, Trait("Category", "Integration")]
    public async Task LoadAsync_KeepIdentity_PreservesIdValues()
    {
        _fixture.SkipIfUnavailable();
        var prefix = $"TEST_{Guid.NewGuid():N}";
        var ids = new[] { 1001, 1002, 1003, 1004, 1005 };

        var path = CreateTempCsv(
            headers: ["Id", "CustomerId", "Name"],
            rows: ids.Select(id => new object?[] { id.ToString(CultureInfo.InvariantCulture), $"{prefix}_{id}", "X" }));

        await using var conn = await _fixture.OpenConnectionAsync();
        try
        {
            var schemaSvc = new SchemaService();
            var schema = await schemaSvc.GetColumnMapAsync(conn, "dbo.TestDimTable");

            await using var txn = await conn.BeginTransactionAsync();
            try
            {
                await using (var cmd = new SqlCommand("SET IDENTITY_INSERT [dbo].[TestDimTable] ON;", conn, (SqlTransaction)txn))
                {
                    _ = await cmd.ExecuteNonQueryAsync();
                }

                await using var reader = new CsvDataReader(path, CsvHeadersFromFile(path));
                await SqlBulkLoader.LoadAsync(conn, (SqlTransaction)txn, "dbo.TestDimTable", reader, schema, keepIdentity: true, batchSize: 100);

                await using (var cmd = new SqlCommand("SET IDENTITY_INSERT [dbo].[TestDimTable] OFF;", conn, (SqlTransaction)txn))
                {
                    _ = await cmd.ExecuteNonQueryAsync();
                }

                await txn.CommitAsync();
            }
            catch
            {
                await txn.RollbackAsync();
                throw;
            }

            var inserted = new List<int>();
            await using (var cmd = new SqlCommand("SELECT Id FROM dbo.TestDimTable WHERE CustomerId LIKE @p + '%'", conn))
            {
                _ = cmd.Parameters.AddWithValue("@p", prefix);
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    inserted.Add(r.GetInt32(0));
                }
            }

            inserted.Should().BeEquivalentTo(ids);
        }
        finally
        {
            await CleanupAsync(conn, prefix);
            File.Delete(path);
        }
    }

    [Fact, Trait("Category", "Integration")]
    public async Task LoadAsync_Cancellation_RollsBack()
    {
        _fixture.SkipIfUnavailable();
        var prefix = $"TEST_{Guid.NewGuid():N}";
        var path = CreateTempCsv(
            headers: ["CustomerId", "Name"],
            rows: Enumerable.Range(0, 1000).Select(i => new object?[] { $"{prefix}_{i:D6}", "X" }));

        await using var conn = await _fixture.OpenConnectionAsync();
        try
        {
            var schemaSvc = new SchemaService();
            var schema = await schemaSvc.GetColumnMapAsync(conn, "dbo.TestDimTable");

            await using var txn = await conn.BeginTransactionAsync();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var action = async () =>
            {
                await using var reader = new CsvDataReader(path, CsvHeadersFromFile(path));
                await SqlBulkLoader.LoadAsync(conn, (SqlTransaction)txn, "dbo.TestDimTable", reader, schema, batchSize: 1, ct: cts.Token);
            };

            await action.Should().ThrowAsync<OperationCanceledException>();
            await txn.RollbackAsync();

            var count = await CountByPrefixAsync(conn, prefix);
            count.Should().Be(0);
        }
        finally
        {
            await CleanupAsync(conn, prefix);
            File.Delete(path);
        }
    }

    [Fact, Trait("Category", "Integration")]
    public async Task LoadAsync_InvalidBatchSize_ThrowsImmediately()
    {
        _fixture.SkipIfUnavailable();
        var path = CreateTempCsv(["CustomerId"], []);
        await using var conn = await _fixture.OpenConnectionAsync();
        await using var reader = new CsvDataReader(path, ["CustomerId"]);
        var schema = new Dictionary<string, ColumnMeta>(StringComparer.OrdinalIgnoreCase);

        var action = async () => await SqlBulkLoader.LoadAsync(conn, null, "dbo.TestDimTable", reader, schema, batchSize: 0);
        await action.Should().ThrowAsync<ArgumentOutOfRangeException>();

        File.Delete(path);
    }

    private static async Task CleanupAsync(SqlConnection conn, string prefix)
    {
        await using var cmd = new SqlCommand("DELETE FROM dbo.TestDimTable WHERE CustomerId LIKE @p + '%'", conn);
        _ = cmd.Parameters.AddWithValue("@p", prefix);
        _ = await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<int> CountByPrefixAsync(SqlConnection conn, string prefix)
    {
        await using var cmd = new SqlCommand("SELECT COUNT(*) FROM dbo.TestDimTable WHERE CustomerId LIKE @p + '%'", conn);
        _ = cmd.Parameters.AddWithValue("@p", prefix);
        var obj = await cmd.ExecuteScalarAsync();
        obj.Should().NotBeNull();
        return Convert.ToInt32(obj, CultureInfo.InvariantCulture);
    }

    private static string[] CsvHeadersFromFile(string path)
    {
        var headerLine = File.ReadLines(path).First();
        return CsvLineParser.Parse(headerLine);
    }

    private static string CreateTempCsv(IReadOnlyList<string> headers, IEnumerable<object?[]> rows)
    {
        var path = Path.GetTempFileName();
        using var writer = new StreamWriter(
            path,
            append: false,
            encoding: new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true))
        {
            NewLine = "\r\n",
        };

        writer.WriteLine(string.Join(",", headers));

        foreach (var row in rows)
        {
            var fields = row.Select(v => v?.ToString() ?? string.Empty);
            writer.WriteLine(string.Join(",", fields));
        }

        writer.Flush();
        return path;
    }
}
