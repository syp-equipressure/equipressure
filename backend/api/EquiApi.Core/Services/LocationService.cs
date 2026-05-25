using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using EquiPressure.Core.Service;
using OneOf.Types;
using OneOf;

namespace EquiApi.Core.Services;

using GetCitiesAsyncResult
    = OneOf<Success<IReadOnlyCollection<Address>>, NotFound>;
using AddAddressAsyncResult
    = OneOf<Success<Address>,
        IBaseService.InvalidData, ILocationService.AddressAlreadyExists>;

public interface ILocationService
{
    /// <summary>
    /// gets cities that are filtered
    /// </summary>
    /// <param name="length">amount of cities returned</param>
    /// <param name="nameFilter">name filter for city names</param>
    /// <returns>
    /// <item><description><see cref="Success{T}"/> with a read only collection of <see cref="Address"/> entitys</description></item>
    /// <item><description>or <see cref="NotFound"/> when no cities are found.</description></item>
    /// </returns>
    public ValueTask<GetCitiesAsyncResult> GetCitiesAsync(int? length, string? nameFilter);
    
    /// <summary>
    /// adds a address if it doesnt already exist
    /// </summary>
    /// <param name="addressName">optional name of address</param>
    /// <param name="plz">plz</param>
    /// <param name="cityName">name of the city</param>
    /// <returns>
    /// <item><description><see cref="Success{T}"/> contains <see cref="Address"/> entity</description></item>
    /// <item><description>oder <see cref="IBaseService.InvalidData"/> if params are invalid data</description></item>
    /// <item><description>oder <see cref="AddressAlreadyExists"/>if address already exists</description></item>
    /// </returns>
    public ValueTask<AddAddressAsyncResult> AddAddressAsync(string? addressName, string plz, string cityName);

    /// <summary>
    /// covers the case, when adding address, if it already exists
    /// </summary>
    public record AddressAlreadyExists();
}

public class LocationService(IUnitOfWork uow, ILogger<LocationService> logger) : ILocationService
{
    public async ValueTask<GetCitiesAsyncResult> GetCitiesAsync(int? length, string? nameFilter)
    {
        var res = await uow.LocationRepository.GetCitiesAsync(length, nameFilter);

        if (!res.Any())
        {
            logger.LogWarning("GetCitiesAsync returned no results for length={Length}, nameFilter={NameFilter}", length, nameFilter);
            return new NotFound();
        }
        
        return new Success<IReadOnlyCollection<Address>>(res);
    }

    public async ValueTask<AddAddressAsyncResult> AddAddressAsync(string? addressName, string plz, string cityName)
    {
        

        if (await uow.LocationRepository.AddressExists(addressName, plz, cityName))
        {
            logger.LogWarning("AddAddressAsync — address already exists: addressName={AddressName}," +
                              " plz={PLZ}, cityName={CityName}",
                              addressName, plz, cityName);
            return new ILocationService.AddressAlreadyExists();
        }

        var address = new Address
        {
            AddressName = addressName,
            PLZ = plz,
            CityName = cityName
        };
        
        uow.LocationRepository.AddAddress(address);
        await uow.SaveChangesAsync();

        logger.LogInformation("AddAddressAsync — address successfully created with id={Id}", address.Id);
        return new Success<Address>(address);
    }
}
