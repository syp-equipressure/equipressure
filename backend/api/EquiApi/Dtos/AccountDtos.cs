using System.Security.Claims;
using EquiApi.Core.Services;
using EquiApi.Shared;
using EquiApi.Util;
using FluentValidation;

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
///     The email is taken from the access token, the role is assigned when claiming a device.
/// </summary>
/// <param name="FirstName">The first name of the person</param>
/// <param name="LastName">The last name of the person</param>
/// <param name="Height">The height of the person in cm</param>
/// <param name="Weight">The weight of the person in kg</param>
/// <param name="DateOfBirth">The birthdate of the person</param>
/// <param name="Address">The address, an existing address with the same values is reused</param>
public sealed record CreateAccountRequest(
    string FirstName,
    string LastName,
    decimal Height,
    decimal Weight,
    LocalDate DateOfBirth,
    AddressRequest Address)
{
    public NewAccountData ToData(string? email) =>
        new(FirstName, LastName, Height, Weight, DateOfBirth, email, Address.ToData());

    public sealed class Validator : AbstractValidator<CreateAccountRequest>
    {
        public Validator(IClock clock)
        {
            RuleFor(x => x.FirstName).NotEmpty();
            RuleFor(x => x.LastName).NotEmpty();
            RuleFor(x => x.Height).GreaterThan(0);
            RuleFor(x => x.Weight).GreaterThan(0);
            RuleFor(x => x.DateOfBirth).LessThan(clock.LocalDate);
            RuleFor(x => x.Address).NotNull().SetValidator(new AddressRequest.Validator());
        }
    }
}
