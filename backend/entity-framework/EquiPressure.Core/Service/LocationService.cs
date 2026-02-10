namespace EquiPressure.Core.Service;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OneOf.Types;
using Model;

using GetCityAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<Model.City[]>, OneOf.Types.Error>;
using AddAddressAsyncResult
    = OneOf.OneOf<OneOf.Types.Success<Model.Address>,
        IBaseService.InvalidData, OneOf.Types.Error>;

public interface ILocationService
{
    public ValueTask<GetCityAsyncResult> GetCityAsync(int? length, string? nameFilter);
    public ValueTask<AddAddressAsyncResult> AddAddressAsync(string? street, int? houseNumber
                                                            , string cityName, string plz);
    
}

public class LocationService(EquiContext context) : ILocationService
{
    // yo warum geht das nicht mit IsolationLevel.Snapshot als Parameter
    private Task<IDbContextTransaction> BeginTransactionAsync() 
        => context.Database.BeginTransactionAsync();

    private async ValueTask CommitAsync()
    {
        await context.SaveChangesAsync();
        await context.Database.CommitTransactionAsync();
    }

    private Task RollbackAsync() => context.Database.RollbackTransactionAsync();
    
    public async ValueTask<GetCityAsyncResult> GetCityAsync(int? length, string? nameFilter)
    {
        var result = context.Addresses
                            .GroupBy(a => a.City)
                            .Select(g => new
                            {
                                city = g.Key,
                                count = g.Sum(a => a.Persons.Count)
                            });
        if (nameFilter != null)
        {
            result = result.Where(g => g.city.Name.ToLower()
                               .Contains(nameFilter.ToLower()));
            
        }

        result = result.OrderBy(r => r.count);
        
        if (length != null)
        {
            result = result.Take(length.Value);
        }

        var res = await result.Select(r => r.city).ToArrayAsync();

        return res.Any() ? new Success<City[]>(res) 
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
