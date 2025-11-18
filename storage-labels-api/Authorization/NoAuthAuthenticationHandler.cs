using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using StorageLabelsApi.Models;

namespace StorageLabelsApi.Authorization;

/// <summary>
/// Authentication handler for No-Auth mode (trusted networks)
/// Automatically authenticates all requests with admin privileges
/// </summary>
public class NoAuthAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public NoAuthAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) 
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "anonymous"),
            new Claim(ClaimTypes.Name, "anonymous"),
            new Claim(ClaimTypes.Email, "anonymous@localhost"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("permission", "read:all"),
            new Claim("permission", "write:all"),
            new Claim("permission", "delete:all"),
        };

        // Add all permissions
        foreach (var permission in Policies.Permissions)
        {
            claims.Add(new Claim("permission", permission));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
