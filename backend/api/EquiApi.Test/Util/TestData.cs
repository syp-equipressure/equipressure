using EquiApi.Core.Services;
using EquiApi.Persistence.Model;
using EquiApi.Shared;
using NSubstitute;

namespace EquiApi.Test.Util;

/// <summary>
///     Creates entities and service inputs with sensible defaults, override only what is relevant for a test
/// </summary>
internal static class TestData
{
    /// <summary>
    ///     The current date of <see cref="CreateClock" />
    /// </summary>
    public static readonly LocalDate Today = new(2026, 1, 1);

    public static IClock CreateClock()
    {
        var clock = Substitute.For<IClock>();
        clock.GetCurrentInstant().Returns(Today.At(new LocalTime(14, 30)).InZoneLeniently(Const.TimeZone).ToInstant());

        return clock;
    }

    public static Address CreateAddress(int id = 1, string? addressName = null, string plz = "4400",
                                        string cityName = "Steyr") => new()
                                        {
                                            Id = id,
                                            AddressName = addressName,
                                            PLZ = plz,
                                            CityName = cityName
                                        };

    public static AddressData CreateAddressData(string? addressName = null, string plz = "4400",
                                                string cityName = "Steyr") => new(addressName, plz, cityName);

    public static AccountRole CreateRole(RoleName name = RoleName.Equestrian) => new()
    {
        Id = (int) name,
        Name = name
    };

    /// <summary>
    ///     Creates a person, a saddler gets an address with street by default
    /// </summary>
    public static Person CreatePerson(int id = 1, string firstName = "Max", string lastName = "Mustermann",
                                      string? email = "max@example.com", Address? address = null,
                                      RoleName? role = null)
    {
        address ??= CreateAddress(addressName: role == RoleName.Saddler ? "Werkstattgasse 1" : null);
        var person = new Person
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            Height = 180M,
            Weight = 75M,
            DateOfBirth = new LocalDate(1990, 1, 1),
            Email = email,
            AddressId = address.Id,
            Address = address
        };

        if (role is not null)
        {
            var accountRole = CreateRole(role.Value);
            person.Roles = [new PersonRoleAssignment { PersonId = id, RoleId = accountRole.Id, Role = accountRole }];
        }

        return person;
    }

    public static Horse CreateHorse(int id = 1, string name = "Hugo") => new()
    {
        Id = id,
        Name = name,
        DateOfBirth = new LocalDate(2015, 1, 1),
        Weight = 500M,
        Height = 170M,
        Gender = HorseGender.Male,
        AddressId = 1,
        Address = CreateAddress()
    };

    public static Breed CreateBreed(int id = 1, string name = "Haflinger") => new()
    {
        Id = id,
        Name = name
    };

    public static Saddle CreateSaddle(int id = 1, int horseId = 1) => new()
    {
        Id = id,
        Name = "Dressage Saddle",
        HorseId = horseId,
        CategoryId = 1,
        Category = new SaddleCategory { Id = 1, Name = "Dressage" }
    };

    public static MeasurementDevice CreateDevice(string id = "device-1", int ownerId = 1, int numOfAllowedPeople = 5,
                                                 params int[] userIds) => new()
                                                 {
                                                     Id = id,
                                                     OwnerId = ownerId,
                                                     Owner = CreatePerson(ownerId),
                                                     CategoryId = 1,
                                                     Category = new DeviceCategory { Id = 1, Name = "Standard", NumOfAllowedPeople = numOfAllowedPeople },
                                                     Users = userIds.Select(userId => new DeviceUser { DeviceId = id, UserId = userId }).ToList()
                                                 };

    public static NewPersonData CreateNewPersonData(string? email = "max@example.com",
                                                    RoleName role = RoleName.Equestrian) =>
        new("Max", "Mustermann", 180M, 75M, new LocalDate(1990, 1, 1), email, null, null, CreateAddressData(), role);

    public static NewHorseData CreateNewHorseData(int ownerId = 1, params int[] breedIds) =>
        new("Hugo", new LocalDate(2015, 1, 1), 500M, 170M, HorseGender.Male, CreateAddressData(),
            breedIds.Length > 0 ? breedIds : [1], ownerId);
}