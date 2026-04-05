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
    [HttpGet("/:{userId:int}")]
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
}

public sealed record DeviceListResponse(IEnumerable<DeviceDto> Devices)
{
    public static DeviceListResponse FromDeviceList(IReadOnlyCollection<MeasurementDevice> devices) =>
        new(devices.Select(d => DeviceDto.FromDevice(d)));
}

public sealed record DeviceDto(int Id, int CategoryId, Person Owner, List<DeviceUser> DeviceUser)
{
    public static DeviceDto FromDevice(MeasurementDevice device) =>
        new(device.Id, device.CategoryId, device.Owner, device.Users);
}
