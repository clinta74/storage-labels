using Microsoft.AspNetCore.Authorization;

namespace StorageLabelsApi.Authorization;

public class HasScopeHandler(ILogger<HasScopeHandler> logger) : AuthorizationHandler<HasScopeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HasScopeRequirement requirement)
    {
        try
        {
            // Check for permission claim (local auth uses "permission" claims)
            if (context.User.HasClaim(c => c.Type == "permission" && c.Value == requirement.Scope))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Check for Auth0-style scope claim (for backward compatibility)
            if (context.User.HasClaim(c => c.Value == requirement.Scope && c.Issuer == requirement.Issuer))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Check for space-separated permissions claim (for backward compatibility)
            var permissionsClaim = context.User.FindFirst("permissions");
            if (permissionsClaim != null)
            {
                var permissions = permissionsClaim.Value.Split(' ');
                if (permissions.Contains(requirement.Scope))
                {
                    context.Succeed(requirement);
                    return Task.CompletedTask;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking authorization requirement");
        }
        return Task.CompletedTask;
    }
}