using System.Security.Claims;
using System.Text.Encodings.Web;
using EquiApi.Util;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace EquiApi.TestInt.Util;

/// <summary>
///     Replaces the Keycloak token validation in tests: a request is authenticated as the user given in the
///     <see cref="UserHeader" /> header (sub claim), the optional <see cref="EmailHeader" /> becomes the email claim
///     and the optional <see cref="RoleHeader" /> a realm role
/// </summary>
internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string UserHeader = "X-Test-User";
    public const string EmailHeader = "X-Test-Email";
    public const string RoleHeader = "X-Test-Role";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserHeader, out var externalId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        List<Claim> claims = [new("sub", externalId.ToString())];
        if (Request.Headers.TryGetValue(EmailHeader, out var email))
        {
            claims.Add(new Claim("email", email.ToString()));
        }

        if (Request.Headers.TryGetValue(RoleHeader, out var role))
        {
            claims.Add(new Claim(ClaimsPrincipalExtensions.RoleClaimType, role.ToString()));
        }

        var identity = new ClaimsIdentity(claims, SchemeName, "sub", ClaimsPrincipalExtensions.RoleClaimType);
        var principal = new ClaimsPrincipal(identity);

        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}