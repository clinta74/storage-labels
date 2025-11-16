namespace StorageLabelsApi.Services;

public interface IRotationProgressNotifier
{
    Task StreamProgressAsync(Guid rotationId, Stream responseStream, CancellationToken cancellationToken);
    Task NotifyProgressAsync(Guid rotationId, RotationProgress progress);
}
