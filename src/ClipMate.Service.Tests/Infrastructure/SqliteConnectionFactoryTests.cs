using ClipMate.Service.Infrastructure;
using Microsoft.Data.Sqlite;
using System.Data;

namespace ClipMate.Service.Tests.Infrastructure;

public sealed class SqliteConnectionFactoryTests
{
    [Fact]
    public async Task CreateOpenConnectionAsync_ShouldReturnNewConnectionEachTime()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = "file:memdb-factory-new",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        };

        await using var keepAliveConnection = new SqliteConnection(builder.ToString());
        await keepAliveConnection.OpenAsync();

        var factory = new SqliteConnectionFactory(builder.ToString());

        await using var connection1 = await factory.CreateOpenConnectionAsync();
        await using var connection2 = await factory.CreateOpenConnectionAsync();

        Assert.NotSame(connection1, connection2);
        Assert.Equal(ConnectionState.Open, connection1.State);
        Assert.Equal(ConnectionState.Open, connection2.State);
    }

    [Fact]
    public async Task ConcurrentConnections_ShouldNotInterfere()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = "file:memdb-factory-concurrent",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        };

        await using var keepAliveConnection = new SqliteConnection(builder.ToString());
        await keepAliveConnection.OpenAsync();

        var factory = new SqliteConnectionFactory(builder.ToString());

        await using (var setupConnection = await factory.CreateOpenConnectionAsync())
        {
            await using var setupCommand = setupConnection.CreateCommand();
            setupCommand.CommandText = "CREATE TABLE IF NOT EXISTS test (id INTEGER PRIMARY KEY, value TEXT)";
            await setupCommand.ExecuteNonQueryAsync();
        }

        var tasks = Enumerable.Range(0, 5).Select(async i =>
        {
            await using var connection = await factory.CreateOpenConnectionAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO test (value) VALUES ($value)";
            command.Parameters.AddWithValue("$value", $"task-{i}");
            await command.ExecuteNonQueryAsync();
        });

        await Task.WhenAll(tasks);

        await using var verifyConnection = await factory.CreateOpenConnectionAsync();
        await using var verifyCommand = verifyConnection.CreateCommand();
        verifyCommand.CommandText = "SELECT COUNT(*) FROM test";
        var result = await verifyCommand.ExecuteScalarAsync();
        Assert.Equal(5, Convert.ToInt32(result));
    }

    [Fact]
    public async Task Connection_ShouldBeDisposable()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = "file:memdb-factory-dispose",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        };

        await using var keepAliveConnection = new SqliteConnection(builder.ToString());
        await keepAliveConnection.OpenAsync();

        var factory = new SqliteConnectionFactory(builder.ToString());

        var connection = await factory.CreateOpenConnectionAsync();
        Assert.Equal(ConnectionState.Open, connection.State);

        connection.Dispose();

        var disposedOrClosed = false;
        try
        {
            disposedOrClosed = connection.State == ConnectionState.Closed;
        }
        catch (ObjectDisposedException)
        {
            disposedOrClosed = true;
        }

        Assert.True(disposedOrClosed);
    }
}

