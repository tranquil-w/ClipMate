using ClipMate.Core.Models;
using Dapper;
using Microsoft.Data.Sqlite;
using Serilog;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace ClipMate.Service.Infrastructure;

public class DatabaseService : IDatabaseService
{
    private const string QueryWarnMsEnv = "CLIPMATE_QUERY_WARN_MS";
    private const string QueryDiagnosticsEnv = "CLIPMATE_QUERY_DIAGNOSTICS";
    private const string LegacyTableName = "ClipMate";

    private const string BaseTableSql = @"
        CREATE TABLE IF NOT EXISTS ClipboardItems (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Content BLOB NOT NULL,        -- 用于存储文本或图像数据
            ContentType TEXT NOT NULL,    -- 用于表示数据类型 (如 'Text' 或 'Image')
            CreatedAt DATETIME NOT NULL,   -- 记录创建时间
            IsFavorite INTEGER NOT NULL DEFAULT 0, -- 是否收藏 (0/1)
            ContentHash TEXT NULL         -- 内容哈希（用于全局去重，旧数据允许为 NULL）
        )";

    private const string CreatedAtIndexSql =
        "CREATE INDEX IF NOT EXISTS IX_ClipboardItems_CreatedAt ON ClipboardItems (CreatedAt DESC)";

    private const string ContentTypeIndexSql =
        "CREATE INDEX IF NOT EXISTS IX_ClipboardItems_ContentType_CreatedAt ON ClipboardItems (ContentType, CreatedAt DESC)";

    private const string IsFavoriteIndexSql =
        "CREATE INDEX IF NOT EXISTS IX_ClipboardItems_IsFavorite_CreatedAt ON ClipboardItems (IsFavorite, CreatedAt DESC)";

    private const string ContentHashIndexSql =
        "CREATE INDEX IF NOT EXISTS IX_ClipboardItems_ContentHash ON ClipboardItems (ContentHash)";

    private static readonly HashSet<string> RequiredColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "Id",
        "Content",
        "ContentType",
        "CreatedAt",
        "IsFavorite",
    };

    private readonly ISqliteConnectionFactory _connectionFactory;
    private readonly ILogger _logger;
    private readonly int _queryWarningThresholdMs;
    private readonly bool _enableQueryDiagnostics;
    private Task? _initializeTask;
    private readonly object _initializeGate = new();

    public DatabaseService(ISqliteConnectionFactory connectionFactory, ILogger logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;

        _queryWarningThresholdMs =
            int.TryParse(Environment.GetEnvironmentVariable(QueryWarnMsEnv), out var warnMs) && warnMs >= 0
                ? warnMs
                : 100;
        _enableQueryDiagnostics = ParseBoolEnvironmentVariable(QueryDiagnosticsEnv);
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return EnsureInitializedAsync(cancellationToken);
    }

    private Task EnsureInitializedAsync(CancellationToken cancellationToken)
    {
        Task? existingTask;
        lock (_initializeGate)
        {
            existingTask = _initializeTask;
            if (existingTask == null || existingTask.IsFaulted || existingTask.IsCanceled)
            {
                existingTask = InitializeDatabaseAsync(cancellationToken);
                _initializeTask = existingTask;
            }
        }

        return existingTask;
    }

    private async Task InitializeDatabaseAsync(CancellationToken cancellationToken)
    {
        _logger.Information("开始初始化数据库");

        bool recreate = false;
        bool migrated = false;

        await using (var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
        {
            var schemaColumns = await GetTableColumnsAsync(connection);
            if (schemaColumns.Count == 0)
            {
                if (await HasLegacyTableAsync(connection, cancellationToken))
                {
                    recreate = true;
                }
                else
                {
                    await CreateSchemaAsync(connection);
                    _logger.Information("数据库初始化成功（新建 schema）");
                    return;
                }
            }
            else if (!IsSchemaCompatible(schemaColumns))
            {
                recreate = true;
            }
            else
            {
                migrated = await ApplyMigrationsAsync(connection, schemaColumns, cancellationToken);
            }
        }

        if (recreate)
        {
            await RecreateDatabaseAsync(cancellationToken);
            _logger.Information("数据库初始化成功（旧 schema 已旁路重建）");
            return;
        }

        if (migrated)
        {
            _logger.Information("数据库初始化成功（已完成 schema 迁移/索引更新）");
            return;
        }

        _logger.Information("数据库初始化成功");
    }

    private static async Task<IReadOnlyList<string>> GetTableColumnsAsync(SqliteConnection connection)
    {
        var columns = await connection.QueryAsync<string>("SELECT name FROM pragma_table_info('ClipboardItems')");
        return columns.ToArray();
    }

    private static async Task<bool> HasLegacyTableAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT count(*) FROM sqlite_master WHERE type='table' AND name=@TableName",
            new { TableName = LegacyTableName },
            cancellationToken: cancellationToken));
        return count > 0;
    }

    private static bool IsSchemaCompatible(IReadOnlyList<string> columns)
    {
        if (columns.Count == 0)
        {
            return false;
        }

        foreach (var requiredColumn in RequiredColumns)
        {
            if (!columns.Contains(requiredColumn, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static async Task CreateSchemaAsync(SqliteConnection connection)
    {
        await connection.ExecuteAsync(BaseTableSql);
        await CreateIndexesAsync(connection, CancellationToken.None);
    }

    private async Task RecreateDatabaseAsync(CancellationToken cancellationToken)
    {
        if (_connectionFactory.IsInMemory)
        {
            await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            await connection.ExecuteAsync("DROP TABLE IF EXISTS ClipboardItems;");
            await connection.ExecuteAsync($"DROP TABLE IF EXISTS {LegacyTableName};");
            await CreateSchemaAsync(connection);
            return;
        }

        if (_connectionFactory.DatabaseFilePath is { Length: > 0 } databasePath && File.Exists(databasePath))
        {
            var directory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var backupPath = $"{databasePath}.bak-{DateTime.UtcNow:yyyyMMddHHmmss}";
            File.Move(databasePath, backupPath, overwrite: true);
        }

        await using (var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken))
        {
            await CreateSchemaAsync(connection);
        }
    }

    private static async Task CreateIndexesAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(CreatedAtIndexSql, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition(ContentTypeIndexSql, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition(IsFavoriteIndexSql, cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition(ContentHashIndexSql, cancellationToken: cancellationToken));
    }

    private async Task<bool> ApplyMigrationsAsync(
        SqliteConnection connection,
        IReadOnlyList<string> schemaColumns,
        CancellationToken cancellationToken)
    {
        var migrated = false;

        if (!schemaColumns.Contains("ContentHash", StringComparer.OrdinalIgnoreCase))
        {
            await connection.ExecuteAsync(new CommandDefinition(
                "ALTER TABLE ClipboardItems ADD COLUMN ContentHash TEXT",
                cancellationToken: cancellationToken));

            _logger.Information("数据库迁移：已添加 ContentHash 列");
            migrated = true;
        }

        await CreateIndexesAsync(connection, cancellationToken);
        return migrated;
    }

    public async Task<IEnumerable<ClipboardItem>> GetAllItemsDescAsync()
    {
        try
        {
            await EnsureInitializedAsync(CancellationToken.None);

            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            var query = "SELECT * FROM ClipboardItems ORDER BY CreatedAt DESC";
            var sw = Stopwatch.StartNew();
            var items = (await connection.QueryAsync<ClipboardItem>(query)).ToArray();
            sw.Stop();

            LogQueryTiming("GetAllItemsDescAsync", query, sw.ElapsedMilliseconds, items.Length);
            return items;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "查询剪贴板记录失败");
            throw;
        }
    }

    public async Task<IReadOnlyList<ClipboardItem>> GetItemsPagedAsync(int offset, int limit, CancellationToken cancellationToken = default)
    {
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "offset 必须 >= 0");
        }

        if (limit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "limit 必须 > 0");
        }

        await EnsureInitializedAsync(cancellationToken);

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        const string query = "SELECT * FROM ClipboardItems ORDER BY CreatedAt DESC LIMIT @Limit OFFSET @Offset";

        var sw = Stopwatch.StartNew();
        var items = (await connection.QueryAsync<ClipboardItem>(new CommandDefinition(
            query,
            new { Limit = limit, Offset = offset },
            cancellationToken: cancellationToken))).ToArray();
        sw.Stop();

        LogQueryTiming("GetItemsPagedAsync", query, sw.ElapsedMilliseconds, items.Length, $"offset={offset}, limit={limit}");
        return items;
    }

    public async Task<ClipboardItem?> GetItemAsync(int id)
    {
        try
        {
            await EnsureInitializedAsync(CancellationToken.None);

            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            var query = "SELECT * FROM ClipboardItems WHERE Id = @Id";
            var sw = Stopwatch.StartNew();
            var item = await connection.QueryFirstOrDefaultAsync<ClipboardItem>(query, new { Id = id });
            sw.Stop();

            LogQueryTiming("GetItemAsync", query, sw.ElapsedMilliseconds, item == null ? 0 : 1, $"id={id}");
            return item;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "查询剪贴板项 {Id} 失败", id);
            throw;
        }
    }

    public async Task<int> InsertItemAsync(ClipboardItem item)
    {
        try
        {
            await EnsureInitializedAsync(CancellationToken.None);

            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();

            var sw = Stopwatch.StartNew();

            // 只对 Text 和 FileDropList 类型进行去重检查
            if (ShouldCheckDuplication(item.ContentType))
            {
                item.ContentHash = ComputeContentHash(item.Content);

                var duplicateIdByHash = await connection.QueryFirstOrDefaultAsync<int?>(new CommandDefinition(
                    "SELECT Id FROM ClipboardItems WHERE ContentHash = @Hash LIMIT 1",
                    new { Hash = item.ContentHash }));

                if (duplicateIdByHash.HasValue)
                {
                    // 更新重复项的 CreatedAt 时间戳
                    await connection.ExecuteAsync(
                        "UPDATE ClipboardItems SET CreatedAt = @CreatedAt WHERE Id = @Id",
                        new { Id = duplicateIdByHash.Value, CreatedAt = DateTime.Now });
                    sw.Stop();
                    LogQueryTiming("InsertItemAsync", "dedup(ContentHash)", sw.ElapsedMilliseconds, 0);
                    _logger.Information("检测到重复的剪贴板内容（哈希匹配），更新时间戳并返回ID。类型：{ContentType}", item.ContentType);
                    return duplicateIdByHash.Value;
                }

                var lastItem = await connection.QueryFirstOrDefaultAsync<ClipboardItem>(
                    "SELECT * FROM ClipboardItems ORDER BY Id DESC LIMIT 1");

                if (lastItem != null &&
                    string.IsNullOrEmpty(lastItem.ContentHash) &&
                    lastItem.ContentType == item.ContentType &&
                    IsDuplicateContent(lastItem.Content, item.Content))
                {
                    // 更新重复项的 CreatedAt 时间戳和 ContentHash
                    await connection.ExecuteAsync(
                        "UPDATE ClipboardItems SET CreatedAt = @CreatedAt, ContentHash = @Hash WHERE Id = @Id",
                        new { Id = lastItem.Id, CreatedAt = DateTime.Now, Hash = item.ContentHash });
                    sw.Stop();
                    LogQueryTiming("InsertItemAsync", "dedup(last-item)", sw.ElapsedMilliseconds, 0);
                    _logger.Debug("检测到重复的剪贴板内容（旧数据无哈希），更新时间戳并返回ID。类型：{ContentType}", item.ContentType);
                    return lastItem.Id;
                }
            }
            else
            {
                item.ContentHash = null;
            }

            var query = @"
                INSERT INTO ClipboardItems (Content, ContentType, CreatedAt, IsFavorite, ContentHash)
                VALUES (@Content, @ContentType, @CreatedAt, @IsFavorite, @ContentHash);
                SELECT last_insert_rowid();";
            var newId = await connection.ExecuteScalarAsync<int>(query, item);
            sw.Stop();

            LogQueryTiming("InsertItemAsync", "INSERT", sw.ElapsedMilliseconds, 1, $"type={item.ContentType}");
            _logger.Information("插入剪贴板项成功，ID：{Id}，类型：{ContentType}", newId, item.ContentType);
            return newId;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "插入剪贴板项失败，类型：{ContentType}", item.ContentType);
            throw;
        }
    }

    public async Task<bool> UpdateItemAsync(ClipboardItem item)
    {
        try
        {
            await EnsureInitializedAsync(CancellationToken.None);

            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();

            item.ContentHash = ShouldCheckDuplication(item.ContentType)
                ? ComputeContentHash(item.Content)
                : null;

            var sw = Stopwatch.StartNew();
            var query = @"
                UPDATE ClipboardItems
                SET Content = @Content, ContentType = @ContentType, CreatedAt = @CreatedAt, ContentHash = @ContentHash
                WHERE Id = @Id";
            var affectedRows = await connection.ExecuteAsync(query, item);
            sw.Stop();

            LogQueryTiming("UpdateItemAsync", query, sw.ElapsedMilliseconds, affectedRows, $"id={item.Id}");
            var success = affectedRows > 0;

            if (success)
            {
                _logger.Information("更新剪贴板项成功，ID：{Id}", item.Id);
            }
            else
            {
                _logger.Warning("更新剪贴板项失败，项不存在，ID：{Id}", item.Id);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "更新剪贴板项失败，ID：{Id}", item.Id);
            throw;
        }
    }

    public async Task<bool> DeleteItemAsync(ClipboardItem item)
    {
        try
        {
            await EnsureInitializedAsync(CancellationToken.None);

            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            var sw = Stopwatch.StartNew();
            var query = "DELETE FROM ClipboardItems WHERE Id = @Id";
            var affectedRows = await connection.ExecuteAsync(query, item);
            sw.Stop();

            LogQueryTiming("DeleteItemAsync", query, sw.ElapsedMilliseconds, affectedRows, $"id={item.Id}");
            var success = affectedRows > 0;

            if (success)
            {
                _logger.Information("删除剪贴板项成功，ID：{Id}", item.Id);
            }
            else
            {
                _logger.Warning("删除剪贴板项失败，项不存在，ID：{Id}", item.Id);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "删除剪贴板项失败，ID：{Id}", item.Id);
            throw;
        }
    }

    public async Task<bool> UpdateFavoriteAsync(int id, bool isFavorite)
    {
        try
        {
            await EnsureInitializedAsync(CancellationToken.None);

            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            var sw = Stopwatch.StartNew();
            var query = @"
                UPDATE ClipboardItems
                SET IsFavorite = @IsFavorite
                WHERE Id = @Id";
            var affectedRows = await connection.ExecuteAsync(query, new { Id = id, IsFavorite = isFavorite ? 1 : 0 });
            sw.Stop();

            LogQueryTiming("UpdateFavoriteAsync", query, sw.ElapsedMilliseconds, affectedRows, $"id={id}, isFavorite={isFavorite}");
            var success = affectedRows > 0;

            if (success)
            {
                _logger.Information("更新收藏状态成功，ID：{Id}，IsFavorite：{IsFavorite}", id, isFavorite);
            }
            else
            {
                _logger.Warning("更新收藏状态失败，项不存在，ID：{Id}", id);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "更新收藏状态失败，ID：{Id}", id);
            throw;
        }
    }

    /// <summary>
    /// 判断指定类型是否需要进行去重检查
    /// </summary>
    private static bool ShouldCheckDuplication(string contentType)
    {
        return contentType == ClipboardContentTypes.Text || contentType == ClipboardContentTypes.FileDropList;
    }

    /// <summary>
    /// 比较两个字节数组内容是否相同
    /// </summary>
    private static bool IsDuplicateContent(byte[] content1, byte[] content2)
    {
        if (content1.Length != content2.Length)
            return false;

        return content1.SequenceEqual(content2);
    }

    private static string ComputeContentHash(byte[] content)
    {
        var hash = SHA256.HashData(content);
        return Convert.ToHexString(hash);
    }

    private static bool ParseBoolEnvironmentVariable(string envName)
    {
        var value = Environment.GetEnvironmentVariable(envName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
               bool.TryParse(value, out var parsed) && parsed;
    }

    private void LogQueryTiming(
        string queryName,
        string query,
        long elapsedMs,
        int rowCount,
        string? extra = null)
    {
        if (_enableQueryDiagnostics)
        {
            if (string.IsNullOrEmpty(extra))
            {
                _logger.Debug("查询诊断: {QueryName}, 耗时: {Ms}ms, 返回: {Count}行, SQL: {Query}",
                    queryName, elapsedMs, rowCount, query);
            }
            else
            {
                _logger.Debug("查询诊断: {QueryName}, {Extra}, 耗时: {Ms}ms, 返回: {Count}行, SQL: {Query}",
                    queryName, extra, elapsedMs, rowCount, query);
            }
        }

        if (elapsedMs >= _queryWarningThresholdMs)
        {
            _logger.Warning("慢查询: {Query}, 耗时: {Ms}ms, 返回: {Count}行",
                query, elapsedMs, rowCount);
        }
    }

    public async Task<int> CleanupOldItemsAsync(int limit)
    {
        try
        {
            await EnsureInitializedAsync(CancellationToken.None);

            await using var connection = await _connectionFactory.CreateOpenConnectionAsync();
            var sw = Stopwatch.StartNew();
            // 获取非收藏记录的总数
            var nonFavoriteCount = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM ClipboardItems WHERE IsFavorite = 0");

            if (nonFavoriteCount <= limit)
            {
                _logger.Debug("非收藏记录数 {Count} 未超过上限 {Limit}，无需清理", nonFavoriteCount, limit);
                return 0;
            }

            // 计算需要删除的记录数
            var deleteCount = nonFavoriteCount - limit;

            // 删除最旧的非收藏记录
            var deletedRows = await connection.ExecuteAsync(@"
                DELETE FROM ClipboardItems
                WHERE Id IN (
                    SELECT Id FROM ClipboardItems
                    WHERE IsFavorite = 0
                    ORDER BY CreatedAt ASC
                    LIMIT @DeleteCount
                )", new { DeleteCount = deleteCount });
            sw.Stop();

            LogQueryTiming("CleanupOldItemsAsync", "DELETE(old-items)", sw.ElapsedMilliseconds, deletedRows, $"limit={limit}");

            _logger.Information("清理历史记录完成，删除了 {DeletedCount} 条非收藏记录", deletedRows);
            return deletedRows;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "清理历史记录时发生错误");
            throw;
        }
    }
}
