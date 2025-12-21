using ClipMate.Platform.Abstractions.Window;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ClipMate.Service.Clipboard;

public interface IPasteTargetWindowService
{
    nint PasteTargetWindowHandle { get; }

    void FreezePasteTarget();

    void UnfreezePasteTarget();

    Task<(bool Ready, nint CurrentForeground)> WaitForReadyToPasteAsync(TimeSpan timeout, CancellationToken cancellationToken = default);
}
