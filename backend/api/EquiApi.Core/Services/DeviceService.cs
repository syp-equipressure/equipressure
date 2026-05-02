using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using OneOf.Types;
using OneOf;

namespace EquiApi.Core.Services;

public interface IDeviceService
{
    /// <summary>
    /// Gibt alle devices eines bestimmten users zurück
    /// </summary>
    /// <param name="userId"> Id des users dessen Geräte wir haben wollen </param>
    /// <returns>
    /// NotFound -> wenn der user nicht gefunden wurde
    /// Success -> eine Liste der Devices (auch wenn keine vorhanden sind)
    /// </returns>
    public ValueTask<OneOf<Success<IReadOnlyCollection<MeasurementDevice>>, NotFound>> GetDevicesFromUserIdAsync
        (int userId);
    
    /// <summary>
    /// Gibt die Person die als Owner eines gewissen Geräts angegeben wurde mit
    /// </summary>
    /// <param name="deviceId"></param>
    /// <returns></returns>
    public ValueTask<OneOf<Success<Person>, NotFound, NoOwnerFound>> GetOwnerOfDevice(int deviceId);
    public ValueTask<OneOf<Success<IReadOnlyCollection<Person>>, NotFound, NoUsersFound>> GetUsersOfDevice(int deviceId);

    public record NoOwnerFound(int deviceId);
    public record NoUsersFound(int deviceId);
}

public class DeviceService(IUnitOfWork uow) : IDeviceService
{
    public async ValueTask<OneOf<Success<IReadOnlyCollection<MeasurementDevice>>, NotFound>> GetDevicesFromUserIdAsync
        (int userId)
    {
        var user = await uow.PersonRepository.GetPersonById(userId, false);
        if (user == null)
        {
            return new NotFound();
        }
        
        var devices = await uow.DeviceRepository.GetDevicesFromUserIdAsync(userId);
        return new Success<IReadOnlyCollection<MeasurementDevice>>(devices);
    }

    public async ValueTask<OneOf<Success<Person>, NotFound, IDeviceService.NoOwnerFound>> GetOwnerOfDevice(int deviceId)
    {
        var exists = await uow.DeviceRepository.DeviceExistsAsync(deviceId);
        if (!exists)
        {
            return new NotFound();
        }
        
        var owner = await uow.DeviceRepository.GetOwnerOfDeviceAsync(deviceId);
        return owner.Match<OneOf<Success<Person>, NotFound, IDeviceService.NoOwnerFound>>(success => 
             new Success<Person>(success),
                    notFound => new IDeviceService.NoOwnerFound(deviceId));
    }

    public async ValueTask<OneOf<Success<IReadOnlyCollection<Person>>, NotFound, IDeviceService.NoUsersFound>> 
        GetUsersOfDevice(int deviceId)
    {
        var exists = await uow.DeviceRepository.DeviceExistsAsync(deviceId);
        if (!exists)
        {
            return new NotFound();
        }
        
        var users = await uow.DeviceRepository.GetPersonOfDeviceAsync(deviceId);

        return users.Match<OneOf<Success<IReadOnlyCollection<Person>>, NotFound, IDeviceService.NoUsersFound>>(
             success => new Success<IReadOnlyCollection<Person>>(success),
             notFound => new IDeviceService.NoUsersFound(deviceId)
             );
    }
}
