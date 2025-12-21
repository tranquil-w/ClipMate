# 性能压测数据集生成指引

用于在本地快速构造 500+ 条混合剪贴项（文本/文件/图片），便于复现搜索与滚动场景。

## 快速生成
1) 关闭正在运行的 ClipMate，确认 `%LocalAppData%\ClipMate\ClipMate.db` 未被占用。  
2) 在仓库根目录运行（方式二不依赖 `sqlite3`）：

方式一：使用 `sqlite3` 执行 SQL 脚本
```powershell
sqlite3 "$env:LocalAppData\\ClipMate\\ClipMate.db" ".read tools/perf-dataset.sql"
```

方式二：使用内置的 .NET 工具生成（跨平台）
```powershell
dotnet run --project tools/ClipMate.DbPerf/ClipMate.DbPerf.csproj -- --db "$env:LocalAppData\\ClipMate\\ClipMate.db" --rows 5000
```
3) 重新启动应用，列表中将包含约 260 条文本、180 条文件路径、90 条缩略图图片，合计 ~530 条记录。

> 提示：如需清空旧数据，可先删除本地 `ClipMate.db` 或在 `tools/perf-dataset.sql` 中取消 `DELETE FROM ClipboardItems;` 注释。

## 数据特点
- 文本：带随机后缀的短文本，验证文本搜索与截断。
- 文件：包含长路径与扩展名，覆盖扩展名/文件名/完整路径匹配。
- 图片：重复的 1x1 PNG 缩略图，用于验证预览降采样和滚动表现。

## 大数据集（1000/5000/10000）
可以直接修改 `tools/perf-dataset.sql` 中三段递归上限（260/180/90）来生成更大规模数据。例如：

- ~1000 条：`500/300/200`
- ~5000 条：`3000/1500/500`
- ~10000 条：`6500/2500/1000`

生成后建议用应用内“历史上限”设置配合验证（默认 500，可能会被清理逻辑影响）。

## 索引/分页验证（sqlite3）
在 `sqlite3` 里执行以下命令可查看是否命中索引（输出中应包含 `USING INDEX` 等关键词）：

```sql
EXPLAIN QUERY PLAN
SELECT * FROM ClipboardItems
WHERE ContentType = 'Text'
ORDER BY CreatedAt DESC
LIMIT 100 OFFSET 0;

EXPLAIN QUERY PLAN
SELECT * FROM ClipboardItems
WHERE IsFavorite = 1
ORDER BY CreatedAt DESC
LIMIT 100 OFFSET 0;
```

分页 OFFSET 性能可以打开计时后测试不同偏移量：

```sql
.timer on
SELECT * FROM ClipboardItems ORDER BY CreatedAt DESC LIMIT 100 OFFSET 0;
SELECT * FROM ClipboardItems ORDER BY CreatedAt DESC LIMIT 100 OFFSET 1000;
SELECT * FROM ClipboardItems ORDER BY CreatedAt DESC LIMIT 100 OFFSET 5000;
.timer off
```

## 索引/分页验证（无 sqlite3）
如果本机没有 `sqlite3`，可直接运行 `tools/ClipMate.DbPerf`（会输出索引列表、`EXPLAIN QUERY PLAN` 以及分页 offset 计时）。

示例输出（rows=5000）：
- `SEARCH ClipboardItems USING INDEX IX_ClipboardItems_ContentType_CreatedAt (ContentType=?)`
- `SEARCH ClipboardItems USING INDEX IX_ClipboardItems_IsFavorite_CreatedAt (IsFavorite=?)`
- `offset=0/500/1000/5000` 的查询耗时应在毫秒级（通常 <50ms）

## ContentHash 去重验证（可选）
可以在应用里反复复制同一段文本，观察数据库是否增长：

- 预期：相同内容再次插入时返回 `-1`（跳过插入），日志里出现“哈希匹配”相关提示。
- 旧数据兼容：老库升级后，旧行的 `ContentHash` 为 `NULL`，仍保留“最后一条对比”的兜底去重。

## 其他方法
- 如果没有 `sqlite3`，也可用任意 SQLite GUI 执行 `tools/perf-dataset.sql`。
- 需要更多数据时，可调整脚本中的递归上限以生成更大的集合。
