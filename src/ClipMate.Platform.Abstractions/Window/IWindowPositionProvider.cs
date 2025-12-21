namespace ClipMate.Platform.Abstractions.Window;

public interface IWindowPositionProvider
{
    ScreenPoint? GetCaretPosition();

    ScreenPoint GetMousePosition();

    ScreenRect GetWorkArea(ScreenPoint referencePoint);

    DpiScale GetDpiScale(ScreenPoint referencePoint);
}

