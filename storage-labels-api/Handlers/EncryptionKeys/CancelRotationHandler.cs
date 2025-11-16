using Mediator;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Handlers.EncryptionKeys;

public record CancelRotation(Guid RotationId, string UserId) : IRequest<Result<bool>>;

public class CancelRotationHandler : IRequestHandler<CancelRotation, Result<bool>>
{
    private readonly IKeyRotationService _rotationService;
    private readonly ILogger<CancelRotationHandler> _logger;

    public CancelRotationHandler(
        IKeyRotationService rotationService,
        ILogger<CancelRotationHandler> logger)
    {
        _rotationService = rotationService;
        _logger = logger;
    }

    public async ValueTask<Result<bool>> Handle(
        CancelRotation request,
        CancellationToken cancellationToken)
    {
        var success = await _rotationService.CancelRotationAsync(request.RotationId, cancellationToken);

        if (!success)
        {
            _logger.CancelNonExistentRotation(request.UserId, request.RotationId);
            return Result.NotFound("Rotation not found or cannot be cancelled");
        }

        _logger.RotationCancelled(request.UserId, request.RotationId);

        return Result.Success(true);
    }
}
