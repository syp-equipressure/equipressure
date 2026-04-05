using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using OneOf.Types;
using OneOf;

namespace EquiApi.Persistence.Repositories;

public interface IDeviceRepository
{
    public ValueTask<bool> DeviceExistsAsync(int deviceId);
    
    public ValueTask<MeasurementDevice?> GetDeviceByIdAsync(int deviceId);
    public ValueTask<IReadOnlyCollection<MeasurementDevice>> GetDevicesFromUserIdAsync(int userId);
    public ValueTask<OneOf<Person, NotFound>> GetOwnerOfDeviceAsync(int deviceId);
    public ValueTask<OneOf<IReadOnlyCollection<Person>, NotFound>> GetPersonOfDeviceAsync(int deviceId);

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
}
