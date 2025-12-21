namespace ClipMate.Platform.Abstractions.Startup;

public enum AutoStartMethod
{
    None,
    Registry,
    TaskScheduler,
    LaunchAgent,
    DesktopAutostart
}

