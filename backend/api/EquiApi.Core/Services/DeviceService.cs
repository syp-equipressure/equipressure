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
    public ValueTask<OneOf<Success<Person>, NotFound, NoOwnerFound>> GetOwnerOfDevice(string deviceId);
    /// <summary>
    /// gets all the users of a specific device
    /// </summary>
    /// <param name="deviceId">the id of the device</param>
    /// <returns>
    ///a list of all the person objects which are registered for the device or a notFound if the device with the id does
    /// not exist or a NoUsersFound if there are no Users registered for the device
    /// </returns>
    public ValueTask<OneOf<Success<IReadOnlyCollection<Person>>, NotFound, NoUsersFound>> GetUsersOfDevice(string deviceId);

    public ValueTask<OneOf<Success<MeasurementDevice>, NotFound>> AddDeviceAsync(string deviceId, int ownerId, int categoryId);
    public ValueTask<OneOf<Success<MeasurementDevice>, NotFound, TooManyUsers>> AddUserToDevice(int userId, string deviceId);
    public ValueTask<OneOf<Success<MeasurementDevice>, NotFound, TooLittleUsers, OwnerCantBeDeleted>> 
        RemoveUserFromDevice(int userId, string deviceId);

    public record NoOwnerFound(string DeviceId);
    public record NoUsersFound(string DeviceId);
    public record TooLittleUsers();
    public record TooManyUsers();
    public record OwnerCantBeDeleted();
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

    public async ValueTask<OneOf<Success<Person>, NotFound, IDeviceService.NoOwnerFound>> GetOwnerOfDevice(string deviceId)
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
        GetUsersOfDevice(string deviceId)
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

    public async ValueTask<OneOf<Success<MeasurementDevice>, NotFound>> AddDeviceAsync(string deviceId, int ownerId, 
        int categoryId)
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
            Id = deviceId,
            OwnerId = ownerId,
            CategoryId = categoryId
        };

        uow.DeviceRepository.AddDevice(device);
        await uow.SaveChangesAsync();

        return new Success<MeasurementDevice>(device);
    }

    public async ValueTask<OneOf<Success<MeasurementDevice>, NotFound, IDeviceService.TooManyUsers>> 
        AddUserToDevice(int userId, string deviceId)
    {
        var device = await uow.DeviceRepository.GetDeviceByIdAsync(deviceId);
        if (device is null)
        {
            logger.LogWarning("Device with id {id} could not be found", deviceId);
            return new NotFound();
        }

        if (!await uow.PersonRepository.PersonExists(userId))
        {
            logger.LogWarning("User with id {id} could not be found", userId);
            return new NotFound();
        }

        if (device.Category.NumOfAllowedPeople > device.Users.Count + 1)
        {
            return new IDeviceService.TooManyUsers();
        }

        var dU = await uow.DeviceRepository.GetDeviceUserEntry(deviceId, userId);
        if (dU is null)
        {
            return new NotFound();
        }
        device.Users.Remove(dU);
        await uow.SaveChangesAsync();

        return new Success<MeasurementDevice>(device);
    }

    public async ValueTask<OneOf<Success<MeasurementDevice>, NotFound, IDeviceService.TooLittleUsers, 
        IDeviceService.OwnerCantBeDeleted>> RemoveUserFromDevice(int userId, string deviceId)
    {
        var device = await uow.DeviceRepository.GetDeviceByIdAsync(deviceId);
        if (device is null)
        {
            logger.LogWarning("Device with id {id} could not be found", deviceId);
            return new NotFound();
        }

        if (!await uow.PersonRepository.PersonExists(userId))
        {
            logger.LogWarning("User with id {id} could not be found", userId);
            return new NotFound();
        }

        if (device.Users.Count - 1 < 1)
        {
            return new IDeviceService.TooLittleUsers();
        }

        if (userId == device.OwnerId)
        {
            return new IDeviceService.OwnerCantBeDeleted();
        }
        
        var dU = new DeviceUser
        {
            DeviceId = deviceId,
            UserId = userId
        };
        device.Users.Remove(dU);
        await uow.SaveChangesAsync();

        return new Success<MeasurementDevice>(device);
    }
}
