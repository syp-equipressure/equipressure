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
        var grouped = addressSet
                      .AsNoTracking()
                      .Select(a => new
                      {
                          a.Id,
                          a.CityName,
                          a.PLZ,
                          Cnt = a.Persons.Count + a.Horses.Count
                      })
                      .GroupBy(x => new { x.CityName, x.PLZ })
                      .Select(g => new
                      {
                          g.Key.CityName,
                          Count = g.Sum(x => x.Cnt),
                          RepresentativeId = g.Min(x => x.Id)
                      });

        if (nameFilter != null)
        {
            var filter = nameFilter.ToLower();
            grouped = grouped.Where(g => g.CityName.ToLower().Contains(filter));
        }

        IQueryable<int> ids = grouped
                              .OrderBy(g => g.Count)
                              .Select(g => g.RepresentativeId);

        if (length != null)
        {
            ids = ids.Take(length.Value);
        }

        // Two simple queries are more robust than a Join on a grouped subquery
        var idList = await ids.ToListAsync();
        return await addressSet
                     .AsNoTracking()
                     .Where(a => idList.Contains(a.Id))
                     .ToListAsync();
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
