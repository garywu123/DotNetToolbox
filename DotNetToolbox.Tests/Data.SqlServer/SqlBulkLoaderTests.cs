using System.Data;

using DotNetToolbox.Data.SqlServer.Bulk;
using DotNetToolbox.Data.SqlServer.Schema;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace DotNetToolbox.Tests.Data.SqlServer;

public class SqlBulkLoaderTests
{
    [Fact]
    public async Task LoadAsync_InvalidBatchSize_ThrowsImmediately()
    {
        using var conn = new SqlConnection();
        using var reader = new DataTable().CreateDataReader();
        var schema = new Dictionary<string, ColumnMeta>(StringComparer.OrdinalIgnoreCase);

        var action = async () => await SqlBulkLoader.LoadAsync(conn, null, "dbo.Table", reader, schema, batchSize: 0);
        await action.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task LoadAsync_InvalidTableName_ThrowsArgumentException()
    {
        using var conn = new SqlConnection();
        using var reader = new DataTable().CreateDataReader();
        var schema = new Dictionary<string, ColumnMeta>(StringComparer.OrdinalIgnoreCase);

        var action = async () => await SqlBulkLoader.LoadAsync(conn, null, "a;b", reader, schema);
        await action.Should().ThrowAsync<ArgumentException>();
    }
}

