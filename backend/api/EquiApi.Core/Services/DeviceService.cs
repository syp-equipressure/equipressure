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

    public ValueTask<OneOf<Success<MeasurementDevice>, NotFound>> AddDeviceAsync(int ownerId, int categoryId);
    public void AddUserToDevice(int userId, int deviceId);
    public void RemoveUserFromDevice(int userId, int deviceId);

    public record NoOwnerFound(int DeviceId);
    public record NoUsersFound(int DeviceId);
}

public class DeviceService(IUnitOfWork uow, ILogger<DeviceService> logger) : IDeviceService
{
    public async ValueTask<OneOf<Success<IReadOnlyCollection<MeasurementDevice>>, NotFound>> GetDevicesFromUserIdAsync
        (int userId)
    {
        var user = await uow.PersonRepository.GetPersonById(userId);
        if (user == null)
        {
            logger.LogWarning("User {UserId} not found", userId);
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
            logger.LogWarning("Device {DeviceId} not found", deviceId);
            return new NotFound();
        }
        
        var owner = await uow.DeviceRepository.GetOwnerOfDeviceAsync(deviceId);
        return owner.Match<OneOf<Success<Person>, NotFound, IDeviceService.NoOwnerFound>>(success => 
             new Success<Person>(success),
                    notFound =>
                    {
                        logger.LogWarning("No owner registered for device {DeviceId}", deviceId);
                        return new IDeviceService.NoOwnerFound(deviceId);
                    });
    }

    public async ValueTask<OneOf<Success<IReadOnlyCollection<Person>>, NotFound, IDeviceService.NoUsersFound>> 
        GetUsersOfDevice(int deviceId)
    {
        var exists = await uow.DeviceRepository.DeviceExistsAsync(deviceId);
        if (!exists)
        {
            logger.LogWarning("Device {DeviceId} not found", deviceId);
            return new NotFound();
        }
        
        var users = await uow.DeviceRepository.GetPersonOfDeviceAsync(deviceId);

        return users.Match<OneOf<Success<IReadOnlyCollection<Person>>, NotFound, IDeviceService.NoUsersFound>>(
             success => new Success<IReadOnlyCollection<Person>>(success),
             notFound =>
             {
                 logger.LogWarning("No users registered for device {DeviceId}", deviceId);
                 return new IDeviceService.NoUsersFound(deviceId);
             });
    }

    public async ValueTask<OneOf<Success<MeasurementDevice>, NotFound>> AddDeviceAsync(int ownerId, int categoryId)
    {
        if (!(await uow.PersonRepository.PersonExists(ownerId)))
        {
            logger.LogWarning("Person with id {id} could not be found", ownerId);
            return new NotFound();
        }

        if (!(await uow.DeviceRepository.CategoryExists(categoryId)))
        {
            logger.LogWarning("Category with id {id} could not be found", categoryId);
            return new NotFound();
        }

        var device = new MeasurementDevice
        {
            OwnerId = ownerId,
            CategoryId = categoryId
        };

        uow.DeviceRepository.AddDevice(device);
        await uow.SaveChangesAsync();

        return new Success<MeasurementDevice>(device);
    }

    public void AddUserToDevice(int userId, int deviceId)
    {
        throw new NotImplementedException();
    }

    public void RemoveUserFromDevice(int userId, int deviceId)
    {
        throw new NotImplementedException();
    }
}
