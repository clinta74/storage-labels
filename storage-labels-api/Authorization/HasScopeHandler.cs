using Microsoft.AspNetCore.Authorization;

namespace StorageLabelsApi.Authorization;

public class HasScopeHandler : AuthorizationHandler<HasScopeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HasScopeRequirement requirement)
    {
        // If user does not have the scope claim, get out of here
        if (context.User.HasClaim(c => c.Value == requirement.Scope && c.Issuer == requirement.Issuer))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}