using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace EquiApi.Persistence.Repositories;

public interface ILocationRepository
{
    // TODO: xml doc
    public ValueTask<IReadOnlyCollection<City>> GetCityAsync(int? length, string? nameFilter);
    public ValueTask<bool> AddressExists(string? street, int? houseNumber);
    public ValueTask<bool> CityExists(string name, string plz);
    public void AddAddress(Address address);
    public void AddCity(City city);
}

public class LocationRepository(DbSet<Address> addressSet, DbSet<City> citySet) : ILocationRepository
{
    public async ValueTask<IReadOnlyCollection<City>> GetCityAsync(int? length, string? nameFilter)
    {
        var result = addressSet
                     .Include(a => a.City)
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

    public async ValueTask<bool> AddressExists(string? street, int? houseNumber)
    {
        return await addressSet.AnyAsync(a => a.Street == street && a.HouseNumber == houseNumber);
    }

    public async ValueTask<bool> CityExists(string name, string plz)
    {
        return await citySet.AnyAsync(c => c.Name.ToLower() == name.ToLower() && c.PLZ.ToLower() == plz.ToLower());
    }

    public void AddAddress(Address address)
    {
        addressSet.Add(address);
    }

    public void AddCity(City city)
    {
        citySet.Add(city);
    }
}
