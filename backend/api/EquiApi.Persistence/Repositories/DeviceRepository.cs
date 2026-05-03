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
    public ValueTask<bool> DeviceExistsAsync(int deviceId);
    
    /// <summary>
    /// Gets a device by its id
    /// </summary>
    /// <param name="deviceId">the id of the device</param>
    /// <returns>The measurementDevice if it exists</returns>
    public ValueTask<MeasurementDevice?> GetDeviceByIdAsync(int deviceId);
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
    public ValueTask<OneOf<Person, NotFound>> GetOwnerOfDeviceAsync(int deviceId);
    /// <summary>
    /// Gets all the people which are subscribed on a specific device
    /// </summary>
    /// <param name="deviceId">the id of the device</param>
    /// <returns>the list of person objects or a notfound if there are none</returns>
    public ValueTask<OneOf<IReadOnlyCollection<Person>, NotFound>> GetPersonOfDeviceAsync(int deviceId);
    
    public void AddDevice(MeasurementDevice device);
}

public class DeviceRepository(DbSet<MeasurementDevice> devices) : IDeviceRepository
{
    
    public async ValueTask<bool> DeviceExistsAsync(int deviceId)
    => await devices.AnyAsync(d => d.Id == deviceId);

    public async ValueTask<MeasurementDevice?> GetDeviceByIdAsync(int deviceId)
        =>  await devices.FirstOrDefaultAsync(d => d.Id == deviceId);
    
    
    public async ValueTask<IReadOnlyCollection<MeasurementDevice>> GetDevicesFromUserIdAsync(int userId)
    {
        return await devices.Include(d => d.Users)
                         .Where(d => d.Users.Any(u => u.UserId == userId)).ToListAsync();
    }

    public async ValueTask<OneOf<Person, NotFound>> GetOwnerOfDeviceAsync(int deviceId)
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

    public async ValueTask<OneOf<IReadOnlyCollection<Person>, NotFound>> GetPersonOfDeviceAsync(int deviceId)
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
}
