using Mediator;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Handlers.EncryptionKeys;

public record ActivateEncryptionKey(
    int Kid, 
    string UserId, 
    bool AutoRotate = true
) : IRequest<Result<ActivateEncryptionKeyResult>>;

public record ActivateEncryptionKeyResult(
    bool Success,
    Guid? RotationId = null
);

public class ActivateEncryptionKeyHandler : IRequestHandler<ActivateEncryptionKey, Result<ActivateEncryptionKeyResult>>
{
    private readonly IImageEncryptionService _encryptionService;
    private readonly IKeyRotationService _rotationService;
    private readonly StorageLabelsDbContext _context;
    private readonly ILogger<ActivateEncryptionKeyHandler> _logger;

    public ActivateEncryptionKeyHandler(
        IImageEncryptionService encryptionService,
        IKeyRotationService rotationService,
        StorageLabelsDbContext context,
        ILogger<ActivateEncryptionKeyHandler> logger)
    {
        _encryptionService = encryptionService;
        _rotationService = rotationService;
        _context = context;
        _logger = logger;
    }

    public async ValueTask<Result<ActivateEncryptionKeyResult>> Handle(
        ActivateEncryptionKey request,
        CancellationToken cancellationToken)
    {
        // Get the currently active key before activation
        var previousActiveKey = await _encryptionService.GetActiveKeyAsync(cancellationToken);

        // Activate the new key
        var success = await _encryptionService.ActivateKeyAsync(request.Kid, cancellationToken);

        if (!success)
        {
            _logger.ActivateNonExistentKey(request.UserId, request.Kid);
            return Result.NotFound("Encryption key not found");
        }

        _logger.EncryptionKeyActivated(request.UserId, request.Kid);

        Guid? rotationId = null;

        // If auto-rotate is enabled and there was a previous active key, start rotation
        if (request.AutoRotate && previousActiveKey != null && previousActiveKey.Kid != request.Kid)
        {
            // Check if there are any images to rotate
            var hasImagesToRotate = await _context.Images
                .AnyAsync(img => img.IsEncrypted && img.EncryptionKeyId == previousActiveKey.Kid, cancellationToken);

            if (hasImagesToRotate)
            {
                try
                {
                    var rotation = await _rotationService.StartRotationAsync(
                        new RotationOptions(
                            FromKeyId: previousActiveKey.Kid,
                            ToKeyId: request.Kid,
                            BatchSize: 100,
                            InitiatedBy: request.UserId,
                            IsAutomatic: true
                        ),
                        cancellationToken);

                    rotationId = rotation.Id;

                    _logger.AutoRotationStarted(rotation.Id, previousActiveKey.Kid, request.Kid);
                }
                catch (Exception ex)
                {
                    _logger.AutoRotationFailed(ex);
                    // Don't fail the activation if rotation fails to start
                }
            }
        }

        return Result.Success(new ActivateEncryptionKeyResult(true, rotationId));
    }
}

