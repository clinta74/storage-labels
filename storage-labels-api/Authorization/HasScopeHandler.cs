using Microsoft.AspNetCore.Authorization;

namespace StorageLabelsApi.Authorization;

public class HasScopeHandler(ILogger<HasScopeHandler> logger) : AuthorizationHandler<HasScopeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HasScopeRequirement requirement)
    {
        try
        {
            // If user does not have the scope claim, get out of here
            if (context.User.HasClaim(c => c.Value == requirement.Scope && c.Issuer == requirement.Issuer))
            {
                context.Succeed(requirement);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }
        return Task.CompletedTask;
    }
}