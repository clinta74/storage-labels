using Mediator;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Handlers.EncryptionKeys;

public record RetireEncryptionKey(int Kid, string UserId) : IRequest<Result<bool>>;

public class RetireEncryptionKeyHandler : IRequestHandler<RetireEncryptionKey, Result<bool>>
{
    private readonly IImageEncryptionService _encryptionService;
    private readonly ILogger<RetireEncryptionKeyHandler> _logger;

    public RetireEncryptionKeyHandler(
        IImageEncryptionService encryptionService,
        ILogger<RetireEncryptionKeyHandler> logger)
    {
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async ValueTask<Result<bool>> Handle(
        RetireEncryptionKey request,
        CancellationToken cancellationToken)
    {
        var success = await _encryptionService.RetireKeyAsync(request.Kid, cancellationToken);

        if (!success)
        {
            _logger.RetireNonExistentKey(request.UserId, request.Kid);
            return Result.NotFound("Encryption key not found");
        }

        _logger.EncryptionKeyRetired(request.UserId, request.Kid);
        return Result.Success(true);
    }
}

