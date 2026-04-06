using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using EquiPressure.Core.Service;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using OneOf.Types;

namespace EquiApi.Core.Services;

using GetCityAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<IReadOnlyCollection<City>>, OneOf.Types.Error>;
using AddAddressAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<Address>,
        IBaseService.InvalidData, OneOf.Types.Error>;

public interface ILocationService
{
    public ValueTask<GetCityAsyncResult> GetCityAsync(int? length, string? nameFilter);

    public ValueTask<AddAddressAsyncResult> AddAddressAsync(string? street, int? houseNumber
                                                            , string cityName, string plz);
}

public class LocationService(IUnitOfWork uow) : ILocationService
{
    public async ValueTask<GetCityAsyncResult> GetCityAsync(int? length, string? nameFilter)
    {
        var res = await uow.LocationRepository.GetCitiesAsync(length, nameFilter, true);

        return res.Any()
            ? new Success<IReadOnlyCollection<City>>(res)
            : new Error();
    }

    public async ValueTask<AddAddressAsyncResult> AddAddressAsync(string? street, int? houseNumber,
                                                                  string cityName, string plz)
    {
        var city = await uow.LocationRepository.CityExists(cityName, plz, false) 
                   ?? uow.LocationRepository.AddCityAsync(cityName, plz);

        var address = await uow.LocationRepository.AddressExists(street, houseNumber, false) 
                      ?? uow.LocationRepository.AddAddressAsync(street, houseNumber, city);

        return new Success<Address>(address);
    }
}
