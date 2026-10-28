using EquiApi.Core.Services;
using EquiApi.Dtos;
using EquiApi.Util;
using Microsoft.AspNetCore.Mvc;

namespace EquiApi.Controllers;

[Route("api/account")]
public sealed class AccountController(IPersonService personService) : BaseController
{
    /// <summary>
    ///     Gets the identity of the logged-in user from the access token, e.g. to prefill the profile setup
    /// </summary>
    [HttpGet("identity")]
    [ProducesResponseType<IdentityDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<IdentityDto> GetIdentity() => Ok(IdentityDto.FromClaims(User));

    /// <summary>
    ///     Gets the person of the logged-in user, 404 means that the profile has not been set up yet
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType<PersonDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<PersonDetailDto>> GetMe()
    {
        var result = await personService.GetPersonByExternalIdAsync(User.ExternalId);

        return result.Match<ActionResult<PersonDetailDto>>(
            success => Ok(PersonDetailDto.FromPerson(success.Value)),
            _ => NotFound());
    }

    /// <summary>
    ///     Sets up the profile of the logged-in user after the first login, the role is assigned when claiming a device
    /// </summary>
    [HttpPost]
    [ProducesResponseType<PersonDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<ActionResult<PersonDetailDto>> CreateAccount([FromBody] CreateAccountRequest request)
    {
        if (!IsValid<CreateAccountRequest.Validator, CreateAccountRequest>(request))
        {
            return ValidationProblem();
        }

        var result = await personService.AddAccountAsync(User.ExternalId, request.ToData(User.Email));

        return result.Match<ActionResult<PersonDetailDto>>(
            success => CreatedAtAction(nameof(GetMe), PersonDetailDto.FromPerson(success.Value)),
            _ => Problem(statusCode: StatusCodes.Status409Conflict,
                         title: "The profile of this account has already been set up"),
            _ => Problem(statusCode: StatusCodes.Status409Conflict,
                         title: "The email is already used by another person"));
    }
}