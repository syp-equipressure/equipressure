using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using OneOf.Types;
using OneOf;

namespace EquiApi.Persistence.Repositories;

public interface IDeviceRepository
{
    /// <summary>
    /// Checks if the device exists
    /// </summary>
    /// <param name="deviceId">id of the device</param>
    /// <returns>true if it exists false if not</returns>
    public ValueTask<bool> DeviceExistsAsync(string deviceId);
    
    /// <summary>
    /// Gets a device by its id
    /// </summary>
    /// <param name="deviceId">serial number of the device</param>
    /// <returns>The measurementDevice if it exists</returns>
    public ValueTask<MeasurementDevice?> GetDeviceByIdAsync(string deviceId);
    
    /// <summary>
    /// Gets all devices of a user
    /// </summary>
    /// <param name="userId">the id of the user</param>
    /// <returns>a list of the devices of the user</returns>
    public ValueTask<IReadOnlyCollection<MeasurementDevice>> GetDevicesFromUserIdAsync(int userId);
    
    /// <summary>
    /// Gets the owner of a device
    /// </summary>
    /// <param name="deviceId">serial number of the device</param>
    /// <returns>the person object of the owner or a notFound if there is no Owner</returns>
    public ValueTask<Person?> GetOwnerOfDeviceAsync(string deviceId);
    
    /// <summary>
    /// Gets all the people which are subscribed on a specific device incl. Owner
    /// </summary>
    /// <param name="deviceId">serial number of the device</param>
    /// <returns>the list of person objects or a notfound if there are none</returns>
    public ValueTask<IReadOnlyCollection<Person>> GetPersonsOfDeviceAsync(string deviceId);

    /// <summary>
    /// Gets the DeviceUser entry by its id
    /// DeviceUser: object which has all the users of a device in it because users and devices
    /// have an m to n relationship
    /// </summary>
    /// <param name="deviceId">serial number of the device</param>
    /// <param name="userId"></param>
    /// <see cref="DeviceUser">
    /// <returns>A Device User Entry if it exists</returns>
    public ValueTask<DeviceUser?> GetDeviceUserEntryAsync(string deviceId, int userId);
    
    /// <summary>
    /// Adds a device to the dbset
    /// </summary>
    /// <param name="device">the device that should be added</param>
    public void AddDevice(MeasurementDevice device);
    
    /// <summary>
    /// checks if a category exists by the id
    /// </summary>
    /// <param name="categoryId"></param>
    /// <returns>true if its found false if not</returns>
    public ValueTask<bool> CategoryExistsAsync(int categoryId);
}

public class DeviceRepository(DbSet<MeasurementDevice> devices, DbSet<DeviceCategory> categories) : IDeviceRepository
{
    public async ValueTask<bool> DeviceExistsAsync(string deviceId)
    => await devices.AnyAsync(d => d.Id == deviceId);

    public async ValueTask<MeasurementDevice?> GetDeviceByIdAsync(string deviceId)
        =>  await devices.Include(d => d.Users)
                         .Include(d => d.Category)
                         .FirstOrDefaultAsync(d => d.Id == deviceId);
    
    
    public async ValueTask<IReadOnlyCollection<MeasurementDevice>> GetDevicesFromUserIdAsync(int userId)
    {
        return await devices.Include(d => d.Users)
                         .Where(d => (d.Users.Any(u => u.UserId == userId)) || (d.OwnerId == userId)).ToListAsync();
    }

    public async ValueTask<Person?> GetOwnerOfDeviceAsync(string deviceId)
    {
        return  await devices.Include(d => d.Owner)
                            .Where(d => d.Id == deviceId)
                            .Select(d => d.Owner).FirstOrDefaultAsync();
    }


    public async ValueTask<IReadOnlyCollection<Person>> GetPersonsOfDeviceAsync(string deviceId)
    {
        return await devices.Include(d => d.Users)
                                  .ThenInclude(u => u.User)
                                  .Where(d => d.Id == deviceId)
                                  .SelectMany(d => d.Users.Select(u => u.User))
                                  .ToListAsync();
    }

    public void AddDevice(MeasurementDevice device)
    {
        devices.Add(device);
    }

    public async ValueTask<DeviceUser?> GetDeviceUserEntryAsync(string deviceId, int userId) 
        => await devices.Include(d => d.Users)
                        .Where(d => d.Id == deviceId)
                        .SelectMany(d => d.Users)
                        .FirstOrDefaultAsync(du => du.UserId == userId);

    public async ValueTask<bool> CategoryExistsAsync(int categoryId)
    => await categories.AnyAsync(c => c.Id == categoryId);
}
