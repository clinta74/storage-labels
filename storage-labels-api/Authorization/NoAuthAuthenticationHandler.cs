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
        const string AnonymousUserId = "00000000-0000-0000-0000-000000000001";

        Claim[] claims = 
        [
            new Claim(ClaimTypes.NameIdentifier, AnonymousUserId),
            new Claim(ClaimTypes.Name, "anonymous"),
            new Claim(ClaimTypes.Email, "anonymous@localhost"),
            new Claim(ClaimTypes.Role, "Admin"),
            ..Policies.AllPermissions.Select(permission => new Claim("permission", permission))
        ];

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
