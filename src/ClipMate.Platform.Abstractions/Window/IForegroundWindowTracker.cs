using System;

namespace ClipMate.Platform.Abstractions.Window;

public interface IForegroundWindowTracker : IDisposable
{
    event EventHandler<nint>? ForegroundWindowChanged;

    nint CurrentForegroundWindowHandle { get; }

    nint LastExternalForegroundWindowHandle { get; }

    bool IsWindowFromCurrentProcess(nint windowHandle);

    void Start();

    void Stop();
}
