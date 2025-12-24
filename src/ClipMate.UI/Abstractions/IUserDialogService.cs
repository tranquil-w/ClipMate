namespace ClipMate.UI.Abstractions;

public interface IUserDialogService
{
    Task<bool> ConfirmAsync(string message, string title);

    Task ShowErrorAsync(string message, string title);
}
