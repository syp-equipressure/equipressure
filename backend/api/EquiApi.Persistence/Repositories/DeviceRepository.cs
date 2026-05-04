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
    /// <param name="deviceId">the id of the device</param>
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
    /// <param name="deviceId">the id of the device</param>
    /// <returns>the person object of the owner or a notFound if there is no Owner</returns>
    public ValueTask<OneOf<Person, NotFound>> GetOwnerOfDeviceAsync(string deviceId);
    /// <summary>
    /// Gets all the people which are subscribed on a specific device
    /// </summary>
    /// <param name="deviceId">the id of the device</param>
    /// <returns>the list of person objects or a notfound if there are none</returns>
    public ValueTask<OneOf<IReadOnlyCollection<Person>, NotFound>> GetPersonOfDeviceAsync(string deviceId);

    public ValueTask<DeviceUser?> GetDeviceUserEntry(string deviceId, int userId);
    
    public void AddDevice(MeasurementDevice device);
    public ValueTask<bool> CategoryExists(int categoryId);

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
                         .Where(d => d.Users.Any(u => u.UserId == userId)).ToListAsync();
    }

    public async ValueTask<OneOf<Person, NotFound>> GetOwnerOfDeviceAsync(string deviceId)
    {
        var result =  await devices.Include(d => d.Owner)
                            .Where(d => d.Id == deviceId)
                            .Select(d => d.Owner).FirstOrDefaultAsync();
        if (result == null)
        {
            return new NotFound();
        }
        return result;
    }

    public async ValueTask<OneOf<IReadOnlyCollection<Person>, NotFound>> GetPersonOfDeviceAsync(string deviceId)
    {
        var result = await devices.Include(d => d.Users)
                                  .ThenInclude(u => u.User)
                                  .Where(d => d.Id == deviceId)
                                  .Select(d => d.Users.Select(u => u.User).FirstOrDefault())
                                  .ToListAsync();
        
        var res = result.Where(p => p != null)
                        .Cast<Person>()
                        .ToList();

        if (res.Count < 0)
        {
            return  new NotFound();
        }

        return res;
    }

    public void AddDevice(MeasurementDevice device)
    {
        devices.Add(device);
    }

    public async ValueTask<DeviceUser?> GetDeviceUserEntry(string deviceId, int userId) 
        => await devices.Include(d => d.Users).SelectMany(d => d.Users)
                        .FirstOrDefaultAsync(du => du.DeviceId == deviceId && du.UserId == userId);

    public async ValueTask<bool> CategoryExists(int categoryId)
    => await categories.AnyAsync(c => c.Id == categoryId);
    public async ValueTask<bool> DeviceExists(string deviceId)
        => await devices.AnyAsync(c => c.Id == deviceId);
}
