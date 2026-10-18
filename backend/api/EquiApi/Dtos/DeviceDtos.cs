using EquiApi.Persistence.Model;
using FluentValidation;

namespace EquiApi.Dtos;

/// <summary>
///     A measurement device
/// </summary>
/// <param name="Id">The serial number of the device</param>
/// <param name="CategoryId">The id of the device category</param>
/// <param name="CategoryName">The name of the device category</param>
/// <param name="Owner">The owner of the device</param>
/// <param name="Users">The users of the device, the owner is not included</param>
public sealed record DeviceDto(
    string Id,
    int CategoryId,
    string CategoryName,
    PersonDto Owner,
    IReadOnlyList<PersonDto> Users)
{
    public static DeviceDto FromDevice(MeasurementDevice device) =>
        new(device.Id, device.CategoryId, device.Category.Name, PersonDto.FromPerson(device.Owner),
            device.Users.Select(du => PersonDto.FromPerson(du.User)).ToList());
}

/// <summary>
///     A list of devices
/// </summary>
/// <param name="Devices">The devices</param>
public sealed record DeviceListResponse(IReadOnlyList<DeviceDto> Devices)
{
    public static DeviceListResponse FromDevices(IEnumerable<MeasurementDevice> devices) =>
        new(devices.Select(DeviceDto.FromDevice).ToList());
}

/// <summary>
///     Request to add a device
/// </summary>
/// <param name="DeviceId">The serial number of the device</param>
/// <param name="OwnerId">The id of the owner</param>
/// <param name="CategoryId">The id of the device category</param>
public sealed record AddDeviceRequest(string DeviceId, int OwnerId, int CategoryId)
{
    public sealed class Validator : AbstractValidator<AddDeviceRequest>
    {
        public Validator()
        {
            RuleFor(x => x.DeviceId).NotEmpty();
            RuleFor(x => x.OwnerId).GreaterThan(0);
            RuleFor(x => x.CategoryId).GreaterThan(0);
        }
    }
}