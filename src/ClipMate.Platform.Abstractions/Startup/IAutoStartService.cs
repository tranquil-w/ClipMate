namespace ClipMate.Platform.Abstractions.Startup;

public interface IAutoStartService
{
    bool IsAutoStartEnabled();

    void SetAutoStart(bool enabled);

    AutoStartMethod GetCurrentMethod();
}

