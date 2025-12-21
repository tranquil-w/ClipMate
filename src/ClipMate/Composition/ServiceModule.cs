using ClipMate.Service.Clipboard;
using ClipMate.Service.Infrastructure;
using Prism.Ioc;

namespace ClipMate.Composition;

internal static class ServiceModule
{
    internal static void RegisterServiceLayer(this IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<IClipboardItemRepository, DatabaseClipboardItemRepository>();
        containerRegistry.RegisterSingleton<IClipboardCaptureUseCase, ClipboardCaptureUseCase>();
        containerRegistry.RegisterSingleton<IPasteTargetWindowService, PasteTargetWindowService>();
        containerRegistry.RegisterSingleton<IClipboardPasteUseCase, ClipboardPasteUseCase>();
        containerRegistry.RegisterSingleton<IClipboardHistoryUseCase, ClipboardHistoryUseCase>();
    }
}
