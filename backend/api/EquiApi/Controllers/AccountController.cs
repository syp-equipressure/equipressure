using EquiApi.Dtos;
using EquiApi.Util;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquiApi.Controllers;

[Route("api/account")]
[Authorize]
public sealed class AccountController : BaseController
{
    /// <summary>
    ///     Gets the identity of the logged-in user from the access token, e.g. to prefill the profile setup
    /// </summary>
    [HttpGet("identity")]
    [ProducesResponseType<IdentityDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<IdentityDto> GetIdentity() => Ok(IdentityDto.FromClaims(User));
}
