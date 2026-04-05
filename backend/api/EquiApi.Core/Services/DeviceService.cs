using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using OneOf.Types;
using OneOf;

namespace EquiApi.Core.Services;

public interface IDeviceService
{
    public ValueTask<OneOf<Success<IReadOnlyCollection<MeasurementDevice>>, NotFound>> GetDevicesFromUserIdAsync(int userId);
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
}
