using Mediator;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Handlers.EncryptionKeys;

public record StartKeyRotation(
    int FromKeyId,
    int ToKeyId,
    int BatchSize,
    string UserId
) : IRequest<Result<Guid>>;

public class StartKeyRotationHandler : IRequestHandler<StartKeyRotation, Result<Guid>>
{
    private readonly IKeyRotationService _rotationService;
    private readonly ILogger<StartKeyRotationHandler> _logger;

    public StartKeyRotationHandler(
        IKeyRotationService rotationService,
        ILogger<StartKeyRotationHandler> logger)
    {
        _rotationService = rotationService;
        _logger = logger;
    }

    public async ValueTask<Result<Guid>> Handle(
        StartKeyRotation request,
        CancellationToken cancellationToken)
    {
        try
        {
            var rotation = await _rotationService.StartRotationAsync(
                new RotationOptions(
                    FromKeyId: request.FromKeyId,
                    ToKeyId: request.ToKeyId,
                    BatchSize: request.BatchSize,
                    InitiatedBy: request.UserId,
                    IsAutomatic: false
                ),
                cancellationToken);

            _logger.ManualRotationStarted(request.UserId, rotation.Id, request.FromKeyId, request.ToKeyId);

            return Result.Success(rotation.Id);
        }
        catch (InvalidOperationException ex)
        {
            _logger.KeyRotationStartFailed(ex);
            return Result.Error(ex.Message);
        }
    }
}
