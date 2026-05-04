using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using EquiPressure.Core.Service;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using OneOf.Types;

namespace EquiApi.Core.Services;

using GetCityAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<IReadOnlyCollection<City>>, OneOf.Types.None>;
using AddAddressAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<Address>, IBaseService.Conflict>;

public interface ILocationService
{
    /// <summary>
    /// retrieves a collection of cities, optionally filtered by name and limited by a maximum count.
    /// </summary>
    /// <param name="length">The maximum number of city records to return.</param>
    /// <param name="nameFilter">The string to filter city names by (case-insensitive).</param>
    /// <returns>
    /// <see cref="GetCityAsyncResult"/> which holds either a success with the collection of cities or a none result.
    /// </returns>
    public ValueTask<GetCityAsyncResult> GetCityAsync(int? length, string? nameFilter);

    
    /// <summary>
    /// adds a new address or retrieves an existing one. Automatically handles city creation if it does not exist.
    /// </summary>
    /// <param name="street">The name of the street.</param>
    /// <param name="houseNumber">The house or building number.</param>
    /// <param name="cityName">The name of the city associated with the address.</param>
    /// <param name="plz">The postal code (Postleitzahl) of the city.</param>
    /// <returns>
    /// <see cref="AddAddressAsyncResult"/> which holds the successfully processed address, or a conflict.
    /// </returns>
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
            logger.LogWarning("No cities found");

            return new None();
        }

        logger.LogInformation("Successfully got list of cities");

        return new Success<IReadOnlyCollection<City>>(res);
    }

    public async ValueTask<AddAddressAsyncResult> AddAddressAsync(string? street, int? houseNumber,
                                                                  string cityName, string plz)
    {
        var city = await uow.LocationRepository.CityExists(cityName, plz);

        if (city == null)
        {
            logger.LogWarning("City doesnt exist");

            return new IBaseService.Conflict();
        }

        var existingAddress = await uow.LocationRepository.AddressExists(street, houseNumber);

        if (existingAddress != null && existingAddress.CityId == city.Id)
        {
            logger.LogInformation("Address already exists");

            return new IBaseService.Conflict();
        }

        var address = new Address
        {
            Street = street,
            HouseNumber = houseNumber,
            City = city,
            CityId = city.Id
        };

        uow.LocationRepository.AddAddress(address);
        await uow.SaveChangesAsync();
        logger.LogInformation("Successfully added address");

        return new Success<Address>(address);
    }
}
