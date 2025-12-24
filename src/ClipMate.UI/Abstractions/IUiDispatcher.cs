namespace ClipMate.UI.Abstractions;

public enum UiDispatcherPriority
{
    Background,
    Normal,
    High
}

public interface IUiDispatcher
{
    bool CheckAccess();

    void Invoke(Action action);

    Task InvokeAsync(
        Action action,
        UiDispatcherPriority priority = UiDispatcherPriority.Normal,
        CancellationToken cancellationToken = default);
}
