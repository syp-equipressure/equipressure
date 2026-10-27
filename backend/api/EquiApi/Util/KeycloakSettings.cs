namespace EquiApi.Util;

public sealed class KeycloakSettings
{
    public const string SectionKey = "Keycloak";

    /// <summary>
    ///     URL of the Keycloak realm, used to load the signing keys and to validate the token issuer
    /// </summary>
    public required string Authority { get; init; }

    /// <summary>
    ///     Audience every access token has to contain (added by the audience mapper of the Keycloak clients)
    /// </summary>
    public required string Audience { get; init; }

    /// <summary>
    ///     Has to be true in production, may only be disabled for the local Keycloak (plain HTTP)
    /// </summary>
    public bool RequireHttpsMetadata { get; init; } = true;
}
