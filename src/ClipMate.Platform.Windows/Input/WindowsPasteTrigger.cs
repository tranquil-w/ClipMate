using ClipMate.Platform.Abstractions.Input;
using ClipMate.Platform.Windows.Interop;
using Serilog;

namespace ClipMate.Platform.Windows.Input;

public sealed class WindowsPasteTrigger(ILogger logger) : IPasteTrigger
{
    private readonly ILogger _logger = logger;

    public Task TriggerPasteAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            KeyboardInput.SendCtrlV();
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "触发粘贴失败");
            throw;
        }
    }
}
