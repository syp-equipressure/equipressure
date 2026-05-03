using EquiApi.Core.Services;
using EquiApi.Persistence.Model;
using EquiApi.Util;
using Microsoft.AspNetCore.Mvc;
using OneOf;
using OneOf.Types;

namespace EquiApi.Controllers;

[Route("api/devices")]
public sealed class DeviceController(IDeviceService deviceService, 
                              ILogger<DeviceController> logger) : BaseController
{
    [HttpGet("/{userId:int}")]
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
    
    [HttpGet("{deviceId:int}/owner")]
    [ProducesResponseType<DataTransfer.PersonDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async ValueTask<ActionResult<DataTransfer.PersonDto>> GetOwnerByDeviceId([FromRoute] int deviceId)
    {
        if (deviceId < 0)
        {
            logger.LogWarning("deviceId {deviceId} has to be a valid number", deviceId);
            return BadRequest();
        }

        var result = await deviceService.GetOwnerOfDevice(deviceId);

        return result.Match<ActionResult<DataTransfer.PersonDto>>(
                                                                  success => Ok(DataTransfer.PersonDto.FromPerson(success.Value)),
                                                                  notFound => NotFound(),
                                                                  noOwner =>
                                                                  {
                                                                      logger.LogWarning("Device {deviceId} has no owner", noOwner.deviceId);
                                                                      return UnprocessableEntity();
                                                                  });
    }

    [HttpGet("{deviceId:int}/users")]
    [ProducesResponseType<DataTransfer.PersonListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async ValueTask<ActionResult<DataTransfer.PersonListResponse>> GetUsersByDeviceId([FromRoute] int deviceId)
    {
        if (deviceId < 0)
        {
            logger.LogWarning("deviceId {deviceId} has to be a valid number", deviceId);
            return BadRequest();
        }

        var result = await deviceService.GetUsersOfDevice(deviceId);

        return result.Match<ActionResult<DataTransfer.PersonListResponse>>(
                                                                           success => Ok(DataTransfer.PersonListResponse.FromPersons(success.Value)),
                                                                           notFound => NotFound(),
                                                                           noUsers =>
                                                                           {
                                                                               logger.LogWarning("Device {deviceId} has no assigned users", noUsers.deviceId);
                                                                               return UnprocessableEntity();
                                                                           });
    }
    
}