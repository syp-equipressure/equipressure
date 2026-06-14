namespace EquiApi.Core.Util;

public class Helper
{
    public static void UpdateIfNotNull<T>(T? value, Action<T> setter)
    {
        if (value is not null)
        {
            setter(value);
        }
    }
    
    public static void UpdateIfHasValue<T>(T? value, Action<T> setter) where T : struct
    {
        if (value.HasValue)
        {
            setter(value.Value);
        }
    }
    
    /// <summary>
    /// DTO for equestrian
    /// </summary>
    /// <param name="Id">Id of the person.</param>
    /// <param name="FirstName">First name of the person.</param>
    /// <param name="LastName">Last name of the person</param>
    /// <param name="Height">Height of the person</param>
    /// <param name="Weight">Weight of the person</param>
    /// <param name="Email">optional email of the person</param>
    /// <param name="Street">Street of the person</param>
    /// <param name="HouseNumber">HouseNumber of the person</param>
    /// <param name="City">City name of the person</param>
    /// <param name="PLZ">PLZ of the person</param>
    public sealed record EquestrianBasicDto(
        int Id,
        string FirstName,
        string LastName,
        decimal Height,
        decimal Weight,
        string? Email,
        string? AddressName,
        string? CityName,
        string? PLZ)
    {
        public static EquestrianBasicDto FromEquestrianBasicData(EquestrianBasicData data, int id) =>
            new(id, data.FirstName, data.LastName, data.Height, data.Weight, data.Email, data.AddressName, data.CityName, data.PLZ);
    }

    /// <summary>
    /// DTO for a saddler
    /// </summary>
    /// <param name="Id">Id of the person.</param>
    /// <param name="FirstName">First name of the person.</param>
    /// <param name="LastName">Last name of the person</param>
    /// <param name="Link">optional websitelink but only allowed for saddlers</param>
    /// <param name="Description">optional description but only for saddlers.</param>
    /// <param name="PLZ">PLZ of the person</param>
    public sealed record SaddlerBasicDto(
        int Id,
        string FirstName,
        string LastName,
        string? AddressName,
        string CityName,
        string PLZ,
        string? Link,
        string? Description)
    {
        public static SaddlerBasicDto FromSaddlerBasicData(SaddlerBasicData data) =>
            new(data.Id, data.FirstName, data.LastName, data.AddressName, data.CityName, data.PLZ, data.Link,
                data.Description);
    }

    /// <summary>
    /// DTO that returns list of saddler dtos
    /// </summary>
    /// <param name="Saddlers">List of saddlers</param>
    public sealed record SaddlersListResponse(IEnumerable<SaddlerBasicDto> Saddlers)
    {
        public static SaddlersListResponse FromSaddlers(IEnumerable<SaddlerBasicDto> saddlers) => new(saddlers);
    }
    
    public record EquestrianBasicData(
        string FirstName,
        string LastName,
        string? AddressName,
        string CityName,
        string PLZ,
        string Email,
        decimal Height,
        decimal Weight);

    public record SaddlerBasicData(
        int Id,
        string FirstName,
        string LastName,
        string? AddressName,
        string CityName,
        string PLZ,
        string? Link,
        string? Description);
    
    public record NameData(string FirstName, string LastName);
}
