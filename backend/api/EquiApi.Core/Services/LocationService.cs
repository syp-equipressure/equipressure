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
        var res = await uow.LocationRepository.GetCityAsync(length, nameFilter, true);

        return res.Any() ? new Success<IReadOnlyCollection<City>>(res) 
            : new Error();
    }

    public async ValueTask<AddAddressAsyncResult> AddAddressAsync(string? street, int? houseNumber, 
                                                                  string cityName, string plz)
    {
        if (string.IsNullOrEmpty(cityName) || string.IsNullOrEmpty(plz))
        {
            return new IBaseService.InvalidData();
        }

        await BeginTransactionAsync();
        
        try
        {
            var city = await context.Cities.FirstOrDefaultAsync(c => c.Name == cityName && c.PLZ == plz) 
                       ?? new City
                       {
                           Name = cityName,
                           PLZ = plz
                       };
            
            await context.AddAsync(city);
            await context.SaveChangesAsync();

            var address = await context.Addresses.FirstOrDefaultAsync(a => a.Street == street
                                                                           && a.HouseNumber == houseNumber
                                                                           && a.CityId == city.Id)
                          ?? new Address
                          {
                              CityId = city.Id,
                              City = city,
                              Street = street,
                              HouseNumber = houseNumber
                          };
            
            city.Addresses.Add(address);
            await context.AddAsync(address);
            await CommitAsync();
            
            return new Success<Address>(address);
        }
        catch
        {
            await RollbackAsync();
            return new Error();
        }
    }
}
