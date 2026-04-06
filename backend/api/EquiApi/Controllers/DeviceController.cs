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
    [ProducesResponseType<DeviceListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async ValueTask<ActionResult<DeviceListResponse>> GetDevicesByUserId([FromRoute] int userId)
    {
        if (userId < 0)
        {
            logger.LogWarning("userId {userId} has to be a valid number", userId);

            return BadRequest();
        }
        
        var result = await deviceService.GetDevicesFromUserIdAsync(userId);
        
        return result.Match<ActionResult<DeviceListResponse>>(success => Ok(DeviceListResponse
                                                                                .FromDeviceList(success.Value)),
                                                              notFound => NotFound());
    }
    
    [HttpGet("{deviceId:int}/owner")]
    [ProducesResponseType<PersonDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async ValueTask<ActionResult<PersonDto>> GetOwnerByDeviceId([FromRoute] int deviceId)
    {
        if (deviceId < 0)
        {
            logger.LogWarning("deviceId {deviceId} has to be a valid number", deviceId);
            return BadRequest();
        }

        var result = await deviceService.GetOwnerOfDevice(deviceId);

        return result.Match<ActionResult<PersonDto>>(
                                                     success => Ok(PersonDto.FromPerson(success.Value)),
                                                     notFound => NotFound(),
                                                     noOwner =>
                                                     {
                                                         logger.LogWarning("Device {deviceId} has no owner", noOwner.deviceId);
                                                         return UnprocessableEntity();
                                                     });
    }

    [HttpGet("{deviceId:int}/users")]
    [ProducesResponseType<PersonListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async ValueTask<ActionResult<PersonListResponse>> GetUsersByDeviceId([FromRoute] int deviceId)
    {
        if (deviceId < 0)
        {
            logger.LogWarning("deviceId {deviceId} has to be a valid number", deviceId);
            return BadRequest();
        }

        var result = await deviceService.GetUsersOfDevice(deviceId);

        return result.Match<ActionResult<PersonListResponse>>(
                                                              success => Ok(PersonListResponse.FromPersonList(success.Value)),
                                                              notFound => NotFound(),
                                                              noUsers =>
                                                              {
                                                                  logger.LogWarning("Device {deviceId} has no assigned users", noUsers.deviceId);
                                                                  return UnprocessableEntity();
                                                              });
    }
    
}



public sealed record DeviceDto(int Id, int CategoryId, Person Owner, List<DeviceUser> DeviceUser)
{
    public static DeviceDto FromDevice(MeasurementDevice device) =>
        new(device.Id, device.CategoryId, device.Owner, device.Users);
}

public sealed record DeviceListResponse(IEnumerable<DeviceDto> Devices)
{
    public static DeviceListResponse FromDeviceList(IReadOnlyCollection<MeasurementDevice> devices) =>
        new(devices.Select(d => DeviceDto.FromDevice(d)));
}

public sealed record PersonDto(int Id, string FirstName, string LastName)
{
    public static PersonDto FromPerson(Person person) =>
        new(person.Id, person.FirstName, person.LastName);
}

public sealed record PersonListResponse(IEnumerable<PersonDto> Users)
{
    public static PersonListResponse FromPersonList(IReadOnlyCollection<Person> persons) =>
        new(persons.Select(PersonDto.FromPerson));
}