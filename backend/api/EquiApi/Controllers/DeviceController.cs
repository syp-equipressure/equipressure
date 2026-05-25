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
                                                                           notFound =>
                                                                           {
                                                                               logger.LogWarning("No devices found for userId {userId}", userId);
                                                                               return NotFound();
                                                                           });
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
                    ActionResult<DataTransfer.PersonDto>>(success =>
                                                              Ok(DataTransfer.PersonDto.FromPerson(success.Value)),
                                                          notFound =>
                                                          {
                                                              logger.LogWarning("Device {deviceId} not found", deviceId);
                                                              return NotFound();
                                                          },
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
                        DataTransfer.PersonListResponse>>(success =>
                                                              Ok(DataTransfer.PersonListResponse
                                                                             .FromPersons(success.Value)),
                                                          notFound =>
                                                          {
                                                              logger.LogWarning("Device {deviceId} not found", deviceId);
                                                              return NotFound();
                                                          },
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
                                                                    logger.LogInformation("Device {id} successfully added", request.DeviceId);
                                                                    await transaction.CommitAsync();

                                                                    return Created();
                                                                },
                                                                async notFound =>
                                                                {
                                                                    logger.LogWarning("CreateDevice failed — owner or category not found for DeviceId" +
                                                                         " {deviceId}, OwnerId {ownerId}, CategoryId {categoryId}",
                                                                         request.DeviceId, request.OwnerId, request.CategoryId);
                                                                    await transaction.RollbackAsync();

                                                                    return NotFound();
                                                                });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error adding Device {deviceId}", request.DeviceId);

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
            logger.LogWarning("AddUserToDevice called with invalid arguments: deviceId {deviceId}, userId {userId}", deviceId, userId);
            return BadRequest();
        }

        try
        {
            await transaction.BeginTransactionAsync();

            var result
                = await deviceService.AddUserToDevice(userId, deviceId);

            return await result.Match<ValueTask<IActionResult>>(async success =>
                                                                {
                                                                    logger.LogInformation("User {uId} successfully added to device {dId}", userId, deviceId);
                                                                    await transaction.CommitAsync();

                                                                    return Created();
                                                                },
                                                                async notFound =>
                                                                {
                                                                    await transaction.RollbackAsync();
                                                                    logger.LogWarning("AddUserToDevice failed — device or user not found: deviceId {deviceId}, userId {userId}", deviceId, userId);
                                                                    return NotFound();
                                                                },
                                                                async tooManyUsers =>
                                                                {
                                                                    await transaction.RollbackAsync();
                                                                    logger.LogWarning("AddUserToDevice failed — too many users assigned to device {deviceId}", deviceId);

                                                                    return Conflict();
                                                                });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error adding User {userId} to Device {deviceId}", userId, deviceId);

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
            logger.LogWarning("RemoveUserFromDevice called with invalid arguments: deviceId {deviceId}, userId {userId}", deviceId, userId);
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
                                                                    logger.LogInformation("User {uId} successfully removed from device {dId}", userId, deviceId);
                                                                    return Ok();
                                                                },
                                                                async notFound =>
                                                                {
                                                                    await transaction.RollbackAsync();
                                                                    logger.LogWarning("RemoveUserFromDevice failed — device or user not found: deviceId {deviceId}, userId {userId}", deviceId, userId);

                                                                    return NotFound();
                                                                },
                                                                async tooLittleUsers =>
                                                                {
                                                                    await transaction.RollbackAsync();
                                                                    logger.LogWarning("RemoveUserFromDevice failed — device {deviceId} would have too few users after removal", deviceId);
                                                                    return Conflict();
                                                                },
                                                                async ownerCantBeDeleted =>
                                                                {
                                                                    await transaction.RollbackAsync();
                                                                    logger.LogWarning("RemoveUserFromDevice failed — user {userId} is the owner of device {deviceId} and cannot be removed", userId, deviceId);
                                                                    return Conflict();
                                                                });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "Error removing User {userId} from Device {deviceId}", userId, deviceId);

            return Problem();
        }
    }
}
