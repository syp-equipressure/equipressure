using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace EquiApi.Persistence.Repositories;

public interface ILocationRepository
{
    /// <summary>
    ///     Gets the cities that are used by addresses, most used cities first
    /// </summary>
    /// <param name="length">Optional maximum number of cities to return</param>
    /// <param name="nameFilter">Optional case-insensitive filter for the city name</param>
    /// <returns>The matching cities</returns>
    public ValueTask<IReadOnlyCollection<CityData>> GetCitiesAsync(int? length, string? nameFilter);

    /// <summary>
    ///     Gets an address
    /// </summary>
    /// <param name="addressId">The id of the address</param>
    /// <returns>The address or null if it does not exist</returns>
    public ValueTask<Address?> GetAddressByIdAsync(int addressId);

    /// <summary>
    ///     Finds an address by its values, city name and address name are compared case-insensitive
    /// </summary>
    /// <param name="addressName">Optional street and house number</param>
    /// <param name="plz">The postal code</param>
    /// <param name="cityName">The name of the city</param>
    /// <returns>The tracked address or null if no matching address exists</returns>
    public ValueTask<Address?> FindAddressAsync(string? addressName, string plz, string cityName);

    /// <summary>
    ///     Adds a new address
    /// </summary>
    /// <param name="address">The address to add</param>
    public void AddAddress(Address address);
}

internal sealed class LocationRepository(DbSet<Address> addressSet) : ILocationRepository
{
    public async ValueTask<IReadOnlyCollection<CityData>> GetCitiesAsync(int? length, string? nameFilter)
    {
        IQueryable<Address> addresses = addressSet.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(nameFilter))
        {
            string filter = nameFilter.ToLower();
            addresses = addresses.Where(a => a.CityName.ToLower().Contains(filter));
        }

        // a city can have several addresses, so the usages (persons and horses) of all its addresses are summed up
        var usagesPerAddress = addresses.Select(a => new
        {
            a.CityName,
            a.PLZ,
            Usages = a.Persons.Count + a.Horses.Count
        });

        var cities = usagesPerAddress.GroupBy(a => new { a.CityName, a.PLZ })
                                     .Select(g => new { g.Key.CityName, g.Key.PLZ, Usages = g.Sum(a => a.Usages) })
                                     .OrderByDescending(c => c.Usages)
                                     .ThenBy(c => c.CityName)
                                     .Select(c => new CityData(c.CityName, c.PLZ));

        if (length.HasValue)
        {
            cities = cities.Take(length.Value);
        }

        return await cities.ToListAsync();
    }

    public async ValueTask<Address?> GetAddressByIdAsync(int addressId) =>
        await addressSet.AsNoTracking()
                        .FirstOrDefaultAsync(a => a.Id == addressId);

    public async ValueTask<Address?> FindAddressAsync(string? addressName, string plz, string cityName)
    {
        string normalizedAddressName = addressName?.ToLower() ?? string.Empty;
        string normalizedCityName = cityName.ToLower();

        return await addressSet.FirstOrDefaultAsync(a => a.PLZ == plz
                                                         && a.CityName.ToLower() == normalizedCityName
                                                         && (a.AddressName ?? string.Empty).ToLower()
                                                         == normalizedAddressName);
    }

    public void AddAddress(Address address)
    {
        addressSet.Add(address);
    }
}