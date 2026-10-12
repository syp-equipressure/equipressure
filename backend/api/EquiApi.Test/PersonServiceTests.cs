using System.Runtime.CompilerServices;
using EquiApi.Core.Services;
using EquiApi.Core.Util;
using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using EquiPressure.Core.Service;
using NSubstitute;

namespace EquiApi.Test;

public sealed class PersonServiceTests
{
    private readonly IUnitOfWork _uowMock = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();
    private readonly ILogger<PersonService> _loggerMock = Substitute.For<ILogger<PersonService>>();
    private readonly PersonService _sut;

    public PersonServiceTests()
    {
        _sut = new PersonService(_uowMock, _dateTimeProviderMock, _loggerMock);

        _dateTimeProviderMock.GetCurrentDate().Returns(new LocalDate(2026, 1, 1));
    }

    /// <summary>
    /// Creates an instance without calling a constructor, so we don't have to know every required member.
    /// Only use it for objects that are just passed through the service (e.g. DTOs / list elements).
    /// </summary>
    private static T Uninit<T>() where T : class => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

    private static Address CreateAddress(int id = 1) => new()
    {
        Id = id,
        AddressName = null,
        PLZ = "4400",
        CityName = "Steyr",
        Persons = [],
        Horses = []
    };

    private static AccountRole CreateRole(int id = 1, RoleName name = RoleName.Equestrian) => new()
    {
        Id = id,
        Name = name
    };

    private static Person CreatePerson(int id = 1)
    {
        var person = Uninit<Person>();
        person.Id = id;
        person.FirstName = "Max";
        person.LastName = "Mustermann";
        person.Height = 180M;
        person.Weight = 75M;
        person.DateOfBirth = new LocalDate(1990, 1, 1);
        person.Email = "max@example.com";
        person.WebsiteLink = null;
        person.Description = null;
        person.Address = CreateAddress();
        person.Roles = [];
        return person;
    }

    private static Horse CreateHorse(int id = 1) => new()
    {
        Id = id,
        Name = "Hugo",
        DateOfBirth = new LocalDate(2015, 1, 1),
        Weight = 500M,
        Height = 170M,
        Gender = HorseGender.Male,
        AddressId = 1,
        Address = CreateAddress(),
        HorseBreeds = [],
        Persons = [],
        Saddles = [],
        MeasurementGroups = []
    };


    [Fact]
    public async Task GetPersonByIdAsync_PersonNotFound_ReturnsNotFound()
    {
        const int personId = 1;
        _uowMock.PersonRepository.GetPersonByIdAsync(personId).Returns((Person?)null);

        var result = await _sut.GetPersonByIdAsync(personId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task GetPersonByIdAsync_PersonExists_ReturnsPerson()
    {
        const int personId = 1;
        var person = CreatePerson(personId);
        _uowMock.PersonRepository.GetPersonByIdAsync(personId).Returns(person);

        var result = await _sut.GetPersonByIdAsync(personId);

        result.IsT0.Should().BeTrue();
        result.AsT0.Value.Should().Be(person);
    }


    [Fact]
    public async Task GetPersonAddressAsync_AddressNotFound_ReturnsNotFound()
    {
        const int personId = 1;
        _uowMock.PersonRepository.GetPersonAddressAsync(personId).Returns((Address?)null);

        var result = await _sut.GetPersonAddressAsync(personId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task GetPersonAddressAsync_AddressExists_ReturnsAddress()
    {
        const int personId = 1;
        var address = CreateAddress();
        _uowMock.PersonRepository.GetPersonAddressAsync(personId).Returns(address);

        var result = await _sut.GetPersonAddressAsync(personId);

        result.IsT0.Should().BeTrue();
        result.AsT0.Value.Should().Be(address);
    }


    [Fact]
    public async Task GetNameByIdAsync_PersonNotFound_ReturnsNotFound()
    {
        const int personId = 1;
        _uowMock.PersonRepository.GetNameByIdAsync(personId).Returns((Helper.NameData?)null);

        var result = await _sut.GetNameByIdAsync(personId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task GetNameByIdAsync_PersonExists_ReturnsNameData()
    {
        const int personId = 1;
        var nameData = Uninit<Helper.NameData>();
        _uowMock.PersonRepository.GetNameByIdAsync(personId).Returns(nameData);

        var result = await _sut.GetNameByIdAsync(personId);

        result.IsT0.Should().BeTrue();
        result.AsT0.Value.Should().Be(nameData);
    }


    [Fact]
    public async Task GetFavouritesAsync_PersonNotFound_ReturnsNotFound()
    {
        const int personId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(personId).Returns(false);

        var result = await _sut.GetFavouritesAsync(personId);

        result.IsT2.Should().BeTrue();
    }

    [Fact]
    public async Task GetFavouritesAsync_NoFavourites_ReturnsNone()
    {
        const int personId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(personId).Returns(true);
        _uowMock.PersonRepository.GetFavouritesAsync(personId).Returns(new List<Person>());

        var result = await _sut.GetFavouritesAsync(personId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task GetFavouritesAsync_HasFavourites_ReturnsFavourites()
    {
        const int personId = 1;
        var favourites = new List<Person> { CreatePerson(2), CreatePerson(3) };
        _uowMock.PersonRepository.PersonExistsAsync(personId).Returns(true);
        _uowMock.PersonRepository.GetFavouritesAsync(personId).Returns(favourites);

        var result = await _sut.GetFavouritesAsync(personId);

        result.IsT0.Should().BeTrue();
        result.AsT0.Value.Should().BeEquivalentTo(favourites);
    }

    // ---------------------------------------------------------------- GetContactsAsync

    [Fact]
    public async Task GetContactsAsync_PersonNotFound_ReturnsNotFound()
    {
        const int personId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(personId).Returns(false);

        var result = await _sut.GetContactsAsync(personId);

        result.IsT2.Should().BeTrue();
    }

    [Fact]
    public async Task GetContactsAsync_NoContacts_ReturnsNone()
    {
        const int personId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(personId).Returns(true);
        _uowMock.PersonRepository.GetContactsAsync(personId).Returns(new List<Person>());

        var result = await _sut.GetContactsAsync(personId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task GetContactsAsync_HasContacts_ReturnsContacts()
    {
        const int personId = 1;
        var contacts = new List<Person> { CreatePerson(2), CreatePerson(3) };
        _uowMock.PersonRepository.PersonExistsAsync(personId).Returns(true);
        _uowMock.PersonRepository.GetContactsAsync(personId).Returns(contacts);

        var result = await _sut.GetContactsAsync(personId);

        result.IsT0.Should().BeTrue();
        result.AsT0.Value.Should().BeEquivalentTo(contacts);
    }


    [Fact]
    public async Task GetOwnedHorsesAsync_PersonNotFound_ReturnsNotFound()
    {
        const int personId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(personId).Returns(false);

        var result = await _sut.GetOwnedHorsesAsync(personId);

        result.IsT2.Should().BeTrue();
    }

    [Fact]
    public async Task GetOwnedHorsesAsync_NoHorses_ReturnsNone()
    {
        const int personId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(personId).Returns(true);
        _uowMock.PersonRepository.GetOwnedHorsesAsync(personId).Returns(new List<Horse>());

        var result = await _sut.GetOwnedHorsesAsync(personId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task GetOwnedHorsesAsync_HasHorses_ReturnsHorses()
    {
        const int personId = 1;
        var horses = new List<Horse> { CreateHorse(1), CreateHorse(2) };
        _uowMock.PersonRepository.PersonExistsAsync(personId).Returns(true);
        _uowMock.PersonRepository.GetOwnedHorsesAsync(personId).Returns(horses);

        var result = await _sut.GetOwnedHorsesAsync(personId);

        result.IsT0.Should().BeTrue();
        result.AsT0.Value.Should().BeEquivalentTo(horses);
    }


    [Fact]
    public async Task GetAllDevicesByPersonAsync_PersonNotFound_ReturnsNotFound()
    {
        const int personId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(personId).Returns(false);

        var result = await _sut.GetAllDevicesByPersonAsync(personId);

        result.IsT2.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllDevicesByPersonAsync_NoDevices_ReturnsNone()
    {
        const int personId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(personId).Returns(true);
        _uowMock.PersonRepository.GetAllDevicesAsync(personId).Returns(new List<MeasurementDevice>());

        var result = await _sut.GetAllDevicesByPersonAsync(personId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllDevicesByPersonAsync_HasDevices_ReturnsDevices()
    {
        const int personId = 1;
        var devices = new List<MeasurementDevice> { Uninit<MeasurementDevice>(), Uninit<MeasurementDevice>() };
        _uowMock.PersonRepository.PersonExistsAsync(personId).Returns(true);
        _uowMock.PersonRepository.GetAllDevicesAsync(personId).Returns(devices);

        var result = await _sut.GetAllDevicesByPersonAsync(personId);

        result.IsT0.Should().BeTrue();
        result.AsT0.Value.Should().HaveCount(2);
        result.AsT0.Value.Should().BeEquivalentTo(devices);
    }


    [Fact]
    public async Task GetAllSaddlersAsync_NoSaddlers_ReturnsNone()
    {
        _uowMock.PersonRepository.GetAllSaddlersAsync().Returns(new List<Helper.SaddlerBasicData>());

        var result = await _sut.GetAllSaddlersAsync();

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllSaddlersAsync_HasSaddlers_ReturnsSaddlers()
    {
        var saddlers = new List<Helper.SaddlerBasicData>
        {
            Uninit<Helper.SaddlerBasicData>(), Uninit<Helper.SaddlerBasicData>()
        };
        _uowMock.PersonRepository.GetAllSaddlersAsync().Returns(saddlers);

        var result = await _sut.GetAllSaddlersAsync();

        result.IsT0.Should().BeTrue();
        result.AsT0.Value.Should().BeEquivalentTo(saddlers);
    }


    [Fact]
    public async Task GetAllSaddlerFavouritesAsync_EquestrianNotFound_ReturnsNotFound()
    {
        const int equestrianId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(equestrianId).Returns(false);

        var result = await _sut.GetAllSaddlerFavouritesAsync(equestrianId);

        result.IsT2.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllSaddlerFavouritesAsync_NoSaddlers_ReturnsNone()
    {
        const int equestrianId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(equestrianId).Returns(true);
        _uowMock.PersonRepository.GetAllSaddlerFavouritesOfEquestrianAsync(equestrianId)
                .Returns(new List<Helper.SaddlerBasicData>());

        var result = await _sut.GetAllSaddlerFavouritesAsync(equestrianId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllSaddlerFavouritesAsync_HasSaddlers_ReturnsSaddlers()
    {
        const int equestrianId = 1;
        var saddlers = new List<Helper.SaddlerBasicData> { Uninit<Helper.SaddlerBasicData>() };
        _uowMock.PersonRepository.PersonExistsAsync(equestrianId).Returns(true);
        _uowMock.PersonRepository.GetAllSaddlerFavouritesOfEquestrianAsync(equestrianId).Returns(saddlers);

        var result = await _sut.GetAllSaddlerFavouritesAsync(equestrianId);

        result.IsT0.Should().BeTrue();
        result.AsT0.Value.Should().BeEquivalentTo(saddlers);
    }


    [Fact]
    public async Task GetAllSaddlerContactsAsync_EquestrianNotFound_ReturnsNotFound()
    {
        const int equestrianId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(equestrianId).Returns(false);

        var result = await _sut.GetAllSaddlerContactsAsync(equestrianId);

        result.IsT2.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllSaddlerContactsAsync_NoSaddlers_ReturnsNone()
    {
        const int equestrianId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(equestrianId).Returns(true);
        _uowMock.PersonRepository.GetAllSaddlerContactsOfEquestrianAsync(equestrianId)
                .Returns(new List<Helper.SaddlerBasicData>());

        var result = await _sut.GetAllSaddlerContactsAsync(equestrianId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllSaddlerContactsAsync_HasSaddlers_ReturnsSaddlers()
    {
        const int equestrianId = 1;
        var saddlers = new List<Helper.SaddlerBasicData> { Uninit<Helper.SaddlerBasicData>() };
        _uowMock.PersonRepository.PersonExistsAsync(equestrianId).Returns(true);
        _uowMock.PersonRepository.GetAllSaddlerContactsOfEquestrianAsync(equestrianId).Returns(saddlers);

        var result = await _sut.GetAllSaddlerContactsAsync(equestrianId);

        result.IsT0.Should().BeTrue();
        result.AsT0.Value.Should().BeEquivalentTo(saddlers);
    }


    [Fact]
    public async Task AddPersonAsync_WithInvalidHeight_ReturnsInvalidData()
    {
        var result = await _sut.AddPersonAsync(
            "Max", "Mustermann", 0M, 75M, new LocalDate(1990, 1, 1),
            null, null, null, CreateAddress(), CreateRole()
        );

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task AddPersonAsync_WithInvalidWeight_ReturnsInvalidData()
    {
        var result = await _sut.AddPersonAsync(
            "Max", "Mustermann", 180M, -1M, new LocalDate(1990, 1, 1),
            null, null, null, CreateAddress(), CreateRole()
        );

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task AddPersonAsync_WithBirthDateInFuture_ReturnsInvalidData()
    {
        var result = await _sut.AddPersonAsync(
            "Max", "Mustermann", 180M, 75M, new LocalDate(2026, 5, 20),
            null, null, null, CreateAddress(), CreateRole()
        );

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task AddPersonAsync_WithBirthDateToday_ReturnsInvalidData()
    {
        var result = await _sut.AddPersonAsync(
            "Max", "Mustermann", 180M, 75M, new LocalDate(2026, 1, 1),
            null, null, null, CreateAddress(), CreateRole()
        );

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task AddPersonAsync_WithExistingEmail_ReturnsConflict()
    {
        const string email = "max@example.com";
        _uowMock.PersonRepository.PersonWithEmailExistsAsync(email).Returns(true);

        var result = await _sut.AddPersonAsync(
            "Max", "Mustermann", 180M, 75M, new LocalDate(1990, 1, 1),
            email, null, null, CreateAddress(), CreateRole()
        );

        result.IsT2.Should().BeTrue();
    }

    [Fact]
    public async Task AddPersonAsync_EquestrianWithWebsiteLink_ReturnsConflict()
    {
        var result = await _sut.AddPersonAsync(
            "Max", "Mustermann", 180M, 75M, new LocalDate(1990, 1, 1),
            null, "https://example.com", null, CreateAddress(), CreateRole(name: RoleName.Equestrian)
        );

        result.IsT2.Should().BeTrue();
    }

    [Fact]
    public async Task AddPersonAsync_EquestrianWithDescription_ReturnsConflict()
    {
        var result = await _sut.AddPersonAsync(
            "Max", "Mustermann", 180M, 75M, new LocalDate(1990, 1, 1),
            null, null, "Some description", CreateAddress(), CreateRole(name: RoleName.Equestrian)
        );

        result.IsT2.Should().BeTrue();
    }

    [Fact]
    public async Task AddPersonAsync_InvalidData_DoesNotSave()
    {
        await _sut.AddPersonAsync(
            "Max", "Mustermann", 0M, 75M, new LocalDate(1990, 1, 1),
            null, null, null, CreateAddress(), CreateRole()
        );

        _uowMock.PersonRepository.DidNotReceive().AddPerson(Arg.Any<Person>());
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AddPersonAsync_ValidEquestrian_ReturnsSuccess()
    {
        var address = CreateAddress();
        var role = CreateRole(name: RoleName.Equestrian);

        var result = await _sut.AddPersonAsync(
            "Max", "Mustermann", 180M, 75M, new LocalDate(1990, 1, 1),
            "max@example.com", null, null, address, role
        );

        result.IsT0.Should().BeTrue();
        result.AsT0.Value.FirstName.Should().Be("Max");
        result.AsT0.Value.LastName.Should().Be("Mustermann");
        result.AsT0.Value.Height.Should().Be(180M);
        result.AsT0.Value.Weight.Should().Be(75M);
        result.AsT0.Value.DateOfBirth.Should().Be(new LocalDate(1990, 1, 1));
        result.AsT0.Value.Email.Should().Be("max@example.com");
        result.AsT0.Value.Address.Should().Be(address);
        result.AsT0.Value.Roles.Should().ContainSingle();
        result.AsT0.Value.Roles.Single().Role.Should().Be(role);
        result.AsT0.Value.Roles.Single().RoleId.Should().Be(role.Id);
    }

    [Fact]
    public async Task AddPersonAsync_ValidSaddlerWithWebsiteAndDescription_ReturnsSuccess()
    {
        var role = CreateRole(2, RoleName.Saddler);

        var result = await _sut.AddPersonAsync(
            "Sabine", "Sattler", 170M, 65M, new LocalDate(1985, 5, 5),
            null, "https://sattler.example.com", "Custom saddles", CreateAddress(), role
        );

        result.IsT0.Should().BeTrue();
        result.AsT0.Value.WebsiteLink.Should().Be("https://sattler.example.com");
        result.AsT0.Value.Description.Should().Be("Custom saddles");
    }

    [Fact]
    public async Task AddPersonAsync_ValidData_AddsPersonAndSavesChanges()
    {
        var result = await _sut.AddPersonAsync(
            "Max", "Mustermann", 180M, 75M, new LocalDate(1990, 1, 1),
            null, null, null, CreateAddress(), CreateRole()
        );

        _uowMock.PersonRepository.Received(1).AddPerson(result.AsT0.Value);
        await _uowMock.Received(1).SaveChangesAsync();
    }

    // ---------------------------------------------------------------- UpdatePersonAsync

    [Fact]
    public async Task UpdatePersonAsync_PersonNotFound_ReturnsNotFound()
    {
        const int personId = 1;
        _uowMock.PersonRepository.GetPersonByIdAsync(personId).Returns((Person?)null);

        var result = await _sut.UpdatePersonAsync(
            personId, "New", null, null, null, null, null, null, null, null, null
        );

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePersonAsync_WithInvalidHeight_ReturnsInvalidData()
    {
        const int personId = 1;
        _uowMock.PersonRepository.GetPersonByIdAsync(personId).Returns(CreatePerson(personId));

        var result = await _sut.UpdatePersonAsync(
            personId, null, null, 0M, null, null, null, null, null, null, null
        );

        result.IsT2.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePersonAsync_WithInvalidWeight_ReturnsInvalidData()
    {
        const int personId = 1;
        _uowMock.PersonRepository.GetPersonByIdAsync(personId).Returns(CreatePerson(personId));

        var result = await _sut.UpdatePersonAsync(
            personId, null, null, null, -5M, null, null, null, null, null, null
        );

        result.IsT2.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePersonAsync_WithBirthDateInFuture_ReturnsInvalidData()
    {
        const int personId = 1;
        _uowMock.PersonRepository.GetPersonByIdAsync(personId).Returns(CreatePerson(personId));

        var result = await _sut.UpdatePersonAsync(
            personId, null, null, null, null, new LocalDate(2026, 5, 20), null, null, null, null, null
        );

        result.IsT2.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePersonAsync_EmailTakenByAnotherUser_ReturnsConflict()
    {
        const int personId = 1;
        const string email = "taken@example.com";
        _uowMock.PersonRepository.GetPersonByIdAsync(personId).Returns(CreatePerson(personId));
        _uowMock.PersonRepository.IsEmailTakenByAnotherUserAsync(email, personId).Returns(true);

        var result = await _sut.UpdatePersonAsync(
            personId, null, null, null, null, null, email, null, null, null, null
        );

        result.IsT3.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePersonAsync_InvalidData_DoesNotSave()
    {
        const int personId = 1;
        _uowMock.PersonRepository.GetPersonByIdAsync(personId).Returns(CreatePerson(personId));

        await _sut.UpdatePersonAsync(personId, null, null, 0M, null, null, null, null, null, null, null);

        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task UpdatePersonAsync_AllFieldsNull_KeepsPersonUnchanged()
    {
        const int personId = 1;
        var person = CreatePerson(personId);
        _uowMock.PersonRepository.GetPersonByIdAsync(personId).Returns(person);

        var result = await _sut.UpdatePersonAsync(
            personId, null, null, null, null, null, null, null, null, null, null
        );

        result.IsT0.Should().BeTrue();
        person.FirstName.Should().Be("Max");
        person.LastName.Should().Be("Mustermann");
        person.Height.Should().Be(180M);
        person.Weight.Should().Be(75M);
        person.DateOfBirth.Should().Be(new LocalDate(1990, 1, 1));
        person.Email.Should().Be("max@example.com");
    }

    [Fact]
    public async Task UpdatePersonAsync_ValidData_UpdatesFieldsAndSaves()
    {
        const int personId = 1;
        var person = CreatePerson(personId);
        _uowMock.PersonRepository.GetPersonByIdAsync(personId).Returns(person);
        _uowMock.PersonRepository.IsEmailTakenByAnotherUserAsync("new@example.com", personId).Returns(false);

        var result = await _sut.UpdatePersonAsync(
            personId, "Moritz", "Musterfrau", 185M, 80M, new LocalDate(1991, 2, 2),
            "new@example.com", "https://example.com", "New description", null, null
        );

        result.IsT0.Should().BeTrue();
        person.FirstName.Should().Be("Moritz");
        person.LastName.Should().Be("Musterfrau");
        person.Height.Should().Be(185M);
        person.Weight.Should().Be(80M);
        person.DateOfBirth.Should().Be(new LocalDate(1991, 2, 2));
        person.Email.Should().Be("new@example.com");
        person.WebsiteLink.Should().Be("https://example.com");
        person.Description.Should().Be("New description");
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task UpdatePersonAsync_WithAddress_UpdatesAddress()
    {
        const int personId = 1;
        var person = CreatePerson(personId);
        var newAddress = CreateAddress(5);
        _uowMock.PersonRepository.GetPersonByIdAsync(personId).Returns(person);

        var result = await _sut.UpdatePersonAsync(
            personId, null, null, null, null, null, null, null, null, newAddress, null
        );

        result.IsT0.Should().BeTrue();
        person.Address.Should().Be(newAddress);
        person.AddressId.Should().Be(5);
    }

    [Fact]
    public async Task UpdatePersonAsync_WithRoles_ReplacesRoles()
    {
        const int personId = 1;
        var person = CreatePerson(personId);
        var roles = new List<PersonRoleAssignment> { Uninit<PersonRoleAssignment>() };
        _uowMock.PersonRepository.GetPersonByIdAsync(personId).Returns(person);

        var result = await _sut.UpdatePersonAsync(
            personId, null, null, null, null, null, null, null, null, null, roles
        );

        result.IsT0.Should().BeTrue();
        person.Roles.Should().BeEquivalentTo(roles);
    }

    [Fact]
    public async Task UpdatePersonAsync_WithEmptyRoles_KeepsExistingRoles()
    {
        const int personId = 1;
        var person = CreatePerson(personId);
        var existingRoles = new List<PersonRoleAssignment> { Uninit<PersonRoleAssignment>() };
        person.Roles = existingRoles;
        _uowMock.PersonRepository.GetPersonByIdAsync(personId).Returns(person);

        var result = await _sut.UpdatePersonAsync(
            personId, null, null, null, null, null, null, null, null, null, []
        );

        result.IsT0.Should().BeTrue();
        person.Roles.Should().BeEquivalentTo(existingRoles);
    }


    [Fact]
    public async Task DeletePersonAsync_PersonNotFound_ReturnsNotFound()
    {
        const int personId = 1;
        _uowMock.PersonRepository.GetPersonByIdAsync(personId).Returns((Person?)null);

        var result = await _sut.DeletePersonAsync(personId);

        result.IsT1.Should().BeTrue();
        _uowMock.PersonRepository.DidNotReceive().RemovePerson(Arg.Any<Person>());
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task DeletePersonAsync_PersonExists_RemovesPersonAndReturnsSuccess()
    {
        const int personId = 1;
        var person = CreatePerson(personId);
        _uowMock.PersonRepository.GetPersonByIdAsync(personId).Returns(person);

        var result = await _sut.DeletePersonAsync(personId);

        result.IsT0.Should().BeTrue();
        _uowMock.PersonRepository.Received(1).RemovePerson(person);
        await _uowMock.Received(1).SaveChangesAsync();
    }
}