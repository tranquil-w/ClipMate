using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Text;

namespace ClipMate.Infrastructure
{
    public static class WindowPositionCalculator
    {
        public static Point? GetCaretPosition()
        {
            try
            {
                var caret = GetCaretPositionFromGuiThreadInfo();
                if (caret != null)
                {
                    return caret;
                }

                return GetCaretPositionFromUiAutomation();
            }
            catch
            {
                return null;
            }
        }

        public static Point GetMousePosition()
        {
            GetCursorPos(out POINT point);
            return new Point(point.X, point.Y);
        }

        public static RECT GetWorkArea(Point referencePoint)
        {
            var point = new POINT { X = (int)referencePoint.X, Y = (int)referencePoint.Y };
            var monitor = MonitorFromPoint(point, MONITOR_DEFAULTTONEAREST);

            var monitorInfo = new MONITORINFO();
            monitorInfo.cbSize = Marshal.SizeOf<MONITORINFO>();
            GetMonitorInfo(monitor, ref monitorInfo);

            return monitorInfo.rcWork;
        }

        public static Point AdjustToScreen(Point desiredTopLeft, Size windowSize, RECT workArea, double dpiScaleX, double dpiScaleY)
        {
            var workLeft = workArea.Left / dpiScaleX;
            var workTop = workArea.Top / dpiScaleY;
            var workRight = workArea.Right / dpiScaleX;
            var workBottom = workArea.Bottom / dpiScaleY;

            var left = desiredTopLeft.X;
            var top = desiredTopLeft.Y;

            if (left + windowSize.Width > workRight)
            {
                left = workRight - windowSize.Width;
            }

            if (top + windowSize.Height > workBottom)
            {
                top = workBottom - windowSize.Height;
            }

            if (left < workLeft)
            {
                left = workLeft;
            }

            if (top < workTop)
            {
                top = workTop;
            }

            return new Point(left, top);
        }

        public static Point GetCenteredPosition(Size windowSize, RECT workArea, double dpiScaleX, double dpiScaleY)
        {
            var startX = workArea.Left / dpiScaleX;
            var startY = workArea.Top / dpiScaleY;
            var width = (workArea.Right - workArea.Left) / dpiScaleX;
            var height = (workArea.Bottom - workArea.Top) / dpiScaleY;

            return new Point(
                startX + (width - windowSize.Width) / 2,
                startY + (height - windowSize.Height) / 2);
        }

        private static Point? GetCaretPositionFromGuiThreadInfo()
        {
            var foreground = GetForegroundWindow();
            if (foreground == IntPtr.Zero)
            {
                return null;
            }

            uint threadId = GetWindowThreadProcessId(foreground, IntPtr.Zero);
            var guiThreadInfo = new GUITHREADINFO
            {
                cbSize = (uint)Marshal.SizeOf<GUITHREADINFO>()
            };

            if (!GetGUIThreadInfo(threadId, ref guiThreadInfo) || guiThreadInfo.hwndCaret == IntPtr.Zero)
            {
                return null;
            }

            var caretPoint = new POINT
            {
                X = guiThreadInfo.rcCaret.Left,
                Y = guiThreadInfo.rcCaret.Bottom
            };

            if (!ClientToScreen(guiThreadInfo.hwndCaret, ref caretPoint))
            {
                return null;
            }

            return new Point(caretPoint.X, caretPoint.Y);
        }

        private static Point? GetCaretPositionFromUiAutomation()
        {
            try
            {
                var element = AutomationElement.FocusedElement;
                if (element == null)
                {
                    return null;
                }

                if (element.TryGetCurrentPattern(TextPattern.Pattern, out var patternObj) &&
                    patternObj is TextPattern textPattern)
                {
                    var caretPoint = GetCaretPointFromTextPattern(textPattern);
                    if (caretPoint != null)
                    {
                        return caretPoint;
                    }
                }
            }
            catch
            {
                // ignore and fall back to other strategies
            }

            return null;
        }

        private static Point? GetCaretPointFromTextPattern(TextPattern textPattern)
        {
            foreach (var range in textPattern.GetSelection() ?? Array.Empty<TextPatternRange>())
            {
                var point = GetCaretPointFromRange(range);
                if (point != null)
                {
                    return point;
                }
            }

            foreach (var range in textPattern.GetVisibleRanges() ?? Array.Empty<TextPatternRange>())
            {
                var point = GetCaretPointFromRange(range);
                if (point != null)
                {
                    return point;
                }
            }

            return GetCaretPointFromRange(textPattern.DocumentRange);
        }

        private static Point? GetCaretPointFromRange(TextPatternRange? range)
        {
            if (range == null)
            {
                return null;
            }

            var rects = range.GetBoundingRectangles();
            if (rects == null)
            {
                return null;
            }

            foreach (Rect rect in rects)
            {
                if (rect.Width <= 0 || rect.Height <= 0 ||
                    double.IsNaN(rect.X) || double.IsNaN(rect.Y) || double.IsNaN(rect.Width) || double.IsNaN(rect.Height) ||
                    double.IsInfinity(rect.X) || double.IsInfinity(rect.Y) || double.IsInfinity(rect.Width) || double.IsInfinity(rect.Height))
                {
                    continue;
                }

                return new Point(rect.X + (rect.Width / 2), rect.Y + rect.Height);
            }

            return null;
        }

        #region Native Structures

        [StructLayout(LayoutKind.Sequential)]
        private struct GUITHREADINFO
        {
            public uint cbSize;
            public uint flags;
            public IntPtr hwndActive;
            public IntPtr hwndFocus;
            public IntPtr hwndCapture;
            public IntPtr hwndMenuOwner;
            public IntPtr hwndMoveSize;
            public IntPtr hwndCaret;
            public RECT rcCaret;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        #endregion

        #region Native Methods

        private const int MONITOR_DEFAULTTONEAREST = 2;

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, int dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        #endregion
    }
}
