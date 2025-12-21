using ClipMate.Core.Models;
using ClipMate.Service.Infrastructure;
using Microsoft.Data.Sqlite;
using Moq;
using Serilog;
using System.Text;

namespace ClipMate.Service.Tests.Infrastructure;

public class DatabaseServiceTests
{
    [Fact]
    public async Task InsertItemAsync_WithDuplicateText_ShouldReturnMinusOne()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = "file:memdb-dup",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared
        };

        await using var keepAliveConnection = new SqliteConnection(builder.ToString());
        await keepAliveConnection.OpenAsync();

        var logger = new Mock<ILogger>();
        var factory = new SqliteConnectionFactory(builder.ToString());
        var service = new DatabaseService(factory, logger.Object);
        await service.InitializeAsync();

        var firstId = await service.InsertItemAsync(new ClipboardItem
        {
            ContentType = ClipboardContentTypes.Text,
            Content = Encoding.UTF8.GetBytes("dup"),
            CreatedAt = DateTime.Now,
            IsFavorite = false
        });

        var secondId = await service.InsertItemAsync(new ClipboardItem
        {
            ContentType = ClipboardContentTypes.Text,
            Content = Encoding.UTF8.GetBytes("dup"),
            CreatedAt = DateTime.Now,
            IsFavorite = false
        });

        Assert.True(firstId > 0);
        Assert.Equal(-1, secondId);
    }

    [Fact]
    public async Task CleanupOldItemsAsync_ShouldDeleteOldestNonFavoritesOnly()
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = "file:memdb-cleanup",
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
        for (var i = 0; i < 5; i++)
        {
            await service.InsertItemAsync(new ClipboardItem
            {
                ContentType = ClipboardContentTypes.Text,
                Content = Encoding.UTF8.GetBytes($"n{i}"),
                CreatedAt = now.AddMinutes(i),
                IsFavorite = false
            });
        }

        // 收藏项不应被清理
        await service.InsertItemAsync(new ClipboardItem
        {
            ContentType = ClipboardContentTypes.Text,
            Content = Encoding.UTF8.GetBytes("fav"),
            CreatedAt = now.AddMinutes(-100),
            IsFavorite = true
        });

        var deleted = await service.CleanupOldItemsAsync(limit: 2);
        Assert.Equal(3, deleted);

        var remaining = (await service.GetAllItemsDescAsync()).ToArray();
        Assert.Contains(remaining, item => item.IsFavorite);
        Assert.Equal(3, remaining.Length); // 2 个非收藏 + 1 个收藏
    }
}

