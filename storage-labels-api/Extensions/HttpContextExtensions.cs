using Microsoft.AspNetCore.Http;

namespace StorageLabelsApi.Extensions;

public static class HttpContextExtensions
{
    public static string? TryGetUserId(this HttpContext context) => context?.User?.Identity?.Name;
    public static string GetUserId(this HttpContext context) => context.TryGetUserId() ?? throw new ArgumentNullException("UserId is null in HttpContext.");
}