using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace EquiApi.Persistence.Repositories;

public interface ILocationRepository
{
    public ValueTask<IReadOnlyCollection<City>> GetCityAsync(int? length, string? nameFilter, bool tracking);
    public ValueTask<Address?> AddressExists(string? street, int? houseNumber, bool tracking);
    public ValueTask<City?> CityExists(string name, string plz, bool tracking);
    public Address AddAddressAsync(string? street, int? houseNumber
                                                            , City city);
    public City AddCityAsync(string name, string plz);
}

public class LocationRepository(DbSet<Address> addressSet, DbSet<City> citySet) : ILocationRepository
{
    private IQueryable<Address> Addresses => addressSet;
    private IQueryable<City> Cities => citySet;
    private IQueryable<City> CitiesNoTracking => Cities.AsNoTracking();
    private IQueryable<Address> AddressesNoTracking => Addresses.AsNoTracking();
    
    public async ValueTask<IReadOnlyCollection<City>> GetCityAsync(int? length, string? nameFilter, bool tracking)
    {
        var source = tracking ? AddressesNoTracking : Addresses;
        var result = source
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

        return await result.Select(r => r.city).ToListAsync();
    }

    public async ValueTask<Address?> AddressExists(string? street, int? houseNumber, bool tracking)
    {
        var source = tracking ? AddressesNoTracking : Addresses;

        return await source.FirstOrDefaultAsync(a => a.Street == street && a.HouseNumber == houseNumber);
    }

    public async ValueTask<City?> CityExists(string name, string plz, bool tracking)
    {
        var source = tracking ? CitiesNoTracking : Cities;

        return await source.FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower() && c.PLZ.ToLower() == plz.ToLower());
    }

    public Address AddAddressAsync(string? street, int? houseNumber, City city)
    {
        var address =  new Address
                      {
                          CityId = city.Id,
                          City = city,
                          Street = street,
                          HouseNumber = houseNumber
                      };
        addressSet.Add(address);
        return address;
    }

    public City AddCityAsync(string name, string plz)
    {
        var city =  new City
                   {
                       Name = name,
                       PLZ = plz
                   };
        citySet.Add(city);
        return city;
    }
}
