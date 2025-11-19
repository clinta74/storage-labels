using System.Security.Claims;

namespace StorageLabelsApi.Extensions;

public static class HttpContextExtensions
{
    public static string? TryGetUserId(this HttpContext context) => 
        context?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    
    public static string GetUserId(this HttpContext context) => 
        context.TryGetUserId() ?? throw new ArgumentNullException("UserId is null in HttpContext.");
}