using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using OneOf;
using OneOf.Types;

namespace EquiApi.Util;

/// <summary>
///     The values of a new user in Keycloak, the email is also the username
/// </summary>
public sealed record NewKeycloakUser(string Email, string Password, string FirstName, string LastName);

public interface IKeycloakAdminClient
{
    /// <summary>
    ///     Creates a user who can log in right away
    /// </summary>
    /// <param name="user">The values of the user</param>
    /// <param name="cancellationToken">Aborts the request</param>
    /// <returns>
    ///     <see cref="Success" />, <see cref="EmailAlreadyUsed" /> if there is a user with this email or
    ///     <see cref="PasswordRejected" /> if the password does not meet the password policy of the realm
    /// </returns>
    public ValueTask<OneOf<Success, EmailAlreadyUsed, PasswordRejected>> CreateUserAsync(
        NewKeycloakUser user, CancellationToken cancellationToken);

    public readonly record struct EmailAlreadyUsed;

    public readonly record struct PasswordRejected;
}

/// <summary>
///     Calls the admin API of Keycloak with the service account of the client equi-api-admin,
///     which may only manage users
/// </summary>
internal sealed class KeycloakAdminClient(
    HttpClient httpClient,
    KeycloakSettings settings,
    ILogger<KeycloakAdminClient> logger) : IKeycloakAdminClient
{
    private string TokenUrl => $"{settings.Authority}/protocol/openid-connect/token";
    private string UsersUrl => $"{settings.Authority.Replace("/realms/", "/admin/realms/")}/users";

    public async ValueTask<OneOf<Success, IKeycloakAdminClient.EmailAlreadyUsed, IKeycloakAdminClient.PasswordRejected>>
        CreateUserAsync(NewKeycloakUser user, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, UsersUrl);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync(cancellationToken));
        request.Content = JsonContent.Create(new
        {
            username = user.Email,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            enabled = true,
            credentials = new[] { new { type = "password", value = user.Password, temporary = false } }
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        switch (response.StatusCode)
        {
            case HttpStatusCode.Created:
                logger.LogInformation("Created a new user in Keycloak");

                return new Success();
            case HttpStatusCode.Conflict:
                return new IKeycloakAdminClient.EmailAlreadyUsed();
            case HttpStatusCode.BadRequest:
                return new IKeycloakAdminClient.PasswordRejected();
            default:
                response.EnsureSuccessStatusCode();
                throw new HttpRequestException($"Unexpected response {response.StatusCode} from Keycloak");
        }
    }

    private async ValueTask<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = settings.AdminClientId,
            ["client_secret"] = settings.AdminClientSecret
                                ?? throw new InvalidOperationException("Keycloak:AdminClientSecret is not configured")
        });
        using var response = await httpClient.PostAsync(TokenUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);

        return token?.AccessToken ?? throw new InvalidOperationException("Keycloak returned no access token");
    }

    private sealed record TokenResponse([property: JsonPropertyName("access_token")] string AccessToken);
}