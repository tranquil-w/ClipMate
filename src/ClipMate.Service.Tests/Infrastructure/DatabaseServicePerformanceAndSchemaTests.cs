using ClipMate.Core.Models;
using ClipMate.Service.Infrastructure;
using Dapper;
using Microsoft.Data.Sqlite;
using Moq;
using Serilog;
using System.Security.Cryptography;
using System.Text;

namespace ClipMate.Service.Tests.Infrastructure;

public sealed class DatabaseServicePerformanceAndSchemaTests
{
    [Fact]
    public async Task InitializeAsync_ShouldCreateExpectedIndexes_AndBeIdempotent()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = "file:memdb-indexes",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        };

        await using var keepAliveConnection = new SqliteConnection(builder.ToString());
        await keepAliveConnection.OpenAsync();

        var logger = new Mock<ILogger>();
        var factory = new SqliteConnectionFactory(builder.ToString());
        var service = new DatabaseService(factory, logger.Object);

        await service.InitializeAsync();
        await service.InitializeAsync();

        await using var connection = await factory.CreateOpenConnectionAsync();
        var indexNames = (await connection.QueryAsync<string>("SELECT name FROM pragma_index_list('ClipboardItems')")).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains("IX_ClipboardItems_CreatedAt", indexNames);
        Assert.Contains("IX_ClipboardItems_ContentType_CreatedAt", indexNames);
        Assert.Contains("IX_ClipboardItems_IsFavorite_CreatedAt", indexNames);
        Assert.Contains("IX_ClipboardItems_ContentHash", indexNames);
    }

    [Fact]
    public async Task InitializeAsync_WithOldSchemaMissingContentHash_ShouldMigrateWithoutDataLoss()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"clipmate-old-schema-{Guid.NewGuid():N}.db");
        try
        {
            await using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Pooling = false
            }.ToString()))
            {
                await connection.OpenAsync();
                await connection.ExecuteAsync(@"
                    CREATE TABLE IF NOT EXISTS ClipboardItems (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Content BLOB NOT NULL,
                        ContentType TEXT NOT NULL,
                        CreatedAt DATETIME NOT NULL,
                        IsFavorite INTEGER NOT NULL DEFAULT 0
                    );");
                await connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_ClipboardItems_CreatedAt ON ClipboardItems (CreatedAt DESC);");
                await connection.ExecuteAsync(
                    "INSERT INTO ClipboardItems (Content, ContentType, CreatedAt, IsFavorite) VALUES (@Content, @ContentType, @CreatedAt, 0);",
                    new { Content = Encoding.UTF8.GetBytes("old"), ContentType = ClipboardContentTypes.Text, CreatedAt = DateTime.UtcNow });
            }

            var logger = new Mock<ILogger>();
            var factory = new SqliteConnectionFactory(new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Pooling = false
            }.ToString());
            var service = new DatabaseService(factory, logger.Object);

            await service.InitializeAsync();

            await using var verifyConnection = await factory.CreateOpenConnectionAsync();
            var columns = (await verifyConnection.QueryAsync<string>("SELECT name FROM pragma_table_info('ClipboardItems')")).ToHashSet(StringComparer.OrdinalIgnoreCase);
            Assert.Contains("ContentHash", columns);

            var count = await verifyConnection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM ClipboardItems;");
            Assert.Equal(1, count);

            var indexNames = (await verifyConnection.QueryAsync<string>("SELECT name FROM pragma_index_list('ClipboardItems')")).ToHashSet(StringComparer.OrdinalIgnoreCase);
            Assert.Contains("IX_ClipboardItems_ContentHash", indexNames);
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                TryDeleteFileWithRetry(dbPath);
            }
        }
    }

    [Fact]
    public async Task GetItemsPagedAsync_ShouldReturnCorrectPages_InCreatedAtDescOrder()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = "file:memdb-paged",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        };

        await using var keepAliveConnection = new SqliteConnection(builder.ToString());
        await keepAliveConnection.OpenAsync();

        var logger = new Mock<ILogger>();
        var factory = new SqliteConnectionFactory(builder.ToString());
        var service = new DatabaseService(factory, logger.Object);
        await service.InitializeAsync();

        var now = DateTime.UtcNow;
        for (var i = 0; i < 150; i++)
        {
            _ = await service.InsertItemAsync(new ClipboardItem
            {
                ContentType = ClipboardContentTypes.Text,
                Content = Encoding.UTF8.GetBytes($"paged-{i}"),
                CreatedAt = now.AddSeconds(i),
                IsFavorite = false
            });
        }

        var page1 = await service.GetItemsPagedAsync(0, 100);
        var page2 = await service.GetItemsPagedAsync(100, 50);
        var page3 = await service.GetItemsPagedAsync(200, 50);

        Assert.Equal(100, page1.Count);
        Assert.Equal(50, page2.Count);
        Assert.Empty(page3);

        Assert.True(page1[0].CreatedAt >= page1[^1].CreatedAt);
        Assert.True(page2[0].CreatedAt >= page2[^1].CreatedAt);

        Assert.Equal("paged-149", Encoding.UTF8.GetString(page1[0].Content));
        Assert.Equal("paged-50", Encoding.UTF8.GetString(page1[^1].Content));
        Assert.Equal("paged-49", Encoding.UTF8.GetString(page2[0].Content));
        Assert.Equal("paged-0", Encoding.UTF8.GetString(page2[^1].Content));
    }

    [Fact]
    public async Task InsertItemAsync_ShouldStoreContentHash_AndDedupByHash()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = "file:memdb-hash",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        };

        await using var keepAliveConnection = new SqliteConnection(builder.ToString());
        await keepAliveConnection.OpenAsync();

        var logger = new Mock<ILogger>();
        var factory = new SqliteConnectionFactory(builder.ToString());
        var service = new DatabaseService(factory, logger.Object);
        await service.InitializeAsync();

        var bytes = Encoding.UTF8.GetBytes("hash-me");
        var expectedHash = Convert.ToHexString(SHA256.HashData(bytes));

        var firstId = await service.InsertItemAsync(new ClipboardItem
        {
            ContentType = ClipboardContentTypes.Text,
            Content = bytes,
            CreatedAt = DateTime.UtcNow,
            IsFavorite = false
        });

        var secondId = await service.InsertItemAsync(new ClipboardItem
        {
            ContentType = ClipboardContentTypes.Text,
            Content = bytes,
            CreatedAt = DateTime.UtcNow,
            IsFavorite = false
        });

        Assert.True(firstId > 0);
        Assert.Equal(-1, secondId);

        var inserted = (await service.GetAllItemsDescAsync()).Single();
        Assert.Equal(expectedHash, inserted.ContentHash);
    }

    [Fact]
    public async Task InsertItemAsync_WithLegacyRowsMissingHash_ShouldFallbackToLastItemCompare()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"clipmate-legacy-hash-{Guid.NewGuid():N}.db");
        try
        {
            await using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Pooling = false
            }.ToString()))
            {
                await connection.OpenAsync();
                await connection.ExecuteAsync(@"
                    CREATE TABLE IF NOT EXISTS ClipboardItems (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Content BLOB NOT NULL,
                        ContentType TEXT NOT NULL,
                        CreatedAt DATETIME NOT NULL,
                        IsFavorite INTEGER NOT NULL DEFAULT 0
                    );");
                await connection.ExecuteAsync("CREATE INDEX IF NOT EXISTS IX_ClipboardItems_CreatedAt ON ClipboardItems (CreatedAt DESC);");
                await connection.ExecuteAsync(
                    "INSERT INTO ClipboardItems (Content, ContentType, CreatedAt, IsFavorite) VALUES (@Content, @ContentType, @CreatedAt, 0);",
                    new { Content = Encoding.UTF8.GetBytes("dup"), ContentType = ClipboardContentTypes.Text, CreatedAt = DateTime.UtcNow });
            }

            var logger = new Mock<ILogger>();
            var factory = new SqliteConnectionFactory(new SqliteConnectionStringBuilder
            {
                DataSource = dbPath,
                Pooling = false
            }.ToString());
            var service = new DatabaseService(factory, logger.Object);
            await service.InitializeAsync();

            var id = await service.InsertItemAsync(new ClipboardItem
            {
                ContentType = ClipboardContentTypes.Text,
                Content = Encoding.UTF8.GetBytes("dup"),
                CreatedAt = DateTime.UtcNow,
                IsFavorite = false
            });

            Assert.Equal(-1, id);
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                TryDeleteFileWithRetry(dbPath);
            }
        }
    }

    [Fact]
    public async Task GetAllItemsDescAsync_WhenWarnThresholdIsZero_ShouldLogWarning()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = "file:memdb-warn",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        };

        await using var keepAliveConnection = new SqliteConnection(builder.ToString());
        await keepAliveConnection.OpenAsync();

        var originalWarnMs = Environment.GetEnvironmentVariable("CLIPMATE_QUERY_WARN_MS");
        var originalDiagnostics = Environment.GetEnvironmentVariable("CLIPMATE_QUERY_DIAGNOSTICS");
        try
        {
            Environment.SetEnvironmentVariable("CLIPMATE_QUERY_WARN_MS", "0");
            Environment.SetEnvironmentVariable("CLIPMATE_QUERY_DIAGNOSTICS", "false");

            var logger = new Mock<ILogger>();
            var factory = new SqliteConnectionFactory(builder.ToString());
            var service = new DatabaseService(factory, logger.Object);
            await service.InitializeAsync();

            _ = await service.GetAllItemsDescAsync();

            logger.Verify(
                x => x.Warning(It.Is<string>(message => message.Contains("慢查询")), It.IsAny<string>(), It.IsAny<long>(), It.IsAny<int>()),
                Times.AtLeastOnce);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CLIPMATE_QUERY_WARN_MS", originalWarnMs);
            Environment.SetEnvironmentVariable("CLIPMATE_QUERY_DIAGNOSTICS", originalDiagnostics);
        }
    }

    [Fact]
    public async Task GetItemsPagedAsync_WhenDiagnosticsEnabled_ShouldLogDebug()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = "file:memdb-diag",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        };

        await using var keepAliveConnection = new SqliteConnection(builder.ToString());
        await keepAliveConnection.OpenAsync();

        var originalWarnMs = Environment.GetEnvironmentVariable("CLIPMATE_QUERY_WARN_MS");
        var originalDiagnostics = Environment.GetEnvironmentVariable("CLIPMATE_QUERY_DIAGNOSTICS");
        try
        {
            Environment.SetEnvironmentVariable("CLIPMATE_QUERY_WARN_MS", "100000");
            Environment.SetEnvironmentVariable("CLIPMATE_QUERY_DIAGNOSTICS", "true");

            var logger = new Mock<ILogger>();
            var factory = new SqliteConnectionFactory(builder.ToString());
            var service = new DatabaseService(factory, logger.Object);
            await service.InitializeAsync();

            _ = await service.GetItemsPagedAsync(0, 10);

            logger.Verify(
                x => x.Debug(It.Is<string>(message => message.Contains("查询诊断")), It.IsAny<object[]>()),
                Times.AtLeastOnce);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CLIPMATE_QUERY_WARN_MS", originalWarnMs);
            Environment.SetEnvironmentVariable("CLIPMATE_QUERY_DIAGNOSTICS", originalDiagnostics);
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
