namespace EquiApi.Persistence.Model;

/// <summary>
///     First and last name of a person
/// </summary>
public sealed record NameData(string FirstName, string LastName);

/// <summary>
///     A saddler with the publicly visible parts of their address
/// </summary>
public sealed record SaddlerData(
    int Id,
    string FirstName,
    string LastName,
    string? AddressName,
    string CityName,
    string PLZ,
    string? WebsiteLink,
    string? Description);

/// <summary>
///     A contact of a person: a saddler if the person is an equestrian, an equestrian if the person is a saddler.
///     Website link and description are only set for saddlers.
/// </summary>
public sealed record ContactData(
    int Id,
    string FirstName,
    string LastName,
    string? AddressName,
    string CityName,
    string PLZ,
    string? WebsiteLink,
    string? Description);

/// <summary>
///     A city that is used by at least one address
/// </summary>
public sealed record CityData(string CityName, string PLZ);