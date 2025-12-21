namespace ClipMate.Platform.Abstractions.Window;

public interface IMainWindowController
{
    void CloseMainWindow();

    void ShowMainWindow();

    void HideMainWindow();

    Task HideMainWindowAsync(CancellationToken cancellationToken = default);

    void SetPosition(ScreenPoint position);
}
