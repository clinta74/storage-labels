using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Models.DTO.EncryptionKey;

namespace StorageLabelsApi.Endpoints.EncryptionKeys;

internal partial class EncryptionKeyEndpoints
{
    private static async Task<Ok<List<EncryptionKeyResponse>>> GetEncryptionKeys([FromServices] StorageLabelsDbContext dbContext, CancellationToken cancellationToken)
    {
        var keys = await dbContext.EncryptionKeys
            .OrderByDescending(k => k.Version)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(keys.Select(k => new EncryptionKeyResponse(k)).ToList());
    }
}
