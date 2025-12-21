using ClipMate.Service.Infrastructure;
using ClipMate.Core.Models;
using ClipMate.Tests.TestHelpers;
using Dapper;
using System.Text;

namespace ClipMate.Tests.Services
{
    public class DatabaseServiceTests : TestBase
    {
        private readonly ISqliteConnectionFactory _connectionFactory;
        private readonly IDatabaseService _databaseService;

        public DatabaseServiceTests()
        {
            _connectionFactory = TestHost.Container.Resolve<ISqliteConnectionFactory>();
            _databaseService = TestHost.Container.Resolve<IDatabaseService>();
        }

        [Fact]
        public async Task DatabaseServiceTest()
        {
            await _databaseService.InitializeAsync();

            var tableName = "ClipboardItems";
            var query = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name=@TableName";
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            int count = connection.ExecuteScalar<int>(query, new { TableName = tableName });
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task GetAllItemsDescAsyncTest()
        {
            await _databaseService.InitializeAsync();

            // Clean up
            await CleanAsync();
            var result = await _databaseService.GetAllItemsDescAsync();
            Assert.Empty(result);

            // Insert item
            ClipboardItem item = BuildTextItem();
            item.Id = await InsertAsync(item);
            result = await _databaseService.GetAllItemsDescAsync();
            Assert.Single(result);
            Assert.Equal(item.Content, result.First().Content);
            Assert.Equal(item.ContentType, result.First().ContentType);
            Assert.Equal(item.CreatedAt, result.First().CreatedAt);

            // Clean up
            await CleanAsync();
            result = await _databaseService.GetAllItemsDescAsync();
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetItemAsyncTestAsync()
        {
            await _databaseService.InitializeAsync();

            // Insert item
            ClipboardItem item = BuildTextItem();
            item.Id = await InsertAsync(item);

            var result = await _databaseService.GetItemAsync(item.Id);
            Assert.NotNull(result);
            Assert.Equal(item.Id, result.Id);
            Assert.Equal(item.Content, result.Content);
            Assert.Equal(item.ContentType, result.ContentType);
            Assert.Equal(item.CreatedAt, result.CreatedAt);

            // Clean up
            await CleanAsync();
            result = await _databaseService.GetItemAsync(item.Id);
            Assert.Null(result);
        }

        [Fact]
        public async Task InsertItemAsyncTestAsync()
        {
            await _databaseService.InitializeAsync();

            ClipboardItem item = new()
            {
                Content = Encoding.UTF8.GetBytes("TestContent"),
                ContentType = "TestContentType",
                CreatedAt = DateTime.Now
            };
            var id = await _databaseService.InsertItemAsync(item);
            Assert.True(id > 0);

            await CleanAsync();
        }

        [Fact]
        public async Task UpdateItemAsyncTest()
        {
            await _databaseService.InitializeAsync();

            var item = BuildTextItem();
            int id = await InsertAsync(item);
            item.Id = id;
            item.Content = Encoding.UTF8.GetBytes("UpdatedContent");
            item.ContentType = "UpdatedContentType";
            item.CreatedAt = DateTime.Now.AddDays(1);
            var updated = await _databaseService.UpdateItemAsync(item);
            Assert.True(updated);

            var query2 = "SELECT * FROM ClipboardItems WHERE Id = @Id";
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            var updatedItem = await connection.QueryFirstOrDefaultAsync<ClipboardItem>(query2, new { Id = id });
            Assert.Equal(item.Content, updatedItem?.Content);
            Assert.Equal(item.ContentType, updatedItem?.ContentType);
            Assert.Equal(item.CreatedAt, updatedItem?.CreatedAt);

            await CleanAsync();
        }

        [Fact]
        public async Task DeleteItemAsyncTest()
        {
            await _databaseService.InitializeAsync();

            var item = BuildTextItem();
            item.Id = await InsertAsync(item);

            var selectQuery = "SELECT * FROM ClipboardItems";
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            var items = connection.Query<ClipboardItem>(selectQuery).ToList();
            Assert.True(items.Count > 0);

            var deleted = await _databaseService.DeleteItemAsync(item);
            Assert.True(deleted);

            await using var connection2 = await _connectionFactory.CreateOpenConnectionAsync();
            items = connection2.Query<ClipboardItem>(selectQuery).ToList();
            Assert.Empty(items);
        }

        [Fact]
        public async Task DatabasePragmaShouldBeOptimized()
        {
            await _databaseService.InitializeAsync();

            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            var journalMode = connection.ExecuteScalar<string>("PRAGMA journal_mode;");
            var synchronous = connection.ExecuteScalar<long>("PRAGMA synchronous;");
            var busyTimeout = connection.ExecuteScalar<long>("PRAGMA busy_timeout;");

            Assert.Equal("wal", journalMode?.ToLowerInvariant());
            Assert.Equal(1, synchronous); // NORMAL = 1
            Assert.Equal(5000, busyTimeout);
        }

        [Fact]
        public async Task CreatedAtIndexShouldExist()
        {
            var query = "SELECT count(*) FROM sqlite_master WHERE type='index' AND name='IX_ClipboardItems_CreatedAt'";

            await _databaseService.InitializeAsync();
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            var count = connection.ExecuteScalar<int>(query);

            Assert.Equal(1, count);
        }

        [Fact]
        public async Task UpdateFavoriteAsync_ShouldSetFavoriteToTrue()
        {
            // Arrange
            await _databaseService.InitializeAsync();
            await CleanAsync();
            var item = BuildTextItem();
            item.Id = await InsertAsync(item);

            // Act
            var result = await _databaseService.UpdateFavoriteAsync(item.Id, true);

            // Assert
            Assert.True(result);
            var updatedItem = await _databaseService.GetItemAsync(item.Id);
            Assert.NotNull(updatedItem);
            Assert.True(updatedItem.IsFavorite);

            await CleanAsync();
        }

        [Fact]
        public async Task UpdateFavoriteAsync_ShouldSetFavoriteToFalse()
        {
            // Arrange
            await _databaseService.InitializeAsync();
            await CleanAsync();
            var item = BuildTextItem();
            item.IsFavorite = true;
            item.Id = await InsertWithFavoriteAsync(item);

            // Act
            var result = await _databaseService.UpdateFavoriteAsync(item.Id, false);

            // Assert
            Assert.True(result);
            var updatedItem = await _databaseService.GetItemAsync(item.Id);
            Assert.NotNull(updatedItem);
            Assert.False(updatedItem.IsFavorite);

            await CleanAsync();
        }

        [Fact]
        public async Task UpdateFavoriteAsync_ShouldReturnFalseForNonExistentItem()
        {
            // Arrange
            await _databaseService.InitializeAsync();
            await CleanAsync();
            var nonExistentId = 99999;

            // Act
            var result = await _databaseService.UpdateFavoriteAsync(nonExistentId, true);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task InsertItemAsync_ShouldDefaultIsFavoriteToFalse()
        {
            // Arrange
            await _databaseService.InitializeAsync();
            await CleanAsync();
            var item = BuildTextItem();

            // Act
            var id = await _databaseService.InsertItemAsync(item);

            // Assert
            Assert.True(id > 0);
            var insertedItem = await _databaseService.GetItemAsync(id);
            Assert.NotNull(insertedItem);
            Assert.False(insertedItem.IsFavorite);

            await CleanAsync();
        }

        [Fact]
        public async Task GetAllItemsDescAsync_ShouldReturnCorrectIsFavoriteValue()
        {
            // Arrange
            await _databaseService.InitializeAsync();
            await CleanAsync();
            var item1 = BuildTextItem("Item1");
            item1.Id = await InsertAsync(item1);

            var item2 = BuildTextItem("Item2");
            item2.IsFavorite = true;
            item2.Id = await InsertWithFavoriteAsync(item2);

            // Act
            var items = (await _databaseService.GetAllItemsDescAsync()).ToList();

            // Assert
            Assert.Equal(2, items.Count);
            var retrievedItem1 = items.First(i => i.Id == item1.Id);
            var retrievedItem2 = items.First(i => i.Id == item2.Id);
            Assert.False(retrievedItem1.IsFavorite);
            Assert.True(retrievedItem2.IsFavorite);

            await CleanAsync();
        }

        private static ClipboardItem BuildTextItem(string content = "TestContent")
        {
            return new()
            {
                Content = Encoding.UTF8.GetBytes(content),
                ContentType = "TestContentType",
                CreatedAt = DateTime.Now
            };
        }

        private async Task<int> InsertAsync(ClipboardItem item)
        {
            var query = @"
                INSERT INTO ClipboardItems (Content, ContentType, CreatedAt)
                VALUES (@Content, @ContentType, @CreatedAt);
                SELECT last_insert_rowid();";

            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            return connection.ExecuteScalar<int>(query, item);
        }

        private async Task<int> InsertWithFavoriteAsync(ClipboardItem item)
        {
            var query = @"
                INSERT INTO ClipboardItems (Content, ContentType, CreatedAt, IsFavorite)
                VALUES (@Content, @ContentType, @CreatedAt, @IsFavorite);
                SELECT last_insert_rowid();";

            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            return connection.ExecuteScalar<int>(query, new
            {
                item.Content,
                item.ContentType,
                item.CreatedAt,
                IsFavorite = item.IsFavorite ? 1 : 0
            });
        }

        private async Task CleanAsync()
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            connection.Execute("DELETE FROM ClipboardItems");
        }
    }
}
