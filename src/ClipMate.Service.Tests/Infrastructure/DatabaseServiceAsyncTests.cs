using ClipMate.Core.Models;
using ClipMate.Service.Infrastructure;
using Microsoft.Data.Sqlite;
using Moq;
using Serilog;
using System.Data;
using System.Diagnostics;
using System.Text;

namespace ClipMate.Service.Tests.Infrastructure;

public sealed class DatabaseServiceAsyncTests
{
    [Fact]
    public async Task InitializeAsync_ShouldNotCreateDatabaseInConstructor()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"clipmate-test-{Guid.NewGuid():N}.db");
        try
        {
            if (File.Exists(dbPath))
            {
                _ = TryDeleteFileWithRetry(dbPath);
            }

            var logger = new Mock<ILogger>();
            var factory = new SqliteConnectionFactory(new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Pooling = false
            }.ToString());

            _ = new DatabaseService(factory, logger.Object);

            Assert.False(File.Exists(dbPath));

            var service = new DatabaseService(factory, logger.Object);
            await service.InitializeAsync();

            Assert.True(File.Exists(dbPath));

            await using var connection = new SqliteConnection(factory.ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='ClipboardItems'";
            var result = await command.ExecuteScalarAsync();
            Assert.Equal(1, Convert.ToInt32(result));
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                _ = TryDeleteFileWithRetry(dbPath);
            }
        }
    }

    [Fact]
    public async Task InitializeAsync_CalledMultipleTimes_ShouldOnlyInitializeOnce()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"clipmate-init-once-{Guid.NewGuid():N}.db");
        try
        {
            if (File.Exists(dbPath))
            {
                _ = TryDeleteFileWithRetry(dbPath);
            }

            var logger = new Mock<ILogger>();
            var innerFactory = new SqliteConnectionFactory(new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Pooling = false
            }.ToString());
            var factory = new CountingSqliteConnectionFactory(innerFactory);

            var service = new DatabaseService(factory, logger.Object);

            await Task.WhenAll(
                service.InitializeAsync(),
                service.InitializeAsync(),
                service.InitializeAsync(),
                service.InitializeAsync(),
                service.InitializeAsync());

            Assert.Equal(1, factory.OpenConnectionCalls);
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                _ = TryDeleteFileWithRetry(dbPath);
            }
        }
    }

    [Fact]
    public async Task InitializeAsync_WhenCancelled_ShouldRespectCancellation_AndAllowRetry()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"clipmate-cancel-{Guid.NewGuid():N}.db");
        try
        {
            if (File.Exists(dbPath))
            {
                _ = TryDeleteFileWithRetry(dbPath);
            }

            var logger = new Mock<ILogger>();
            var factory = new SqliteConnectionFactory(new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Pooling = false
            }.ToString());
            var service = new DatabaseService(factory, logger.Object);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.InitializeAsync(cts.Token));

            await service.InitializeAsync();
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                _ = TryDeleteFileWithRetry(dbPath);
            }
        }
    }

    [Fact]
    public async Task DatabaseOperations_BeforeInitialize_ShouldWaitForInit()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = "file:memdb-before-init",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        };

        await using var keepAliveConnection = new SqliteConnection(builder.ToString());
        await keepAliveConnection.OpenAsync();

        var logger = new Mock<ILogger>();
        var factory = new SqliteConnectionFactory(builder.ToString());
        var service = new DatabaseService(factory, logger.Object);

        var stopwatch = Stopwatch.StartNew();
        var id = await service.InsertItemAsync(new ClipboardItem
        {
            ContentType = ClipboardContentTypes.Text,
            Content = Encoding.UTF8.GetBytes("before-init"),
            CreatedAt = DateTime.UtcNow,
            IsFavorite = false
        });
        stopwatch.Stop();

        Assert.True(id > 0);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), "数据库初始化与插入应在合理时间内完成");

        var items = (await service.GetAllItemsDescAsync()).ToArray();
        Assert.Single(items);
        Assert.Equal("before-init", Encoding.UTF8.GetString(items[0].Content));
    }

    private sealed class CountingSqliteConnectionFactory : ISqliteConnectionFactory
    {
        private readonly ISqliteConnectionFactory _inner;
        private int _openConnectionCalls;

        public CountingSqliteConnectionFactory(ISqliteConnectionFactory inner)
        {
            _inner = inner;
        }

        public int OpenConnectionCalls => _openConnectionCalls;

        public string ConnectionString => _inner.ConnectionString;

        public bool IsInMemory => _inner.IsInMemory;

        public string DataSource => _inner.DataSource;

        public string? DatabaseFilePath => _inner.DatabaseFilePath;

        public async Task<SqliteConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _openConnectionCalls);
            return await _inner.CreateOpenConnectionAsync(cancellationToken);
        }
    }

    private static bool TryDeleteFileWithRetry(string filePath)
    {
        const int attempts = 30;
        for (var i = 0; i < attempts; i++)
        {
            try
            {
                File.Delete(filePath);
                return true;
            }
            catch (IOException) when (i < attempts - 1)
            {
                if (i == 2)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }

                Thread.Sleep(100);
            }
            catch (UnauthorizedAccessException) when (i < attempts - 1)
            {
                Thread.Sleep(100);
            }
        }

        return false;
    }
}
