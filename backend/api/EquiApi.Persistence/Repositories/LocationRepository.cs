using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using OneOf.Types;

namespace EquiApi.Persistence.Repositories;

/// <summary>
/// Provides direct data access to location entities, including cities and addresses.
/// </summary>
public interface ILocationRepository
{
    /// <summary>
    /// Retrieves a filtered list of cities/addresses.
    /// </summary>
    /// <param name="length">optional number of cities to return</param>
    /// <param name="nameFilter">optional name filter for city name</param>
    /// <returns>
    /// <item><description><see cref="Success{T}"/> readonly collection of <see cref="Address"/> entities</description></item>
    /// <item><description>or <see cref="NotFound"/> if no cities exist.</description></item>
    /// </returns>
    public ValueTask<IReadOnlyCollection<Address>> GetCitiesAsync(int? length, string? nameFilter);
    
    /// <summary>
    /// checks if address exists
    /// </summary>
    /// <param name="addressName">optional adressname</param>
    /// <param name="plz">optional plz</param>
    /// <param name="cityName">optional cityName</param>
    /// <returns>
    /// <item><description><see cref="bool"/> true if address exists, false otherwise</description></item>
    /// </returns>
    public ValueTask<bool> AddressExists(string? addressName, string plz, string cityName);
    
    /// <summary>
    /// adds address to addressset
    /// </summary>
    /// <param name="address">address entity to be added</param>
    public void AddAddress(Address address);
}

public class LocationRepository(DbSet<Address> addressSet) : ILocationRepository
{ 
    public async ValueTask<IReadOnlyCollection<Address>> GetCitiesAsync(int? length, string? nameFilter)
    {
        var result = addressSet
            .GroupBy(a => new { a.CityName, Plz = a.PLZ })
            .Select(g => new
            {
                CityName = g.Key.CityName,
                Plz = g.Key.Plz,
                Count = g.Sum(a => a.Persons.Count + a.Horses.Count),
                Representative = g.First()
            });

        if (nameFilter != null)
        {
            result = result.Where(g => g.CityName.ToLower().Contains(nameFilter.ToLower()));
        }

        result = result.OrderBy(r => r.Count);

        if (length != null)
        {
            result = result.Take(length.Value);
        }

        return await result.Select(r => r.Representative).ToListAsync();
    }

    public async ValueTask<bool> AddressExists(string? addressName, string plz, string cityName)
    {
        return await addressSet.AsNoTracking().AnyAsync(a =>
            a.CityName.ToLower() == cityName.ToLower() &&
            a.PLZ.ToLower() == plz.ToLower() &&
            a.AddressName == addressName);
    }

    public void AddAddress(Address address)
    {
        addressSet.Add(address);
    }

}

