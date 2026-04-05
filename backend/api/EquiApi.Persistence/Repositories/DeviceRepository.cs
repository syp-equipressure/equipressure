using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace EquiApi.Persistence.Repositories;

public interface IDeviceRepository
{
    public ValueTask<IReadOnlyCollection<MeasurementDevice>> GetDevicesFromUserIdAsync(int userId);
    public ValueTask<Person> GetOwnerOfDeviceAsync(int deviceId);
    public ValueTask<IReadOnlyCollection<Person>> GetPersonOfDeviceAsync(int deviceId);
}

public class DeviceRepository(DbSet<MeasurementDevice> devices) : IDeviceRepository
{
    
    public async ValueTask<IReadOnlyCollection<MeasurementDevice>> GetDevicesFromUserIdAsync(int userId)
    {
        return await devices.Include(d => d.Users)
                         .Where(d => d.Users.Any(u => u.UserId == userId)).ToListAsync();
    }

    public ValueTask<Person> GetOwnerOfDeviceAsync(int deviceId) => throw new NotImplementedException();

    public ValueTask<IReadOnlyCollection<Person>> GetPersonOfDeviceAsync(int deviceId) => throw new NotImplementedException();
}
