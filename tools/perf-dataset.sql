-- 生成混合类型的基准数据集（文本/文件/图片），默认插入约 530 条记录。
-- 运行方式示例（Windows 下）：
--   sqlite3 "%LocalAppData%\\ClipMate\\ClipMate.db" ".read tools/perf-dataset.sql"
-- 如果需要清空现有数据，可在执行前手动删除 ClipMate.db 或在下方取消注释 DELETE 语句。

PRAGMA journal_mode = WAL;

CREATE TABLE IF NOT EXISTS ClipboardItems (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Content BLOB NOT NULL,
    ContentType TEXT NOT NULL,
    CreatedAt DATETIME NOT NULL,
    IsFavorite INTEGER NOT NULL DEFAULT 0,
    ContentHash TEXT NULL
);

CREATE INDEX IF NOT EXISTS IX_ClipboardItems_CreatedAt ON ClipboardItems (CreatedAt DESC);

-- DELETE FROM ClipboardItems;

BEGIN TRANSACTION;

WITH RECURSIVE txt(i) AS (
    SELECT 1
    UNION ALL
    SELECT i + 1 FROM txt WHERE i < 260
)
INSERT INTO ClipboardItems (Content, ContentType, CreatedAt, IsFavorite)
SELECT CAST('Perf text sample #' || i || ' ' || substr(hex(randomblob(8)), 1, 8) AS BLOB),
       'Text',
       datetime('now', printf('-%d seconds', i)),
       CASE WHEN (i % 17) = 0 THEN 1 ELSE 0 END
FROM txt;

WITH RECURSIVE files(i) AS (
    SELECT 1
    UNION ALL
    SELECT i + 1 FROM files WHERE i < 180
)
INSERT INTO ClipboardItems (Content, ContentType, CreatedAt, IsFavorite)
SELECT CAST('["C:\\\\PerfData\\\\doc_' || i || '.txt","C:\\\\PerfData\\\\image_' || i || '.png"]' AS BLOB),
       'FileDropList',
       datetime('now', printf('-%d seconds', 300 + i)),
       0
FROM files;

WITH RECURSIVE imgs(i) AS (
    SELECT 1
    UNION ALL
    SELECT i + 1 FROM imgs WHERE i < 90
)
INSERT INTO ClipboardItems (Content, ContentType, CreatedAt, IsFavorite)
SELECT X'89504e470d0a1a0a0000000d4948445200000001000000010804000000b51c0c020000000b4944415478da63fcff1f0003030200ef9049970000000049454e44ae426082',
       'Image',
       datetime('now', printf('-%d seconds', 600 + i)),
       0
FROM imgs;

COMMIT;
