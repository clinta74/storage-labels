using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.CommonLocations;

public record GetCommonLocation() : IStreamRequest<CommonLocation>;
public class GetCommonLocationsHandler(StorageLabelsDbContext dbContext) : IStreamRequestHandler<GetCommonLocation, CommonLocation>
{
    public IAsyncEnumerable<CommonLocation> Handle(GetCommonLocation request, CancellationToken cancellationToken)
    {
        return dbContext.CommonLocations
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .AsAsyncEnumerable();
    }
}