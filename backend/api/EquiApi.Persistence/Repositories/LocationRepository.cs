using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace EquiApi.Persistence.Repositories;

public interface ILocationRepository
{
    /// <summary>
    /// Get all cities in the database ordered alphabetically
    /// </summary>
    /// <param name="maxNumOfCities">the maximum number of cities to return</param>
    /// <param name="cityNameFilter">a filter that checks whether a city name contains the given characters</param>
    /// <param name="tracking">flag to indicate whether the entities are returned tracked or not tracked</param>
    /// <returns>A read-only collection of City objects</returns>
    public ValueTask<IReadOnlyCollection<City>>
        GetCitiesAsync(int? maxNumOfCities, string? cityNameFilter, bool tracking);

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

    public async ValueTask<IReadOnlyCollection<City>> GetCitiesAsync(int? maxNumOfCities, string? cityNameFilter,
                                                                     bool tracking)
    {
        IQueryable<City> source = tracking ? Cities : CitiesNoTracking;
        if (cityNameFilter != null)
        {
            // ignores case sensitivity
            source = source.Where(c => c.Name.Contains(cityNameFilter, StringComparison.CurrentCultureIgnoreCase));
        }

        source = source.OrderBy(c => c.Name);

        if (maxNumOfCities != null)
        {
            source = source.Take(maxNumOfCities.Value);
        }

        return await source.ToListAsync();
    }

    public async ValueTask<Address?> AddressExists(string? street, int? houseNumber, bool tracking)
    {
        var source = tracking ? AddressesNoTracking : Addresses;

        return await source.FirstOrDefaultAsync(a => a.Street == street && a.HouseNumber == houseNumber);
    }

    public async ValueTask<City?> CityExists(string name, string plz, bool tracking)
    {
        var source = tracking ? CitiesNoTracking : Cities;

        return await source.FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower() &&
                                                     c.PLZ.ToLower() == plz.ToLower());
    }

    public Address AddAddressAsync(string? street, int? houseNumber, City city)
    {
        var address = new Address
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
        var city = new City
        {
            Name = name,
            PLZ = plz
        };
        citySet.Add(city);

        return city;
    }
}
