using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using OneOf.Types;
using OneOf;

namespace EquiApi.Core.Services;

public interface IDeviceService
{
    /// <summary>
    /// returns all devices of a specific user
    /// </summary>
    /// <param name="userId">id of the user</param>
    /// <returns>
    /// returns a list of all devices or a not found if there are none
    /// </returns>
    public ValueTask<OneOf<Success<IReadOnlyCollection<MeasurementDevice>>, NotFound>> GetDevicesFromUserIdAsync
        (int userId);
    
    /// <summary>
    /// returns the owner of a specific user
    /// </summary>
    /// <param name="deviceId">id of the device</param>
    /// <returns>
    /// the person object who is the owner, notFound if the deviceId does not exist and
    /// NoOwnerFound if there is no owner registered
    /// </returns>
    public ValueTask<OneOf<Success<Person>, NotFound, NoOwnerFound>> GetOwnerOfDevice(int deviceId);
    /// <summary>
    /// gets all the users of a specific device
    /// </summary>
    /// <param name="deviceId">the id of the device</param>
    /// <returns>
    ///a list of all the person objects which are registered for the device or a notFound if the device with the id does
    /// not exist or a NoUsersFound if there are no Users registered for the device
    /// </returns>
    public ValueTask<OneOf<Success<IReadOnlyCollection<Person>>, NotFound, NoUsersFound>> GetUsersOfDevice(int deviceId);

    public record NoOwnerFound(int DeviceId);
    public record NoUsersFound(int DeviceId);
}

public class DeviceService(IUnitOfWork uow) : IDeviceService
{
    public async ValueTask<OneOf<Success<IReadOnlyCollection<MeasurementDevice>>, NotFound>> GetDevicesFromUserIdAsync
        (int userId)
    {
        var user = await uow.PersonRepository.GetPersonById(userId);
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
