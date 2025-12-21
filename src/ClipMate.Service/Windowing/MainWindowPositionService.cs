using ClipMate.Core.Models;
using ClipMate.Platform.Abstractions.Window;
using ClipMate.Service.Interfaces;

namespace ClipMate.Service.Windowing;

public sealed class MainWindowPositionService : IMainWindowPositionService
{
    private const double DefaultWindowWidth = 360;
    private const double DefaultWindowHeight = 500;

    private readonly ISettingsService _settingsService;
    private readonly IWindowPositionProvider _windowPositionProvider;
    private readonly IMainWindowController _mainWindowController;

    public MainWindowPositionService(
        ISettingsService settingsService,
        IWindowPositionProvider windowPositionProvider,
        IMainWindowController mainWindowController)
    {
        _settingsService = settingsService;
        _windowPositionProvider = windowPositionProvider;
        _mainWindowController = mainWindowController;
    }

    public void PositionMainWindow()
    {
        var mode = _settingsService.GetWindowPosition();
        var referencePoint = _windowPositionProvider.GetMousePosition();

        ScreenPoint? caret = null;
        if (mode == WindowPosition.FollowCaret)
        {
            caret = _windowPositionProvider.GetCaretPosition();
            if (caret != null)
            {
                referencePoint = caret.Value;
            }
        }

        var dpi = _windowPositionProvider.GetDpiScale(referencePoint);
        if (dpi.X <= 0 || dpi.Y <= 0)
        {
            dpi = new DpiScale(1.0, 1.0);
        }

        var workArea = _windowPositionProvider.GetWorkArea(referencePoint);

        var position = mode switch
        {
            WindowPosition.FollowCaret => (caret != null
                ? CalculateFromAnchor(caret.Value, workArea, dpi, DefaultWindowWidth, DefaultWindowHeight, horizontalOffsetDip: 20, verticalOffsetDip: -10)
                : CalculateFromAnchor(referencePoint, workArea, dpi, DefaultWindowWidth, DefaultWindowHeight, horizontalOffsetDip: 20, verticalOffsetDip: -10)),
            WindowPosition.FollowMouse => CalculateFromAnchor(referencePoint, workArea, dpi, DefaultWindowWidth, DefaultWindowHeight, horizontalOffsetDip: 20, verticalOffsetDip: -10),
            WindowPosition.ScreenCenter => GetScreenCenter(workArea, dpi, DefaultWindowWidth, DefaultWindowHeight),
            _ => GetScreenCenter(workArea, dpi, DefaultWindowWidth, DefaultWindowHeight)
        };

        _mainWindowController.SetPosition(position);
    }

    private static ScreenPoint GetScreenCenter(ScreenRect workArea, DpiScale dpi, double windowWidthDip, double windowHeightDip)
    {
        var workLeftDip = workArea.Left / dpi.X;
        var workTopDip = workArea.Top / dpi.Y;
        var workWidthDip = workArea.Width / dpi.X;
        var workHeightDip = workArea.Height / dpi.Y;

        var leftDip = workLeftDip + ((workWidthDip - windowWidthDip) / 2);
        var topDip = workTopDip + ((workHeightDip - windowHeightDip) / 2);

        (leftDip, topDip) = AdjustToWorkArea(leftDip, topDip, windowWidthDip, windowHeightDip, workArea, dpi);

        return new ScreenPoint(
            (int)Math.Round(leftDip * dpi.X),
            (int)Math.Round(topDip * dpi.Y));
    }

    private static ScreenPoint CalculateFromAnchor(
        ScreenPoint anchor,
        ScreenRect workArea,
        DpiScale dpi,
        double windowWidthDip,
        double windowHeightDip,
        double horizontalOffsetDip,
        double verticalOffsetDip)
    {
        var anchorXDip = anchor.X / dpi.X;
        var anchorYDip = anchor.Y / dpi.Y;

        var leftDip = anchorXDip - horizontalOffsetDip;
        var topDip = anchorYDip - verticalOffsetDip;

        (leftDip, topDip) = AdjustToWorkArea(leftDip, topDip, windowWidthDip, windowHeightDip, workArea, dpi);

        return new ScreenPoint(
            (int)Math.Round(leftDip * dpi.X),
            (int)Math.Round(topDip * dpi.Y));
    }

    private static (double leftDip, double topDip) AdjustToWorkArea(
        double desiredLeftDip,
        double desiredTopDip,
        double windowWidthDip,
        double windowHeightDip,
        ScreenRect workArea,
        DpiScale dpi)
    {
        var workLeftDip = workArea.Left / dpi.X;
        var workTopDip = workArea.Top / dpi.Y;
        var workRightDip = workArea.Right / dpi.X;
        var workBottomDip = workArea.Bottom / dpi.Y;

        var leftDip = desiredLeftDip;
        var topDip = desiredTopDip;

        if (leftDip + windowWidthDip > workRightDip)
        {
            leftDip = workRightDip - windowWidthDip;
        }

        if (topDip + windowHeightDip > workBottomDip)
        {
            topDip = workBottomDip - windowHeightDip;
        }

        if (leftDip < workLeftDip)
        {
            leftDip = workLeftDip;
        }

        if (topDip < workTopDip)
        {
            topDip = workTopDip;
        }

        return (leftDip, topDip);
    }
}

