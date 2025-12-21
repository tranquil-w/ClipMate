using ClipMate.Platform.Abstractions.Window;
using System.Runtime.InteropServices;

namespace ClipMate.Platform.Windows.Windowing;

public sealed class WindowsWindowPositionProvider : IWindowPositionProvider
{
    public ScreenPoint? GetCaretPosition()
    {
        var point = ClipMate.Infrastructure.WindowPositionCalculator.GetCaretPosition();
        if (point == null)
        {
            return null;
        }

        return new ScreenPoint((int)Math.Round(point.Value.X), (int)Math.Round(point.Value.Y));
    }

    public ScreenPoint GetMousePosition()
    {
        var point = ClipMate.Infrastructure.WindowPositionCalculator.GetMousePosition();
        return new ScreenPoint((int)Math.Round(point.X), (int)Math.Round(point.Y));
    }

    public ScreenRect GetWorkArea(ScreenPoint referencePoint)
    {
        var workArea = ClipMate.Infrastructure.WindowPositionCalculator.GetWorkArea(
            new System.Windows.Point(referencePoint.X, referencePoint.Y));

        return new ScreenRect(workArea.Left, workArea.Top, workArea.Right, workArea.Bottom);
    }

    public DpiScale GetDpiScale(ScreenPoint referencePoint)
    {
        try
        {
            var point = new POINT(referencePoint.X, referencePoint.Y);
            var monitor = MonitorFromPoint(point, MONITOR_DEFAULTTONEAREST);
            if (monitor == nint.Zero)
            {
                return new DpiScale(1.0, 1.0);
            }

            var hr = GetDpiForMonitor(monitor, MonitorDpiType.EffectiveDpi, out var dpiX, out var dpiY);
            if (hr != 0 || dpiX == 0 || dpiY == 0)
            {
                return new DpiScale(1.0, 1.0);
            }

            return new DpiScale(dpiX / 96.0, dpiY / 96.0);
        }
        catch
        {
            return new DpiScale(1.0, 1.0);
        }
    }

    private const int MONITOR_DEFAULTTONEAREST = 2;

    private enum MonitorDpiType
    {
        EffectiveDpi = 0
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct POINT
    {
        public readonly int X;
        public readonly int Y;

        public POINT(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    [DllImport("user32.dll")]
    private static extern nint MonitorFromPoint(POINT pt, int dwFlags);

    [DllImport("shcore.dll")]
    private static extern int GetDpiForMonitor(
        nint hmonitor,
        MonitorDpiType dpiType,
        out uint dpiX,
        out uint dpiY);
}

