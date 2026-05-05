using System.Text;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace DotNetToolbox.Tests.TestHelpers;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly string? _connectionString;

    public SqlServerFixture()
    {
        var raw = Environment.GetEnvironmentVariable("TOOLBOX_TEST_CONN");
        if (string.IsNullOrWhiteSpace(raw))
        {
            _connectionString = null;
            return;
        }

        var builder = new SqlConnectionStringBuilder(raw);
        if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
        {
            builder.InitialCatalog = "ToolboxTest";
        }

        _connectionString = builder.ConnectionString;
    }

    public bool IsAvailable => !string.IsNullOrWhiteSpace(_connectionString);

    public string ConnectionString => _connectionString ?? string.Empty;

    public void SkipIfUnavailable()
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException("Integration tests require TOOLBOX_TEST_CONN to be set.");
        }
    }

    public async Task InitializeAsync()
    {
        var cs = _connectionString;
        if (string.IsNullOrWhiteSpace(cs))
        {
            return;
        }

        var builder = new SqlConnectionStringBuilder(cs);
        await EnsureDatabaseExistsAsync(builder, CancellationToken.None);

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync();

        var scriptPath = Path.Combine(AppContext.BaseDirectory, "TestHelpers", "TestSchema.sql");
        File.Exists(scriptPath).Should().BeTrue($"schema file must be copied to output: {scriptPath}");

        var script = await File.ReadAllTextAsync(scriptPath, Encoding.UTF8);
        foreach (var batch in SplitBatches(script))
        {
            using var cmd = new SqlCommand(batch, connection);
            _ = await cmd.ExecuteNonQueryAsync();
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task<SqlConnection> OpenConnectionAsync(CancellationToken ct = default)
    {
        SkipIfUnavailable();
        var cs = _connectionString!;

        var connection = new SqlConnection(cs);
        await connection.OpenAsync(ct);
        return connection;
    }

    private static IEnumerable<string> SplitBatches(string script)
    {
        var sb = new StringBuilder();
        foreach (var line in script.Split('\n'))
        {
            if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
            {
                var batch = sb.ToString().Trim();
                sb.Clear();
                if (batch.Length > 0)
                {
                    yield return batch;
                }

                continue;
            }

            sb.AppendLine(line);
        }

        var last = sb.ToString().Trim();
        if (last.Length > 0)
        {
            yield return last;
        }
    }

    private static async Task EnsureDatabaseExistsAsync(SqlConnectionStringBuilder builder, CancellationToken ct)
    {
        var database = builder.InitialCatalog;
        var masterBuilder = new SqlConnectionStringBuilder(builder.ConnectionString)
        {
            InitialCatalog = "master",
        };

        await using var master = new SqlConnection(masterBuilder.ConnectionString);
        await master.OpenAsync(ct);

        using (var existsCmd = new SqlCommand("SELECT COUNT(*) FROM sys.databases WHERE name = @name", master))
        {
            _ = existsCmd.Parameters.AddWithValue("@name", database);
            var exists = (int)await existsCmd.ExecuteScalarAsync(ct);
            if (exists > 0)
            {
                return;
            }
        }

        using var createCmd = new SqlCommand($"CREATE DATABASE [{database}];", master);
        _ = await createCmd.ExecuteNonQueryAsync(ct);
    }
}
