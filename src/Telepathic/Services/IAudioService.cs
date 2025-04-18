namespace Telepathic.Services;

public interface IAudioService
{
    Task StartRecordingAsync(CancellationToken ct);
    Task StopRecordingAsync();
    string RecordedFilePath { get; }
}
