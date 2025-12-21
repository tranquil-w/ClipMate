using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace ClipMate.Service.Infrastructure;

public interface ISqliteConnectionFactory
{
    string ConnectionString { get; }

    bool IsInMemory { get; }

    string DataSource { get; }

    string? DatabaseFilePath { get; }

    Task<SqliteConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}

public sealed class SqliteConnectionFactory : ISqliteConnectionFactory
{
    private readonly SqliteConnectionStringBuilder _connectionStringBuilder;

    public SqliteConnectionFactory(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        ConnectionString = connectionString;
        _connectionStringBuilder = new SqliteConnectionStringBuilder(connectionString);

        DataSource = _connectionStringBuilder.DataSource ?? string.Empty;
        IsInMemory =
            _connectionStringBuilder.Mode == SqliteOpenMode.Memory ||
            string.Equals(DataSource, ":memory:", StringComparison.OrdinalIgnoreCase);

        DatabaseFilePath = GetDatabaseFilePath(DataSource, IsInMemory);
    }

    public string ConnectionString { get; }

    public bool IsInMemory { get; }

    public string DataSource { get; }

    public string? DatabaseFilePath { get; }

    public async Task<SqliteConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqliteConnection(ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            await ConfigurePragmasAsync(connection, cancellationToken);
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    private async Task ConfigurePragmasAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            if (!IsInMemory)
            {
                _ = await ExecuteScalarAsync(connection, "PRAGMA journal_mode=WAL;", cancellationToken);
            }

            await ExecuteNonQueryAsync(connection, "PRAGMA synchronous=NORMAL;", cancellationToken);
            await ExecuteNonQueryAsync(connection, "PRAGMA busy_timeout=5000;", cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // PRAGMA 优化属于性能增强项，失败时继续使用 SQLite 默认配置。
        }
    }

    private static string? GetDatabaseFilePath(string dataSource, bool isInMemory)
    {
        if (isInMemory)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(dataSource))
        {
            return null;
        }

        if (dataSource.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return Path.GetFullPath(dataSource);
    }

    private static async Task ExecuteNonQueryAsync(DbConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<string?> ExecuteScalarAsync(DbConnection connection, string sql, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result?.ToString();
    }
}

