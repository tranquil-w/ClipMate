using ClipMate.UI.Abstractions;

namespace ClipMate.Tests.TestHelpers;

public sealed class ImmediateUiDispatcher : IUiDispatcher
{
    public bool CheckAccess() => true;

    public void Invoke(Action action)
    {
        action();
    }

    public Task InvokeAsync(
        Action action,
        UiDispatcherPriority priority = UiDispatcherPriority.Normal,
        CancellationToken cancellationToken = default)
    {
        action();
        return Task.CompletedTask;
    }
}
