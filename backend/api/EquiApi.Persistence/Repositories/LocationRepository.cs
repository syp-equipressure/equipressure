using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace EquiApi.Persistence.Repositories;

/// <summary>
/// Provides direct data access to location entities, including cities and addresses.
/// </summary>
public interface ILocationRepository
{
    public ValueTask<IReadOnlyCollection<Address>> GetCitiesAsync(int? length, string? nameFilter, bool tracking);
    public ValueTask<bool> AddressExists(string? addressName, string plz, string cityName);
    public void AddAddress(Address address);
}

public class LocationRepository(DbSet<Address> addressSet) : ILocationRepository
{
    private IQueryable<Address> Addresses => addressSet;
    private IQueryable<Address> AddressesNoTracking => Addresses.AsNoTracking();

    public async ValueTask<IReadOnlyCollection<Address>> GetCitiesAsync(int? length, string? nameFilter, bool tracking)
    {
        var source = tracking ? AddressesNoTracking : Addresses;

        var result = source
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
        return await AddressesNoTracking.AnyAsync(a =>
            a.CityName.ToLower() == cityName.ToLower() &&
            a.PLZ.ToLower() == plz.ToLower() &&
            a.AddressName == addressName);
    }

    public void AddAddress(Address address)
    {
        addressSet.Add(address);
    }

}

