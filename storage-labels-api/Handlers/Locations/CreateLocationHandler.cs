using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Locations;

public record CreateLocation(string UserId, string Name) : IRequest<Result<LocationWithAccess>>;
public class CreateLocationHandler(StorageLabelsDbContext dbContext, TimeProvider timeProvider) : IRequestHandler<CreateLocation, Result<LocationWithAccess>>
{
    public async Task<Result<LocationWithAccess>> Handle(CreateLocation request, CancellationToken cancellationToken)
    {
        var dateTime = timeProvider.GetUtcNow();

        using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var location = dbContext.Locations
            .Add(new(
                LocationId: 0,
                Name: request.Name,
                Created: dateTime,
                Updated: dateTime
            )
        );
        await dbContext.SaveChangesAsync(cancellationToken);

        var locationId = location.Entity.LocationId;
        var defaultAccessLevel = AccessLevels.Owner;
        
        dbContext.UserLocations
            .Add(new(
                UserId: request.UserId,
                LocationId: locationId,
                AccessLevel: defaultAccessLevel,
                Created: dateTime,
                Updated: dateTime)
            );

        await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Result.Created(new LocationWithAccess(location.Entity, defaultAccessLevel));
    }
}