using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using OneOf.Types;
using OneOf;

namespace EquiApi.Persistence.Repositories;

public interface IDeviceRepository
{
    public ValueTask<IReadOnlyCollection<MeasurementDevice>> GetDevicesFromUserIdAsync(int userId);
    public ValueTask<OneOf<Person, NotFound>> GetOwnerOfDeviceAsync(int deviceId);
    public ValueTask<IReadOnlyCollection<Person>> GetPersonOfDeviceAsync(int deviceId);
}

public class DeviceRepository(DbSet<MeasurementDevice> devices) : IDeviceRepository
{
    
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

    public ValueTask<IReadOnlyCollection<Person>> GetPersonOfDeviceAsync(int deviceId) => throw new NotImplementedException();
}
