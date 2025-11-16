using Mediator;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Handlers.EncryptionKeys;

public record GetRotations(RotationStatus? Status = null) : IRequest<Result<List<EncryptionKeyRotation>>>;

public class GetRotationsHandler : IRequestHandler<GetRotations, Result<List<EncryptionKeyRotation>>>
{
    private readonly IKeyRotationService _rotationService;

    public GetRotationsHandler(IKeyRotationService rotationService)
    {
        _rotationService = rotationService;
    }

    public async ValueTask<Result<List<EncryptionKeyRotation>>> Handle(
        GetRotations request,
        CancellationToken cancellationToken)
    {
        var rotations = await _rotationService.GetRotationsAsync(request.Status, cancellationToken);
        return Result.Success(rotations);
    }
}
