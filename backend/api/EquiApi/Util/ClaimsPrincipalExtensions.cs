using System.Security.Claims;

namespace EquiApi.Util;

/// <summary>
///     Access to the claims of the Keycloak access token of the logged-in user
/// </summary>
public static class ClaimsPrincipalExtensions
{
    extension(ClaimsPrincipal user)
    {
        /// <summary>
        ///     The unique, stable id of the user in Keycloak (sub claim)
        /// </summary>
        public string ExternalId =>
            user.FindFirstValue("sub") ?? throw new InvalidOperationException("Access token has no sub claim");

        /// <summary>
        ///     The email of the user, if provided by the identity provider
        /// </summary>
        public string? Email => user.FindFirstValue("email");

        /// <summary>
        ///     The first name of the user, if provided by the identity provider
        /// </summary>
        public string? FirstName => user.FindFirstValue("given_name");

        /// <summary>
        ///     The last name of the user, if provided by the identity provider
        /// </summary>
        public string? LastName => user.FindFirstValue("family_name");
    }
}
