using Mediator;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Handlers.EncryptionKeys;

public record GetEncryptionKeyStats(int Kid) : IRequest<Result<EncryptionKeyStats>>;

public class GetEncryptionKeyStatsHandler : IRequestHandler<GetEncryptionKeyStats, Result<EncryptionKeyStats>>
{
    private readonly IImageEncryptionService _encryptionService;
    private readonly ILogger<GetEncryptionKeyStatsHandler> _logger;

    public GetEncryptionKeyStatsHandler(
        IImageEncryptionService encryptionService,
        ILogger<GetEncryptionKeyStatsHandler> logger)
    {
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async ValueTask<Result<EncryptionKeyStats>> Handle(
        GetEncryptionKeyStats request,
        CancellationToken cancellationToken)
    {
        try
        {
            var stats = await _encryptionService.GetKeyStatsAsync(request.Kid, cancellationToken);
            return Result.Success(stats);
        }
        catch (InvalidOperationException)
        {
            _logger.EncryptionKeyStatsNotFound(request.Kid);
            return Result.NotFound("Encryption key not found");
        }
    }
}

