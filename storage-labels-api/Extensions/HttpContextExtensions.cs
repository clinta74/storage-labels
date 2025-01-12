using Microsoft.AspNetCore.Http;

namespace StorageLabelsApi.Extensions;

public static class HttpContextExtensions
{
    public static string? GetUserId(this HttpContext context) => context?.User?.Identity?.Name;
}