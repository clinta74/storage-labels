using System.Text;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;

namespace StorageLabelsApi.Handlers.Users;

public record ExportUserDataRequest(string UserId, string ExportType) : IRequest<Result<byte[]>>;

public class ExportUserDataHandler(StorageLabelsDbContext dbContext) : IRequestHandler<ExportUserDataRequest, Result<byte[]>>
{
    public async ValueTask<Result<byte[]>> Handle(ExportUserDataRequest request, CancellationToken cancellationToken)
    {
        var exportType = request.ExportType.ToLowerInvariant();

        return exportType switch
        {
            "locations" => await ExportLocations(request.UserId, cancellationToken),
            "boxes" => await ExportBoxes(request.UserId, cancellationToken),
            "items" => await ExportItems(request.UserId, cancellationToken),
            _ => Result.Error($"Invalid export type: {request.ExportType}. Valid types are: locations, boxes, items")
        };
    }

    private async Task<Result<byte[]>> ExportLocations(string userId, CancellationToken cancellationToken)
    {
        var locations = await dbContext.UserLocations
            .Where(ul => ul.UserId == userId)
            .Include(ul => ul.Location)
            .Select(ul => new
            {
                LocationId = ul.Location.LocationId,
                Name = ul.Location.Name,
                ul.Location.Created,
                ul.Location.Updated,
                BoxCount = ul.Location.Boxes.Count,
                ItemCount = ul.Location.Boxes.SelectMany(b => b.Items).Count()
            })
            .OrderBy(l => l.LocationId)
            .ToListAsync(cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("LocationId,Name,Created,Updated,BoxCount,ItemCount");
        
        foreach (var location in locations)
        {
            csv.AppendLine($"{location.LocationId}," +
                          $"\"{EscapeCsv(location.Name)}\"," +
                          $"{location.Created:yyyy-MM-dd HH:mm:ss}," +
                          $"{location.Updated:yyyy-MM-dd HH:mm:ss}," +
                          $"{location.BoxCount}," +
                          $"{location.ItemCount}");
        }

        return Result.Success(Encoding.UTF8.GetBytes(csv.ToString()));
    }

    private async Task<Result<byte[]>> ExportBoxes(string userId, CancellationToken cancellationToken)
    {
        var boxes = await dbContext.UserLocations
            .Where(ul => ul.UserId == userId)
            .SelectMany(ul => ul.Location.Boxes)
            .Include(b => b.Location)
            .Select(b => new
            {
                b.BoxId,
                b.Code,
                b.Name,
                b.Description,
                b.ImageUrl,
                LocationId = b.Location.LocationId,
                LocationName = b.Location.Name,
                b.Created,
                b.Updated,
                b.LastAccessed,
                ItemCount = b.Items.Count
            })
            .OrderBy(b => b.Code)
            .ToListAsync(cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("BoxId,Code,Name,Description,ImageUrl,LocationId,LocationName,Created,Updated,LastAccessed,ItemCount");
        
        foreach (var box in boxes)
        {
            csv.AppendLine($"{box.BoxId}," +
                          $"\"{EscapeCsv(box.Code)}\"," +
                          $"\"{EscapeCsv(box.Name)}\"," +
                          $"\"{EscapeCsv(box.Description ?? string.Empty)}\"," +
                          $"\"{EscapeCsv(box.ImageUrl ?? string.Empty)}\"," +
                          $"{box.LocationId}," +
                          $"\"{EscapeCsv(box.LocationName)}\"," +
                          $"{box.Created:yyyy-MM-dd HH:mm:ss}," +
                          $"{box.Updated:yyyy-MM-dd HH:mm:ss}," +
                          $"{box.LastAccessed:yyyy-MM-dd HH:mm:ss}," +
                          $"{box.ItemCount}");
        }

        return Result.Success(Encoding.UTF8.GetBytes(csv.ToString()));
    }

    private async Task<Result<byte[]>> ExportItems(string userId, CancellationToken cancellationToken)
    {
        var items = await dbContext.UserLocations
            .Where(ul => ul.UserId == userId)
            .SelectMany(ul => ul.Location.Boxes)
            .SelectMany(b => b.Items)
            .Include(i => i.Box)
                .ThenInclude(b => b.Location)
            .Select(i => new
            {
                i.ItemId,
                i.Name,
                i.Description,
                i.ImageUrl,
                BoxId = i.Box.BoxId,
                BoxCode = i.Box.Code,
                BoxName = i.Box.Name,
                LocationId = i.Box.Location.LocationId,
                LocationName = i.Box.Location.Name,
                i.Created,
                i.Updated
            })
            .OrderBy(i => i.BoxCode)
                .ThenBy(i => i.Name)
            .ToListAsync(cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("ItemId,Name,Description,ImageUrl,BoxId,BoxCode,BoxName,LocationId,LocationName,Created,Updated");
        
        foreach (var item in items)
        {
            csv.AppendLine($"{item.ItemId}," +
                          $"\"{EscapeCsv(item.Name)}\"," +
                          $"\"{EscapeCsv(item.Description ?? string.Empty)}\"," +
                          $"\"{EscapeCsv(item.ImageUrl ?? string.Empty)}\"," +
                          $"{item.BoxId}," +
                          $"\"{EscapeCsv(item.BoxCode)}\"," +
                          $"\"{EscapeCsv(item.BoxName)}\"," +
                          $"{item.LocationId}," +
                          $"\"{EscapeCsv(item.LocationName)}\"," +
                          $"{item.Created:yyyy-MM-dd HH:mm:ss}," +
                          $"{item.Updated:yyyy-MM-dd HH:mm:ss}");
        }

        return Result.Success(Encoding.UTF8.GetBytes(csv.ToString()));
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // Escape double quotes by doubling them
        return value.Replace("\"", "\"\"");
    }
}
