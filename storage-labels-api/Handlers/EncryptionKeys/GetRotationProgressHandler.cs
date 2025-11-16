using Mediator;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Handlers.EncryptionKeys;

public record GetRotationProgress(Guid RotationId) : IRequest<Result<RotationProgress>>;

public class GetRotationProgressHandler : IRequestHandler<GetRotationProgress, Result<RotationProgress>>
{
    private readonly IKeyRotationService _rotationService;

    public GetRotationProgressHandler(IKeyRotationService rotationService)
    {
        _rotationService = rotationService;
    }

    public async ValueTask<Result<RotationProgress>> Handle(
        GetRotationProgress request,
        CancellationToken cancellationToken)
    {
        var progress = await _rotationService.GetRotationProgressAsync(request.RotationId, cancellationToken);

        if (progress == null)
        {
            return Result.NotFound("Rotation not found");
        }

        return Result.Success(progress);
    }
}
