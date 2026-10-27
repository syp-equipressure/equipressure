using System.Security.Claims;
using EquiApi.Persistence.Model;
using EquiApi.Util;

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
        new(user.ExternalId, user.Email, user.FirstName, user.LastName);
}

/// <summary>
///     Request to set up the profile of the logged-in user after the first login.
///     The email is taken from the access token.
/// </summary>
/// <param name="FirstName">The first name of the person</param>
/// <param name="LastName">The last name of the person</param>
/// <param name="Height">The height of the person in cm</param>
/// <param name="Weight">The weight of the person in kg</param>
/// <param name="DateOfBirth">The birthdate of the person</param>
/// <param name="WebsiteLink">The optional website link, only for saddlers</param>
/// <param name="Description">The optional description, only for saddlers</param>
/// <param name="Address">The address, an existing address with the same values is reused</param>
/// <param name="Role">The role of the person</param>
public sealed record CreateAccountRequest(
    string FirstName,
    string LastName,
    decimal Height,
    decimal Weight,
    LocalDate DateOfBirth,
    string? WebsiteLink,
    string? Description,
    AddressRequest Address,
    RoleName Role)
{
    /// <summary>
    ///     Converts the request to a regular person request, so that the same validation rules apply
    /// </summary>
    public AddPersonRequest ToAddPersonRequest(string? email) =>
        new(FirstName, LastName, Height, Weight, DateOfBirth, email, WebsiteLink, Description, Address, Role);
}
