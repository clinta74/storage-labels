using Mediator;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Models.DTO.EncryptionKey;

namespace StorageLabelsApi.Handlers.EncryptionKeys;

public record GetEncryptionKeys() : IRequest<Result<List<EncryptionKeyResponse>>>;

public class GetEncryptionKeysHandler : IRequestHandler<GetEncryptionKeys, Result<List<EncryptionKeyResponse>>>
{
    private readonly StorageLabelsDbContext _context;

    public GetEncryptionKeysHandler(StorageLabelsDbContext context)
    {
        _context = context;
    }

    public async ValueTask<Result<List<EncryptionKeyResponse>>> Handle(
        GetEncryptionKeys request,
        CancellationToken cancellationToken)
    {
        var keys = await _context.EncryptionKeys
            .OrderByDescending(k => k.Version)
            .ToListAsync(cancellationToken);

        var dtos = keys.Select(k => new EncryptionKeyResponse(k)).ToList();

        return Result.Success(dtos);
    }
}
