using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using EquiPressure.Core.Service;
using OneOf.Types;
using OneOf;

namespace EquiApi.Core.Services;

using GetCitiesAsyncResult
    = OneOf<Success<IReadOnlyCollection<Address>>, Error>;
using AddAddressAsyncResult
    = OneOf<Success<Address>,
        IBaseService.InvalidData, Error>;

public interface ILocationService
{
    public ValueTask<GetCitiesAsyncResult> GetCitiesAsync(int? length, string? nameFilter);
    public ValueTask<AddAddressAsyncResult> AddAddressAsync(string? addressName, string plz, string cityName);
}

public class LocationService(IUnitOfWork uow) : ILocationService
{
    public async ValueTask<GetCitiesAsyncResult> GetCitiesAsync(int? length, string? nameFilter)
    {
        var res = await uow.LocationRepository.GetCitiesAsync(length, nameFilter, true);

        return res.Any()
            ? new Success<IReadOnlyCollection<Address>>(res)
            : new Error();
    }

    public async ValueTask<AddAddressAsyncResult> AddAddressAsync(string? addressName, string plz, string cityName)
    {
        var address = await uow.LocationRepository.AddressExists(addressName, plz, cityName, false)
                      ?? uow.LocationRepository.AddAddress(addressName, plz, cityName);

        return new Success<Address>(address);
    }
}
