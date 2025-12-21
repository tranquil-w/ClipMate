using Microsoft.Data.Sqlite;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

static string? GetArgValue(string[] args, string name)
{
    for (var i = 0; i < args.Length; i++)
    {
        if (!string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
        {
            continue;
        }

        if (i + 1 >= args.Length)
        {
            return null;
        }

        return args[i + 1];
    }

    return null;
}

static bool HasArg(string[] args, string name) =>
    args.Any(arg => string.Equals(arg, name, StringComparison.OrdinalIgnoreCase));

static int GetIntArg(string[] args, string name, int defaultValue)
{
    var value = GetArgValue(args, name);
    return int.TryParse(value, out var parsed) ? parsed : defaultValue;
}

static string ComputeContentHash(byte[] content) =>
    Convert.ToHexString(SHA256.HashData(content));

static async Task ExecuteNonQueryAsync(SqliteConnection connection, string sql)
{
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    await command.ExecuteNonQueryAsync();
}

static async Task<IReadOnlyList<string>> QueryStringsAsync(SqliteConnection connection, string sql)
{
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    await using var reader = await command.ExecuteReaderAsync();

    var results = new List<string>();
    while (await reader.ReadAsync())
    {
        results.Add(reader.GetString(0));
    }

    return results;
}

static async Task<int> ExecuteCountAsync(SqliteConnection connection, string sql, Action<SqliteParameterCollection>? bind = null)
{
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    bind?.Invoke(command.Parameters);
    var result = await command.ExecuteScalarAsync();
    return Convert.ToInt32(result);
}

static async Task<long> TimeQueryAsync(SqliteConnection connection, string sql, Action<SqliteParameterCollection>? bind = null)
{
    var sw = Stopwatch.StartNew();
    _ = await ExecuteCountAsync(connection, sql, bind);
    sw.Stop();
    return sw.ElapsedMilliseconds;
}

static async Task EnsureSchemaAsync(SqliteConnection connection)
{
    await ExecuteNonQueryAsync(connection, @"
CREATE TABLE IF NOT EXISTS ClipboardItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Content BLOB NOT NULL,
    ContentType TEXT NOT NULL,
    CreatedAt DATETIME NOT NULL,
    IsFavorite INTEGER NOT NULL DEFAULT 0,
    ContentHash TEXT NULL
);");

    await ExecuteNonQueryAsync(connection, "CREATE INDEX IF NOT EXISTS IX_ClipboardItems_CreatedAt ON ClipboardItems (CreatedAt DESC);");
    await ExecuteNonQueryAsync(connection, "CREATE INDEX IF NOT EXISTS IX_ClipboardItems_ContentType_CreatedAt ON ClipboardItems (ContentType, CreatedAt DESC);");
    await ExecuteNonQueryAsync(connection, "CREATE INDEX IF NOT EXISTS IX_ClipboardItems_IsFavorite_CreatedAt ON ClipboardItems (IsFavorite, CreatedAt DESC);");
    await ExecuteNonQueryAsync(connection, "CREATE INDEX IF NOT EXISTS IX_ClipboardItems_ContentHash ON ClipboardItems (ContentHash);");
}

static async Task PopulateAsync(SqliteConnection connection, int totalRows)
{
    var now = DateTime.UtcNow;
    var textRows = (int)(totalRows * 0.65);
    var fileRows = (int)(totalRows * 0.25);
    var imageRows = Math.Max(0, totalRows - textRows - fileRows);

    await ExecuteNonQueryAsync(connection, "BEGIN TRANSACTION;");

    await using (var insert = connection.CreateCommand())
    {
        insert.CommandText = @"
INSERT INTO ClipboardItems (Content, ContentType, CreatedAt, IsFavorite, ContentHash)
VALUES ($content, $type, $createdAt, $isFavorite, $hash);";
        var contentParam = insert.Parameters.Add("$content", SqliteType.Blob);
        var typeParam = insert.Parameters.Add("$type", SqliteType.Text);
        var createdAtParam = insert.Parameters.Add("$createdAt", SqliteType.Text);
        var favoriteParam = insert.Parameters.Add("$isFavorite", SqliteType.Integer);
        var hashParam = insert.Parameters.Add("$hash", SqliteType.Text);

        for (var i = 0; i < totalRows; i++)
        {
            string type;
            byte[] content;
            string? hash = null;
            var isFavorite = 0;

            if (i < textRows)
            {
                type = "Text";
                var text = $"Perf text sample #{i} {Guid.NewGuid():N}"[..48];
                content = Encoding.UTF8.GetBytes(text);
                hash = ComputeContentHash(content);
                isFavorite = i % 17 == 0 ? 1 : 0;
            }
            else if (i < textRows + fileRows)
            {
                type = "FileDropList";
                var text = $"[\"C:\\\\PerfData\\\\doc_{i}.txt\",\"C:\\\\PerfData\\\\image_{i}.png\"]";
                content = Encoding.UTF8.GetBytes(text);
                hash = ComputeContentHash(content);
            }
            else
            {
                type = "Image";
                content = Convert.FromHexString("89504e470d0a1a0a0000000d4948445200000001000000010804000000b51c0c020000000b4944415478da63fcff1f0003030200ef9049970000000049454e44ae426082");
            }

            contentParam.Value = content;
            typeParam.Value = type;
            createdAtParam.Value = now.AddSeconds(-i).ToString("O");
            favoriteParam.Value = isFavorite;
            hashParam.Value = (object?)hash ?? DBNull.Value;

            await insert.ExecuteNonQueryAsync();
        }
    }

    await ExecuteNonQueryAsync(connection, "COMMIT;");
}

static async Task PrintExplainAsync(SqliteConnection connection, string sql)
{
    Console.WriteLine();
    Console.WriteLine("EXPLAIN QUERY PLAN");
    Console.WriteLine(sql.Trim());

    await using var command = connection.CreateCommand();
    command.CommandText = $"EXPLAIN QUERY PLAN {sql}";
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        Console.WriteLine($"- {reader.GetString(3)}");
    }
}

static void PrintUsage()
{
    Console.WriteLine("用法:");
    Console.WriteLine("  dotnet run --project tools/ClipMate.DbPerf/ClipMate.DbPerf.csproj -- --db <path> [--reset] [--rows <n>]");
    Console.WriteLine();
    Console.WriteLine("示例:");
    Console.WriteLine("  dotnet run --project tools/ClipMate.DbPerf/ClipMate.DbPerf.csproj -- --db /tmp/clipmate-perf.db --reset --rows 10000");
}

var dbPath = GetArgValue(args, "--db");
if (string.IsNullOrWhiteSpace(dbPath))
{
    PrintUsage();
    return 2;
}

var reset = HasArg(args, "--reset");
var rows = GetIntArg(args, "--rows", 0);

var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = dbPath,
    Pooling = false
}.ToString();

await using var connection = new SqliteConnection(connectionString);
await connection.OpenAsync();

if (reset)
{
    await ExecuteNonQueryAsync(connection, "DROP TABLE IF EXISTS ClipboardItems;");
}

await EnsureSchemaAsync(connection);

if (rows > 0)
{
    var existing = await ExecuteCountAsync(connection, "SELECT COUNT(*) FROM ClipboardItems;");
    if (existing == 0)
    {
        Console.WriteLine($"生成数据集: rows={rows}, db={dbPath}");
        await PopulateAsync(connection, rows);
    }
    else
    {
        Console.WriteLine($"跳过生成：表已有数据 rows={existing}（如需重置请加 --reset）");
    }
}

var indexNames = (await QueryStringsAsync(connection, "SELECT name FROM pragma_index_list('ClipboardItems')")).OrderBy(x => x).ToArray();
Console.WriteLine();
Console.WriteLine("Indexes:");
foreach (var name in indexNames)
{
    Console.WriteLine($"- {name}");
}

await PrintExplainAsync(connection, @"
SELECT * FROM ClipboardItems
WHERE ContentType = 'Text'
ORDER BY CreatedAt DESC
LIMIT 100 OFFSET 0;");

await PrintExplainAsync(connection, @"
SELECT * FROM ClipboardItems
WHERE IsFavorite = 1
ORDER BY CreatedAt DESC
LIMIT 100 OFFSET 0;");

Console.WriteLine();
Console.WriteLine("Paging timings (ms):");
var offsets = new[] { 0, 500, 1000, 5000 };
foreach (var offset in offsets)
{
    var ms = await TimeQueryAsync(
        connection,
        "SELECT COUNT(*) FROM (SELECT 1 FROM ClipboardItems ORDER BY CreatedAt DESC LIMIT $limit OFFSET $offset);",
        parameters =>
        {
            _ = parameters.AddWithValue("$limit", 100);
            _ = parameters.AddWithValue("$offset", offset);
        });
    Console.WriteLine($"- offset={offset}: {ms}ms");
}

return 0;

