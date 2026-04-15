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
    // TODO: xml doc
    public ValueTask<GetCityAsyncResult> GetCityAsync(int? length, string? nameFilter);

    public ValueTask<AddAddressAsyncResult> AddAddressAsync(string? street, int? houseNumber
                                                            , string cityName, string plz);
}

public class LocationService(IUnitOfWork uow, ILogger<LocationService> logger) : ILocationService
{
    public async ValueTask<GetCityAsyncResult> GetCityAsync(int? length, string? nameFilter)
    {
        IReadOnlyCollection<City> res = await uow.LocationRepository.GetCityAsync(length, nameFilter);

        if (!res.Any())
        {
            // wieso error??
            logger.LogWarning("no cities");

            return new Error();
        }

        logger.LogInformation("list of cities");

        return new Success<IReadOnlyCollection<City>>(res);
    }

    public async ValueTask<AddAddressAsyncResult> AddAddressAsync(string? street, int? houseNumber,
                                                                  string cityName, string plz)
    {
        var newCity = new City
        {
            PLZ = plz,
            Name = cityName
        };

        var newAddress = new Address
        {
            Street = street,
            HouseNumber = houseNumber,
            CityId = newCity.Id,
            City = newCity
        };

        uow.LocationRepository.AddCity(newCity);
        uow.LocationRepository.AddAddress(newAddress);
        await uow.SaveChangesAsync();
        
        logger.LogInformation("Successfully added Address");
        return new Success<Address>(newAddress);
    }
}
