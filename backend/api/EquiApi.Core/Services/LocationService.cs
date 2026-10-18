using EquiApi.Persistence.Model;
using EquiApi.Persistence.Repositories;
using EquiApi.Persistence.Util;
using OneOf;
using OneOf.Types;

namespace EquiApi.Core.Services;

/// <summary>
///     The values of an address
/// </summary>
/// <param name="AddressName">Optional street and house number</param>
/// <param name="PLZ">The postal code</param>
/// <param name="CityName">The name of the city</param>
public sealed record AddressData(string? AddressName, string PLZ, string CityName);

public interface ILocationService
{
    /// <summary>
    ///     Gets the cities that are used by addresses, most used cities first
    /// </summary>
    /// <param name="length">Optional maximum number of cities to return</param>
    /// <param name="nameFilter">Optional case-insensitive filter for the city name</param>
    /// <returns>The matching cities, which may be empty</returns>
    public ValueTask<IReadOnlyCollection<CityData>> GetCitiesAsync(int? length, string? nameFilter);

    /// <summary>
    ///     Gets an address
    /// </summary>
    /// <param name="addressId">The id of the address</param>
    /// <returns>The address or <see cref="NotFound" /> if it does not exist</returns>
    public ValueTask<OneOf<Success<Address>, NotFound>> GetAddressByIdAsync(int addressId);

    /// <summary>
    ///     Adds an address if it does not exist yet
    /// </summary>
    /// <param name="address">The values of the new address</param>
    /// <returns>The new address or <see cref="AddressAlreadyExists" /> if the same address already exists</returns>
    public ValueTask<OneOf<Success<Address>, AddressAlreadyExists>> AddAddressAsync(AddressData address);

    /// <summary>
    ///     Returned if an address with the same values already exists
    /// </summary>
    /// <param name="ExistingAddressId">The id of the existing address</param>
    public readonly record struct AddressAlreadyExists(int ExistingAddressId);
}

internal sealed class LocationService(IUnitOfWork uow, ILogger<LocationService> logger) : ILocationService
{
    public async ValueTask<IReadOnlyCollection<CityData>> GetCitiesAsync(int? length, string? nameFilter) =>
        await uow.LocationRepository.GetCitiesAsync(length, nameFilter);

    public async ValueTask<OneOf<Success<Address>, NotFound>> GetAddressByIdAsync(int addressId)
    {
        var address = await uow.LocationRepository.GetAddressByIdAsync(addressId);
        if (address is null)
        {
            logger.LogInformation("Address {AddressId} not found", addressId);

            return new NotFound();
        }

        return new Success<Address>(address);
    }

    public async ValueTask<OneOf<Success<Address>, ILocationService.AddressAlreadyExists>> AddAddressAsync(
        AddressData address)
    {
        var existingAddress = await uow.LocationRepository.FindAddressAsync(address.AddressName, address.PLZ,
                                                                            address.CityName);
        if (existingAddress is not null)
        {
            logger.LogWarning("Address {AddressId} already exists", existingAddress.Id);

            return new ILocationService.AddressAlreadyExists(existingAddress.Id);
        }

        var newAddress = new Address
        {
            AddressName = address.AddressName,
            PLZ = address.PLZ,
            CityName = address.CityName
        };

        uow.LocationRepository.AddAddress(newAddress);
        await uow.SaveChangesAsync();
        logger.LogInformation("Address {AddressId} added", newAddress.Id);

        return new Success<Address>(newAddress);
    }
}

internal static class LocationRepositoryExtensions
{
    extension(ILocationRepository repository)
    {
        /// <summary>
        ///     Returns the existing address with the given values or adds a new one (without saving)
        /// </summary>
        public async ValueTask<Address> FindOrAddAddressAsync(AddressData address)
        {
            var existingAddress = await repository.FindAddressAsync(address.AddressName, address.PLZ,
                                                                    address.CityName);
            if (existingAddress is not null)
            {
                return existingAddress;
            }

            var newAddress = new Address
            {
                AddressName = address.AddressName,
                PLZ = address.PLZ,
                CityName = address.CityName
            };
            repository.AddAddress(newAddress);

            return newAddress;
        }
    }
}