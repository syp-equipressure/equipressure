using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace EquiApi.Persistence.Repositories;

/// <summary>
/// Provides direct data access to location entities, including cities and addresses.
/// </summary>
public interface ILocationRepository
{
    /// <summary>
    /// Retrieves a collection of cities, optionally filtered by name and limited by a maximum count.
    /// </summary>
    /// <param name="length">The maximum number of city records to return.</param>
    /// <param name="nameFilter">The string to filter city names by.</param>
    /// <returns>A collection of <see cref="City"/> entities.</returns>
    public ValueTask<IReadOnlyCollection<City>> GetCityAsync(int? length, string? nameFilter);

    /// <summary>
    /// Checks if an address with the specified street and house number already exists in the database.
    /// </summary>
    /// <param name="street">The name of the street to check.</param>
    /// <param name="houseNumber">The house number to check.</param>
    /// <returns>The <see cref="Address"/> entity if found; otherwise, <see langword="null"/>.</returns>
    public ValueTask<Address?> AddressExists(string? street, int? houseNumber);

    /// <summary>
    /// Checks if a city with the specified name and postal code already exists in the database.
    /// </summary>
    /// <param name="name">The name of the city to check.</param>
    /// <param name="plz">The postal code of the city to check.</param>
    /// <returns>The <see cref="City"/> entity if found; otherwise, <see langword="null"/>.</returns>
    public ValueTask<City?> CityExists(string name, string plz);

    /// <summary>
    /// adds a new address entity.
    /// </summary>
    /// <param name="address">The address entity to add.</param>
    public void AddAddress(Address address);

    /// <summary>
    /// adds a new city entity.
    /// </summary>
    /// <param name="city">The city entity to add.</param>
    public void AddCity(City city);
}

public class LocationRepository(DbSet<Address> addressSet, DbSet<City> citySet) : ILocationRepository
{
    public async ValueTask<IReadOnlyCollection<City>> GetCityAsync(int? length, string? nameFilter)
    {
        var result = addressSet
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

    public async ValueTask<Address?> AddressExists(string? street, int? houseNumber) =>
        await addressSet.FirstOrDefaultAsync(a => a.Street == street && a.HouseNumber == houseNumber);

    public async ValueTask<City?> CityExists(string name, string plz) =>
        await citySet.FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower() &&
                                               c.PLZ.ToLower() == plz.ToLower());

    public void AddAddress(Address address)
    {
        addressSet.Add(address);
    }

    public void AddCity(City city)
    {
        citySet.Add(city);
    }
}
