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
    public ValueTask<GetCitiesAsyncResult> GetCitiesAsync(int? length, string? nameFilter);
    public ValueTask<AddAddressAsyncResult> AddAddressAsync(string? addressName, string plz, string cityName);

    public record AddressAlreadyExists();
}

public class LocationService(IUnitOfWork uow, ILogger<LocationService> logger) : ILocationService
{
    public async ValueTask<GetCitiesAsyncResult> GetCitiesAsync(int? length, string? nameFilter)
    {
        var res = await uow.LocationRepository.GetCitiesAsync(length, nameFilter, true);

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
