using EquiApi.Core.Services;
using EquiApi.Persistence.Model;
using FluentValidation;

namespace EquiApi.Dtos;

/// <summary>
///     Request to add an address or to set the address of a person or horse
/// </summary>
/// <param name="AddressName">Optional street and house number</param>
/// <param name="CityName">The name of the city</param>
/// <param name="PLZ">The postal code</param>
public sealed record AddressRequest(string? AddressName, string CityName, string PLZ)
{
    public AddressData ToData() => new(AddressName, PLZ, CityName);

    public sealed class Validator : AbstractValidator<AddressRequest>
    {
        public Validator()
        {
            RuleFor(x => x.AddressName).NotEmpty().When(x => x.AddressName is not null);
            RuleFor(x => x.CityName).NotEmpty();
            RuleFor(x => x.PLZ).NotEmpty();
        }
    }
}

/// <summary>
///     An address
/// </summary>
/// <param name="Id">The id of the address</param>
/// <param name="AddressName">Optional street and house number</param>
/// <param name="CityName">The name of the city</param>
/// <param name="PLZ">The postal code</param>
public sealed record AddressDto(int Id, string? AddressName, string CityName, string PLZ)
{
    public static AddressDto FromAddress(Address address) =>
        new(address.Id, address.AddressName, address.CityName, address.PLZ);
}

/// <summary>
///     A city that is used by at least one address
/// </summary>
/// <param name="CityName">The name of the city</param>
/// <param name="PLZ">The postal code</param>
public sealed record CityDto(string CityName, string PLZ)
{
    public static CityDto FromCity(CityData city) => new(city.CityName, city.PLZ);
}