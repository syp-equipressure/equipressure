using EquiApi.Core.Services;
using EquiApi.Dtos;
using EquiApi.Util;
using Microsoft.AspNetCore.Mvc;

namespace EquiApi.Controllers;

[Route("api/devices")]
public sealed class DeviceController(IDeviceService deviceService) : BaseController
{
    /// <summary>
    ///     Gets a device
    /// </summary>
    [HttpGet("{deviceId}")]
    [ProducesResponseType<DeviceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<DeviceDto>> GetDeviceById([FromRoute] string deviceId)
    {
        var result = await deviceService.GetDeviceByIdAsync(deviceId);

        return result.Match<ActionResult<DeviceDto>>(
            success => Ok(DeviceDto.FromDevice(success.Value)),
            _ => NotFound());
    }

    /// <summary>
    ///     Gets the owner of a device
    /// </summary>
    [HttpGet("{deviceId}/owner")]
    [ProducesResponseType<PersonDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<PersonDto>> GetOwner([FromRoute] string deviceId)
    {
        var result = await deviceService.GetOwnerOfDeviceAsync(deviceId);

        return result.Match<ActionResult<PersonDto>>(
            success => Ok(PersonDto.FromPerson(success.Value)),
            _ => NotFound());
    }

    /// <summary>
    ///     Gets the users of a device, the owner is not included
    /// </summary>
    [HttpGet("{deviceId}/users")]
    [ProducesResponseType<PersonListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<PersonListResponse>> GetUsers([FromRoute] string deviceId)
    {
        var result = await deviceService.GetUsersOfDeviceAsync(deviceId);

        return result.Match<ActionResult<PersonListResponse>>(
            success => Ok(PersonListResponse.FromPersons(success.Value)),
            _ => NotFound());
    }

    /// <summary>
    ///     Adds a device
    /// </summary>
    [HttpPost]
    [ProducesResponseType<DeviceDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<ActionResult<DeviceDto>> CreateDevice([FromBody] AddDeviceRequest request)
    {
        if (!IsValid<AddDeviceRequest.Validator, AddDeviceRequest>(request))
        {
            return ValidationProblem();
        }

        var result = await deviceService.AddDeviceAsync(request.DeviceId, request.OwnerId, request.CategoryId);

        return result.Match<ActionResult<DeviceDto>>(
            success => CreatedAtAction(nameof(GetDeviceById), new { deviceId = success.Value.Id },
                                       DeviceDto.FromDevice(success.Value)),
            _ => NotFound(),
            _ => Conflict());
    }

    /// <summary>
    ///     Adds a user to a device
    /// </summary>
    [HttpPost("{deviceId}/users/{userId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> AddUser([FromRoute] string deviceId, [FromRoute][ValidId] int userId)
    {
        var result = await deviceService.AddUserToDeviceAsync(deviceId, userId);

        return result.Match<IActionResult>(
            _ => NoContent(),
            _ => NotFound(),
            _ => Conflict(),
            _ => Conflict());
    }

    /// <summary>
    ///     Removes a user from a device, the owner cannot be removed
    /// </summary>
    [HttpDelete("{deviceId}/users/{userId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> RemoveUser([FromRoute] string deviceId, [FromRoute][ValidId] int userId)
    {
        var result = await deviceService.RemoveUserFromDeviceAsync(deviceId, userId);

        return result.Match<IActionResult>(
            _ => NoContent(),
            _ => NotFound(),
            _ => Conflict());
    }

    /// <summary>
    ///     Makes another person the owner of a device, the previous owner becomes a user
    /// </summary>
    [HttpPatch("{deviceId}/owner/{userId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> ChangeOwner([FromRoute] string deviceId, [FromRoute][ValidId] int userId)
    {
        var result = await deviceService.ChangeOwnerOfDeviceAsync(deviceId, userId);

        return result.Match<IActionResult>(
            _ => NoContent(),
            _ => NotFound(),
            _ => Conflict());
    }
}