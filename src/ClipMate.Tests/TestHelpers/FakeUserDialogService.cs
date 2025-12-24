using ClipMate.UI.Abstractions;

namespace ClipMate.Tests.TestHelpers;

public sealed class FakeUserDialogService : IUserDialogService
{
    public Task<bool> ConfirmAsync(string message, string title)
    {
        return Task.FromResult(true);
    }

    public Task ShowErrorAsync(string message, string title)
    {
        return Task.CompletedTask;
    }
}
