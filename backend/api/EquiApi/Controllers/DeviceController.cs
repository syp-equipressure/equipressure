using EquiApi.Core.Services;
using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using EquiApi.Util;
using Microsoft.AspNetCore.Mvc;

namespace EquiApi.Controllers;

[Route("api/devices")]
public sealed class DeviceController(
    IDeviceService deviceService,
    ITransactionProvider transaction,
    ILogger<DeviceController> logger) : BaseController
{
    [HttpGet("{userId:int}")]
    [ProducesResponseType<DataTransfer.DeviceListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<ActionResult<DataTransfer.DeviceListResponse>> GetDevicesByUserId([FromRoute] int userId)
    {
        if (userId < 0)
        {
            logger.LogWarning("userId {userId} has to be a valid number", userId);

            return BadRequest();
        }

        var result = await deviceService.GetDevicesFromUserIdAsync(userId);

        return result.Match<ActionResult<DataTransfer.DeviceListResponse>>(success => Ok(DataTransfer.DeviceListResponse
                                                                               .FromDevices(success.Value)),
                                                                           notFound => NotFound());
    }

    [HttpGet("{deviceId}/owner")]
    [ProducesResponseType<DataTransfer.PersonDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async ValueTask<ActionResult<DataTransfer.PersonDto>> GetOwnerByDeviceId([FromRoute] string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            logger.LogWarning("deviceId {deviceId} has to be a valid number", deviceId);

            return BadRequest();
        }

        var result = await deviceService.GetOwnerOfDevice(deviceId);

        return
            result
                .Match<
                    ActionResult<DataTransfer.PersonDto>>(success => Ok(DataTransfer.PersonDto.FromPerson(success.Value)),
                                                          notFound => NotFound(),
                                                          noOwner =>
                                                          {
                                                              logger.LogWarning("Device {deviceId} has no owner",
                                                                                noOwner.DeviceId);

                                                              return UnprocessableEntity();
                                                          });
    }

    [HttpGet("{deviceId}/users")]
    [ProducesResponseType<DataTransfer.PersonListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async ValueTask<ActionResult<DataTransfer.PersonListResponse>> GetUsersByDeviceId(
        [FromRoute] string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            logger.LogWarning("deviceId {deviceId} has to be a valid number", deviceId);

            return BadRequest();
        }

        var result = await deviceService.GetUsersOfDevice(deviceId);

        return
            result
                .Match<
                    ActionResult<
                        DataTransfer.PersonListResponse>>(success => Ok(DataTransfer.PersonListResponse.FromPersons(success.Value)),
                                                          notFound => NotFound(),
                                                          noUsers =>
                                                          {
                                                              logger
                                                                  .LogWarning("Device {deviceId} has no assigned users",
                                                                              noUsers.DeviceId);

                                                              return UnprocessableEntity();
                                                          });
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<IActionResult> CreateDevice([FromBody] DataTransfer.AddDeviceRequest request)
    {
        if (!ValidateRequest<DataTransfer.AddDeviceRequest.Validator, DataTransfer.AddDeviceRequest>(request))
        {
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            var result
                = await deviceService.AddDeviceAsync(request.DeviceId, request.OwnerId, request.CategoryId);

            return await result.Match<ValueTask<IActionResult>>(async success =>
                                                                {
                                                                    await transaction.CommitAsync();

                                                                    return Created();
                                                                },
                                                                async notFound =>
                                                                {
                                                                    await transaction.RollbackAsync();

                                                                    return NotFound();
                                                                });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            logger.LogError("Error adding Device");

            return Problem();
        }
    }

    [HttpPost("{deviceId}/add/{userId:int}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<IActionResult> AddUserToDevice([FromRoute] string deviceId,
                                                          [FromRoute] int userId)
    {
        if (string.IsNullOrWhiteSpace(deviceId) || userId < 0)
        {
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            var result
                = await deviceService.AddUserToDevice(userId, deviceId);

            return await result.Match<ValueTask<IActionResult>>(async success =>
                                                                {
                                                                    await transaction.CommitAsync();

                                                                    return Created();
                                                                },
                                                                async notFound =>
                                                                {
                                                                    await transaction.RollbackAsync();

                                                                    return NotFound();
                                                                },
                                                                async tooManyUsers =>
                                                                {
                                                                    await transaction.RollbackAsync();

                                                                    return Conflict();
                                                                });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            logger.LogError("Error adding User to Device");

            return Problem();
        }
    }
    [HttpDelete("{deviceId}/remove/{userId:int}")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status409Conflict)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async ValueTask<IActionResult> RemoveUserFromDevice([FromRoute] string deviceId,
                                                           [FromRoute] int userId)
{
    if (string.IsNullOrWhiteSpace(deviceId) || userId < 0)
    {
        return BadRequest();
    }

    try
    {
        await transaction.BeginTransactionAsync();

        var result
            = await deviceService.RemoveUserFromDevice(userId, deviceId);

        return await result.Match<ValueTask<IActionResult>>(async success =>
                                                            {
                                                                await transaction.CommitAsync();

                                                                return Ok();
                                                            },
                                                            async notFound =>
                                                            {
                                                                await transaction.RollbackAsync();

                                                                return NotFound();
                                                            },
                                                            async tooLittleUsers =>
                                                            {
                                                                await transaction.RollbackAsync();

                                                                return Conflict();
                                                            },
                                                            async ownerCantBeDeleted =>
                                                            {
                                                                await transaction.RollbackAsync();

                                                                return Conflict();
                                                            });
    }
    catch (Exception)
    {
        await transaction.RollbackAsync();
        logger.LogError("Error removing User from Device");

        return Problem();
    }
}
}
