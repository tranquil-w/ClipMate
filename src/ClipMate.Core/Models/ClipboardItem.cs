namespace ClipMate.Core.Models;

public class ClipboardItem
{
    public int Id { get; set; }
    public required byte[] Content { get; set; }  // 使用 byte[] 存储二进制数据
    public required string ContentType { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsFavorite { get; set; }
    public string? ContentHash { get; set; }
}
