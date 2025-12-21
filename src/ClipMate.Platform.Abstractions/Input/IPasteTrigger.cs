namespace ClipMate.Platform.Abstractions.Input;

public interface IPasteTrigger
{
    Task TriggerPasteAsync(CancellationToken cancellationToken = default);
}

