using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.CommonLocations;

public record CreateCommonLocation(string Name) : IRequest<Result<CommonLocation>>;

public class CreateCommonLocationHandler(StorageLabelsDbContext dbContext) : IRequestHandler<CreateCommonLocation, Result<CommonLocation>>
{
    public async ValueTask<Result<CommonLocation>> Handle(CreateCommonLocation request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Name))
        {
            return Result.Invalid(new ValidationError(nameof(CommonLocation), "Name is required."));
        }

        var commonLocation = dbContext.CommonLocations
            .Add(
                new (
                    CommonLocationId: 0,
                    Name: request.Name
                )
            );
        
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(commonLocation.Entity);
    }
}
