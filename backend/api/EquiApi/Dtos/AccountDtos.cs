using System.Security.Claims;

namespace EquiApi.Dtos;

/// <summary>
///     The identity of the logged-in user, as provided by Keycloak in the access token
/// </summary>
/// <param name="Subject">The unique, stable id of the user in Keycloak</param>
/// <param name="Email">The email of the user, if provided by the identity provider</param>
/// <param name="FirstName">The first name of the user, if provided by the identity provider</param>
/// <param name="LastName">The last name of the user, if provided by the identity provider</param>
public sealed record IdentityDto(string Subject, string? Email, string? FirstName, string? LastName)
{
    public static IdentityDto FromClaims(ClaimsPrincipal user) =>
        new(user.FindFirstValue("sub") ?? throw new InvalidOperationException("Access token has no sub claim"),
            user.FindFirstValue("email"),
            user.FindFirstValue("given_name"),
            user.FindFirstValue("family_name"));
}
