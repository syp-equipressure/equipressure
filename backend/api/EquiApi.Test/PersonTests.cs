using EquiApi.Core.Services;
using EquiApi.Core.Util;
using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using EquiPressure.Core.Service;
using NSubstitute;

namespace EquiApi.Test;

public sealed class PersonTests
{
    private readonly IUnitOfWork _uowMock = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();
    private readonly ILogger<PersonService> _loggerMock = Substitute.For<ILogger<PersonService>>();
    private readonly PersonService _sut;

    public PersonTests()
    {
        _sut = new PersonService(_uowMock, _dateTimeProviderMock, _loggerMock);

        _dateTimeProviderMock.GetCurrentDate().Returns(new LocalDate(2026, 1, 1));
    }

    private static Person CreatePerson(int id = 1) => new()
    {
        Id = id,
        FirstName = "Max",
        LastName = "Mustermann",
        Height = 180M,
        Weight = 75M,
        DateOfBirth = new LocalDate(1995, 5, 20),
        Email = "max@reiter.at",
        WebsiteLink = null,
        Description = null,
        AddressId = 1,
        Address = new Address
        {
            Id = 1,
            AddressName = null,
            PLZ = "4400",
            CityName = "Steyr",
            Persons = [],
            Horses = []
        },
        Relationships = [],
        Roles = [],
        Horses = [],
        MeasurementGroups = [],
        UserDevices = [],
        OwnerDevices = [],
        Releases = []
    };

    private static Address CreateAddress(int id = 1) => new()
    {
        Id = id,
        AddressName = null,
        PLZ = "4400",
        CityName = "Steyr",
        Persons = [],
        Horses = []
    };

    private static AccountRole CreateRole(RoleName name = RoleName.Equestrian, int id = 1) => new()
    {
        Id = id,
        Name = name,
        RoleAssignments = []
    };

    private static Helper.EquestrianBasicData CreateEquestrianBasicData() => new(
        FirstName: "Max",
        LastName: "Mustermann",
        AddressName: "Hauptstraße 1",
        CityName: "Steyr",
        PLZ: "4400",
        Email: "max@reiter.at",
        Height: 180M,
        Weight: 75M);

    private static Helper.SaddlerBasicData CreateSaddlerBasicData(int id = 1) => new(
        Id: id,
        FirstName: "Anna",
        LastName: "Sattler",
        AddressName: "Hauptstraße 5",
        CityName: "Steyr",
        PLZ: "4400",
        Link: "https://saddler.at",
        Description: "Beschreibung");

    private static Helper.NameData CreateNameData() => new("Max", "Mustermann");

    private static MeasurementDevice CreateDevice(string id = "device-1") => new()
    {
        Id = id,
        OwnerId = 1,
        Owner = CreatePerson(),
        CategoryId = 1,
        Category = new DeviceCategory
        {
            Id = 1,
            NumOfAllowedPeople = 5,
            Name = "Standard"
        },
        MeasurementGroups = [],
        Users = []
    };

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
    public async Task GetPersonAsEquestrianByIdAsync_PersonNotFound_ReturnsNotFound()
    {
        const int personId = 1;
        _uowMock.PersonRepository.GetPersonAsEquestrianByIdAsync(personId).Returns((Helper.EquestrianBasicData?)null);

        var result = await _sut.GetPersonAsEquestrianByIdAsync(personId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task GetPersonAsEquestrianByIdAsync_PersonExists_ReturnsSuccess()
    {
        const int personId = 1;
        _uowMock.PersonRepository.GetPersonAsEquestrianByIdAsync(personId).Returns(CreateEquestrianBasicData());

        var result = await _sut.GetPersonAsEquestrianByIdAsync(personId);

        result.IsT0.Should().BeTrue();
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
    public async Task GetPersonAddressAsync_AddressExists_ReturnsSuccess()
    {
        const int personId = 1;
        _uowMock.PersonRepository.GetPersonAddressAsync(personId).Returns(CreateAddress());

        var result = await _sut.GetPersonAddressAsync(personId);

        result.IsT0.Should().BeTrue();
    }

    [Fact]
    public async Task GetPersonAsSaddlerByIdAsync_SaddlerNotFound_ReturnsNotFound()
    {
        const int saddlerId = 1;
        _uowMock.PersonRepository.GetPersonAsSaddlerByIdAsync(saddlerId)
            .Returns((Helper.SaddlerBasicData?)null);

        var result = await _sut.GetPersonAsSaddlerByIdAsync(saddlerId);

        result.IsT2.Should().BeTrue();
    }

    [Fact]
    public async Task GetPersonAsSaddlerByIdAsync_SaddlerExists_ReturnsSuccess()
    {
        const int saddlerId = 1;
        _uowMock.PersonRepository.GetPersonAsSaddlerByIdAsync(saddlerId)
            .Returns(CreateSaddlerBasicData(saddlerId));

        var result = await _sut.GetPersonAsSaddlerByIdAsync(saddlerId);

        result.IsT0.Should().BeTrue();
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
    public async Task GetNameByIdAsync_PersonExists_ReturnsSuccess()
    {
        const int personId = 1;
        _uowMock.PersonRepository.GetNameByIdAsync(personId).Returns(CreateNameData());

        var result = await _sut.GetNameByIdAsync(personId);

        result.IsT0.Should().BeTrue();
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
    public async Task GetFavouritesAsync_ListEmpty_ReturnsNone()
    {
        const int personId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(personId).Returns(true);
        _uowMock.PersonRepository.GetFavouritesAsync(personId).Returns(new List<Person>());

        var result = await _sut.GetFavouritesAsync(personId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task GetFavouritesAsync_ListNotEmpty_ReturnsSuccess()
    {
        const int personId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(personId).Returns(true);
        _uowMock.PersonRepository.GetFavouritesAsync(personId).Returns(new List<Person> { CreatePerson() });

        var result = await _sut.GetFavouritesAsync(personId);

        result.IsT0.Should().BeTrue();
    }

    [Fact]
    public async Task GetContactsAsync_PersonNotFound_ReturnsNotFound()
    {
        const int personId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(personId).Returns(false);

        var result = await _sut.GetContactsAsync(personId);

        result.IsT2.Should().BeTrue();
    }

    [Fact]
    public async Task GetContactsAsync_ListEmpty_ReturnsNone()
    {
        const int personId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(personId).Returns(true);
        _uowMock.PersonRepository.GetContactsAsync(personId).Returns(new List<Person>());

        var result = await _sut.GetContactsAsync(personId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task GetContactsAsync_ListNotEmpty_ReturnsSuccess()
    {
        const int personId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(personId).Returns(true);
        _uowMock.PersonRepository.GetContactsAsync(personId).Returns(new List<Person> { CreatePerson() });

        var result = await _sut.GetContactsAsync(personId);

        result.IsT0.Should().BeTrue();
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
    public async Task GetOwnedHorsesAsync_ListEmpty_ReturnsNone()
    {
        const int personId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(personId).Returns(true);
        _uowMock.PersonRepository.GetOwnedHorsesAsync(personId).Returns(new List<Horse>());

        var result = await _sut.GetOwnedHorsesAsync(personId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task GetOwnedHorsesAsync_ListNotEmpty_ReturnsSuccess()
    {
        const int personId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(personId).Returns(true);
        _uowMock.PersonRepository.GetOwnedHorsesAsync(personId).Returns(new List<Horse> { CreateHorse() });

        var result = await _sut.GetOwnedHorsesAsync(personId);

        result.IsT0.Should().BeTrue();
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
    public async Task GetAllDevicesByPersonAsync_ListEmpty_ReturnsNone()
    {
        const int personId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(personId).Returns(true);
        _uowMock.PersonRepository.GetAllDevicesAsync(personId).Returns(new List<MeasurementDevice>());

        var result = await _sut.GetAllDevicesByPersonAsync(personId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllDevicesByPersonAsync_ListNotEmpty_ReturnsSuccess()
    {
        const int personId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(personId).Returns(true);
        _uowMock.PersonRepository.GetAllDevicesAsync(personId).Returns(new List<MeasurementDevice> { CreateDevice() });

        var result = await _sut.GetAllDevicesByPersonAsync(personId);

        result.IsT0.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllSaddlersAsync_ListEmpty_ReturnsNone()
    {
        _uowMock.PersonRepository.GetAllSaddlersAsync().Returns(new List<Helper.SaddlerBasicData>());

        var result = await _sut.GetAllSaddlersAsync();

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllSaddlersAsync_ListNotEmpty_ReturnsSuccess()
    {
        _uowMock.PersonRepository.GetAllSaddlersAsync().Returns(new List<Helper.SaddlerBasicData> { CreateSaddlerBasicData() });

        var result = await _sut.GetAllSaddlersAsync();

        result.IsT0.Should().BeTrue();
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
    public async Task GetAllSaddlerFavouritesAsync_ListEmpty_ReturnsNone()
    {
        const int equestrianId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(equestrianId).Returns(true);
        _uowMock.PersonRepository.GetAllSaddlerFavouritesOfEquestrianAsync(equestrianId)
            .Returns(new List<Helper.SaddlerBasicData>());

        var result = await _sut.GetAllSaddlerFavouritesAsync(equestrianId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllSaddlerFavouritesAsync_ListNotEmpty_ReturnsSuccess()
    {
        const int equestrianId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(equestrianId).Returns(true);
        _uowMock.PersonRepository.GetAllSaddlerFavouritesOfEquestrianAsync(equestrianId)
            .Returns(new List<Helper.SaddlerBasicData> { CreateSaddlerBasicData() });

        var result = await _sut.GetAllSaddlerFavouritesAsync(equestrianId);

        result.IsT0.Should().BeTrue();
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
    public async Task GetAllSaddlerContactsAsync_ListEmpty_ReturnsNone()
    {
        const int equestrianId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(equestrianId).Returns(true);
        _uowMock.PersonRepository.GetAllSaddlerContactsOfEquestrianAsync(equestrianId)
            .Returns(new List<Helper.SaddlerBasicData>());

        var result = await _sut.GetAllSaddlerContactsAsync(equestrianId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllSaddlerContactsAsync_ListNotEmpty_ReturnsSuccess()
    {
        const int equestrianId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(equestrianId).Returns(true);
        _uowMock.PersonRepository.GetAllSaddlerContactsOfEquestrianAsync(equestrianId)
            .Returns(new List<Helper.SaddlerBasicData> { CreateSaddlerBasicData() });

        var result = await _sut.GetAllSaddlerContactsAsync(equestrianId);

        result.IsT0.Should().BeTrue();
    }

    [Fact]
    public async Task AddPerson_WithInvalidPhysicalData_ReturnsInvalidData()
    {
        var invalidHeight = 0M;

        var result = await _sut.AddPersonAsync(
            "Max", "Mustermann", invalidHeight, 75M, new LocalDate(1995, 5, 20),
            "max@reiter.at", null, null, CreateAddress(), CreateRole()
        );

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task AddPerson_WithBirthDateInFuture_ReturnsInvalidData()
    {
        var futureDate = new LocalDate(2026, 5, 20);

        var result = await _sut.AddPersonAsync(
            "Max", "Mustermann", 180M, 75M, futureDate,
            "max@reiter.at", null, null, CreateAddress(), CreateRole()
        );

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task AddPerson_WithExistingEmail_ReturnsConflict()
    {
        const string existingEmail = "duplicate@reiter.at";
        _uowMock.PersonRepository.PersonWithEmailExistsAsync(existingEmail).Returns(true);

        var result = await _sut.AddPersonAsync(
            "Max", "Mustermann", 180M, 75M, new LocalDate(1995, 5, 20),
            existingEmail, null, null, CreateAddress(), CreateRole()
        );

        result.IsT2.Should().BeTrue();
    }

    [Fact]
    public async Task AddPerson_EquestrianWithWebsite_ReturnsConflict()
    {
        var website = "https://my-stable.at";

        var result = await _sut.AddPersonAsync(
            "Max", "Mustermann", 180M, 75M, new LocalDate(1995, 5, 20),
            "max@reiter.at", website, null, CreateAddress(), CreateRole()
        );

        result.IsT2.Should().BeTrue();
    }

    [Fact]
    public async Task AddPerson_ValidData_SavesToDatabaseAndReturnsSuccess()
    {
        _uowMock.PersonRepository.PersonWithEmailExistsAsync(Arg.Any<string>()).Returns(false);

        var result = await _sut.AddPersonAsync(
            "Max", "Mustermann", 180M, 75M, new LocalDate(1995, 5, 20),
            "max.unique@reiter.at", null, null, CreateAddress(), CreateRole(RoleName.Equestrian, 1)
        );

        result.IsT0.Should().BeTrue();

        _uowMock.PersonRepository.Received(1).AddPerson(Arg.Any<Person>());
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task UpdatePerson_PersonNotFound_ReturnsNotFound()
    {
        const int personId = 1;
        _uowMock.PersonRepository.GetPersonByIdAsync(personId).Returns((Person?)null);

        var result = await _sut.UpdatePersonAsync(
            personId, "Max", "Mustermann", 180M, 75M, new LocalDate(1995, 5, 20),
            "max@reiter.at", null, null, null, null
        );

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePerson_WithInvalidHeight_ReturnsInvalidData()
    {
        const int personId = 1;
        _uowMock.PersonRepository.GetPersonByIdAsync(personId).Returns(CreatePerson(personId));

        var result = await _sut.UpdatePersonAsync(
            personId, null, null, 0M, null, null,
            null, null, null, null, null
        );

        result.IsT2.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePerson_WithInvalidWeight_ReturnsInvalidData()
    {
        const int personId = 1;
        _uowMock.PersonRepository.GetPersonByIdAsync(personId).Returns(CreatePerson(personId));

        var result = await _sut.UpdatePersonAsync(
            personId, null, null, null, -1M, null,
            null, null, null, null, null
        );

        result.IsT2.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePerson_WithBirthDateInFuture_ReturnsInvalidData()
    {
        const int personId = 1;
        _uowMock.PersonRepository.GetPersonByIdAsync(personId).Returns(CreatePerson(personId));

        var result = await _sut.UpdatePersonAsync(
            personId, null, null, null, null, new LocalDate(2026, 5, 20),
            null, null, null, null, null
        );

        result.IsT2.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePerson_WithEmailTakenByAnotherUser_ReturnsConflict()
    {
        const int personId = 1;
        const string email = "taken@reiter.at";
        _uowMock.PersonRepository.GetPersonByIdAsync(personId).Returns(CreatePerson(personId));
        _uowMock.PersonRepository.IsEmailTakenByAnotherUserAsync(email, personId).Returns(true);

        var result = await _sut.UpdatePersonAsync(
            personId, null, null, null, null, null,
            email, null, null, null, null
        );

        result.IsT3.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePerson_ValidData_UpdatesAndReturnsSuccess()
    {
        const int personId = 1;
        var person = CreatePerson(personId);
        _uowMock.PersonRepository.GetPersonByIdAsync(personId).Returns(person);
        _uowMock.PersonRepository.IsEmailTakenByAnotherUserAsync(Arg.Any<string>(), personId).Returns(false);

        var newAddress = CreateAddress(2);

        var result = await _sut.UpdatePersonAsync(
            personId, "Moritz", "Musterfrau", 190M, 80M, new LocalDate(1990, 1, 1),
            "new@reiter.at", null, null, newAddress, null
        );

        result.IsT0.Should().BeTrue();
        person.FirstName.Should().Be("Moritz");
        person.LastName.Should().Be("Musterfrau");
        person.Height.Should().Be(190M);
        person.Weight.Should().Be(80M);
        person.DateOfBirth.Should().Be(new LocalDate(1990, 1, 1));
        person.Email.Should().Be("new@reiter.at");
        person.AddressId.Should().Be(newAddress.Id);
        person.Address.Should().Be(newAddress);

        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task UpdatePerson_WithRoles_UpdatesRoles()
    {
        const int personId = 1;
        var person = CreatePerson(personId);
        _uowMock.PersonRepository.GetPersonByIdAsync(personId).Returns(person);

        var newRoles = new List<PersonRoleAssignment>
        {
            new()
            {
                PersonId = personId,
                RoleId = 1,
                Person = person,
                Role = CreateRole(RoleName.Saddler, 1)
            }
        };

        var result = await _sut.UpdatePersonAsync(
            personId, null, null, null, null, null,
            null, null, null, null, newRoles
        );

        result.IsT0.Should().BeTrue();
        person.Roles.Should().BeEquivalentTo(newRoles);
    }

    [Fact]
    public async Task DeletePerson_PersonNotFound_ReturnsNotFound()
    {
        const int personId = 1;
        _uowMock.PersonRepository.GetPersonByIdAsync(personId).Returns((Person?)null);

        var result = await _sut.DeletePersonAsync(personId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task DeletePerson_PersonExists_RemovesAndReturnsSuccess()
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