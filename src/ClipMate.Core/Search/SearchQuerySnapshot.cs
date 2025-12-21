using System;

namespace ClipMate.Core.Search;

/// <summary>
/// 标准化后的搜索查询快照，用于在一次刷新中保持一致的搜索字符串。
/// </summary>
public readonly record struct SearchQuerySnapshot(
    string Original,
    string Normalized,
    string LowerInvariant,
    string LowerInvariantNoDot,
    int Length,
    bool HasQuery)
{
    public static readonly SearchQuerySnapshot Empty = new(string.Empty, string.Empty, string.Empty, string.Empty, 0, false);

    public static SearchQuerySnapshot From(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Empty;
        }

        var normalized = query.Trim();
        if (normalized.Length == 0)
        {
            return Empty;
        }

        var lower = normalized.ToLowerInvariant();
        var lowerNoDot = normalized.TrimStart('.').ToLowerInvariant();

        return new SearchQuerySnapshot(
            query,
            normalized,
            lower,
            lowerNoDot,
            normalized.Length,
            true);
    }
}

