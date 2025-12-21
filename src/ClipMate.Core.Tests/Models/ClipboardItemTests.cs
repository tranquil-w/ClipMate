using ClipMate.Core.Models;

namespace ClipMate.Core.Tests.Models;

public class ClipboardItemTests
{
    [Fact]
    public void NewItem_WithRequiredFields_ShouldBeCreatable()
    {
        var item = new ClipboardItem
        {
            Id = 1,
            ContentType = "Text",
            Content = [],
            CreatedAt = DateTime.UtcNow,
            IsFavorite = false
        };

        Assert.Equal(1, item.Id);
        Assert.Equal("Text", item.ContentType);
        Assert.NotNull(item.Content);
    }
}

