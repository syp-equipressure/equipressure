using EquiApi.Core.Services;
using EquiApi.Dtos;
using EquiApi.Util;
using Microsoft.AspNetCore.Mvc;

namespace EquiApi.Controllers;

[Route("api/devices")]
public sealed class DeviceController(IDeviceService deviceService) : BaseController
{
    /// <summary>
    ///     Gets all device categories, ordered by the number of allowed users
    /// </summary>
    // a literal segment has precedence over {deviceId}, so this route is not mistaken for a device
    [HttpGet("categories")]
    [ProducesResponseType<DeviceCategoryListResponse>(StatusCodes.Status200OK)]
    public async ValueTask<ActionResult<DeviceCategoryListResponse>> GetCategories()
    {
        var categories = await deviceService.GetAllCategoriesAsync();

        return Ok(DeviceCategoryListResponse.FromCategories(categories));
    }

    /// <summary>
    ///     Gets a device
    /// </summary>
    [HttpGet("{deviceId}")]
    [ProducesResponseType<DeviceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<ActionResult<DeviceDto>> GetDeviceById([FromRoute] string deviceId)
    {
        if (!await HasAccessAsync((access, me) => access.CanUseDeviceAsync(me, deviceId)))
        {
            return AccessDenied();
        }

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
        if (!await HasAccessAsync((access, me) => access.CanUseDeviceAsync(me, deviceId)))
        {
            return AccessDenied();
        }

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
        if (!await HasAccessAsync((access, me) => access.CanUseDeviceAsync(me, deviceId)))
        {
            return AccessDenied();
        }

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

        if (!await IsCurrentPersonAsync(request.OwnerId))
        {
            return AccessDenied();
        }

        var result = await deviceService.AddDeviceAsync(request.DeviceId, request.OwnerId, request.CategoryId);

        return result.Match<ActionResult<DeviceDto>>(
            success => CreatedAtAction(nameof(GetDeviceById), new { deviceId = success.Value.Id },
                                       DeviceDto.FromDevice(success.Value)),
            _ => NotFound(),
            _ => Conflict());
    }

    /// <summary>
    ///     Claims a device for the logged-in user (QR code scan): the user becomes the owner and gets the role of the
    ///     device category. 404 means that the profile has not been set up yet or the category does not exist.
    /// </summary>
    [HttpPost("{deviceId}/claim")]
    [ProducesResponseType<DeviceDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<ActionResult<DeviceDto>> ClaimDevice([FromRoute] string deviceId,
                                                                [FromBody] ClaimDeviceRequest request)
    {
        if (!IsValid<ClaimDeviceRequest.Validator, ClaimDeviceRequest>(request))
        {
            return ValidationProblem();
        }

        var result = await deviceService.ClaimDeviceAsync(deviceId, User.ExternalId, request.CategoryId);

        return result.Match<ActionResult<DeviceDto>>(
            success => CreatedAtAction(nameof(GetDeviceById), new { deviceId = success.Value.Id },
                                       DeviceDto.FromDevice(success.Value)),
            _ => NotFound(),
            _ => Problem(statusCode: StatusCodes.Status409Conflict,
                         title: "The device has already been claimed"),
            _ => Problem(statusCode: StatusCodes.Status409Conflict,
                         title: "A saddler needs an address with street"));
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
        if (!await HasAccessAsync((access, me) => access.CanManageDeviceAsync(me, deviceId)))
        {
            return AccessDenied();
        }

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
        if (!await HasAccessAsync((access, me) => access.CanRemoveDeviceUserAsync(me, deviceId, userId)))
        {
            return AccessDenied();
        }

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
        if (!await HasAccessAsync((access, me) => access.CanManageDeviceAsync(me, deviceId)))
        {
            return AccessDenied();
        }

        var result = await deviceService.ChangeOwnerOfDeviceAsync(deviceId, userId);

        return result.Match<IActionResult>(
            _ => NoContent(),
            _ => NotFound(),
            _ => Conflict());
    }

    /// <summary>
    ///     Removes a device including its users, a device that was used for measurements cannot be removed
    /// </summary>
    [HttpDelete("{deviceId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async ValueTask<IActionResult> DeleteDevice([FromRoute] string deviceId)
    {
        if (!await HasAccessAsync((access, me) => access.CanManageDeviceAsync(me, deviceId)))
        {
            return AccessDenied();
        }

        var result = await deviceService.DeleteDeviceAsync(deviceId);

        return result.Match<IActionResult>(
            _ => NoContent(),
            _ => NotFound(),
            _ => Problem(statusCode: StatusCodes.Status409Conflict,
                         detail: "The device was used for measurements, delete its measurement groups first"));
    }
}