using Mediator;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Models.DTO.EncryptionKey;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Handlers.EncryptionKeys;

public record CreateEncryptionKey(
    string? Description,
    string UserId
) : IRequest<Result<EncryptionKeyResponse>>;

public class CreateEncryptionKeyHandler : IRequestHandler<CreateEncryptionKey, Result<EncryptionKeyResponse>>
{
    private readonly IImageEncryptionService _encryptionService;
    private readonly ILogger<CreateEncryptionKeyHandler> _logger;

    public CreateEncryptionKeyHandler(
        IImageEncryptionService encryptionService,
        ILogger<CreateEncryptionKeyHandler> logger)
    {
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async ValueTask<Result<EncryptionKeyResponse>> Handle(
        CreateEncryptionKey request,
        CancellationToken cancellationToken)
    {
        try
        {
            var key = await _encryptionService.CreateKeyAsync(
                request.Description,
                request.UserId,
                cancellationToken);

            _logger.EncryptionKeyCreated(request.UserId, key.Kid, key.Version);

            return Result.Success(EncryptionKeyResponse.FromEntity(key));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Failed to create encryption key: {Description}", request.Description ?? "unnamed");
            return Result.Error(ex.Message);
        }
    }
}
