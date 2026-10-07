using EquiApi.Core.Services;
using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using NSubstitute;
using Horse = EquiApi.Persistence.Model.Horse;

namespace EquiApi.Test;

public sealed class HorseServiceTests
{
    private readonly IUnitOfWork _uowMock = Substitute.For<IUnitOfWork>();
    private readonly ILogger<Core.Services.HorseService> _loggerMock = Substitute.For<ILogger<Core.Services.HorseService>>();
    private readonly Core.Services.HorseService _sut;

    public HorseServiceTests()
    {
        _sut = new Core.Services.HorseService(_uowMock, _loggerMock);
    }

    private static Address CreateAddress(int id = 1) => new()
    {
        Id = id,
        AddressName = null,
        PLZ = "4400",
        CityName = "Steyr",
        Persons = [],
        Horses = []
    };

    private static Breed CreateBreed(int id = 1, string name = "Haflinger") => new()
    {
        Id = id,
        Name = name,
        HorseBreeds = []
    };

    private static HorseBreed CreateHorseBreed(int breedId = 1, int horseId = 0) => new()
    {
        BreedId = breedId,
        Breed = CreateBreed(breedId),
        HorseId = horseId,
        Horse = null!
    };

    private static Horse CreateHorse(int id = 1, Address? address = null) => new()
    {
        Id = id,
        Name = "Hugo",
        DateOfBirth = new LocalDate(2015, 1, 1),
        Weight = 500M,
        Height = 170M,
        Gender = HorseGender.Male,
        AddressId = address?.Id ?? 1,
        Address = address ?? CreateAddress(),
        HorseBreeds = [],
        Persons = [],
        Saddles = [],
        MeasurementGroups = []
    };

    private static Person CreatePerson(int id = 1, Address? address = null) => new()
    {
        Id = id,
        FirstName = "Max",
        LastName = "Mustermann",
        Height = 180M,
        Weight = 75M,
        DateOfBirth = new LocalDate(1990, 1, 1),
        AddressId = address?.Id ?? 1,
        Address = address ?? CreateAddress()
    };

    [Fact]
    public async Task GetHorseByIdAsync_HorseNotFound_ReturnsNotFound()
    {
        const int HorseId = 1;
        _uowMock.HorseRepository.GetHorseByIdAsync(HorseId).Returns((Horse?)null);

        var result = await _sut.GetHorseByIdAsync(HorseId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task GetHorseByIdAsync_HorseExists_ReturnsHorse()
    {
        const int HorseId = 1;
        var horse = CreateHorse(HorseId);
        _uowMock.HorseRepository.GetHorseByIdAsync(HorseId).Returns(horse);

        var result = await _sut.GetHorseByIdAsync(HorseId);

        result.IsT0.Should().BeTrue();
        result.AsT0.Should().Be(horse);
    }


    [Fact]
    public async Task GetAllHorsesOfPersonAsync_PersonNotFound_ReturnsNotFound()
    {
        const int personId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(personId).Returns(false);

        var result = await _sut.GetAllHorsesOfPersonAsync(personId);

        result.IsT1.Should().BeTrue();
        await _uowMock.HorseRepository.DidNotReceive().GetAllHorsesOfUserAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task GetAllHorsesOfPersonAsync_PersonExists_ReturnsHorses()
    {
        const int personId = 1;
        var horse = CreateHorse();
        _uowMock.PersonRepository.PersonExistsAsync(personId).Returns(true);
        _uowMock.HorseRepository.GetAllHorsesOfUserAsync(personId).Returns(new List<Horse> { horse });

        var result = await _sut.GetAllHorsesOfPersonAsync(personId);

        result.IsT0.Should().BeTrue();
        result.AsT0.Should().ContainSingle().Which.Should().Be(horse);
    }


    [Fact]
    public async Task AddHorse_OwnerNotFound_ReturnsNotFound()
    {
        const int ownerId = 1;
        _uowMock.PersonRepository.GetPersonByIdAsync(ownerId).Returns((Person?)null);

        var result = await _sut.AddHorse(
            "Hugo", new LocalDate(2015, 1, 1), 500M, 170M, HorseGender.Male, CreateAddress(),
            [CreateHorseBreed()], ownerId
        );

        result.IsT2.Should().BeTrue();
        _uowMock.HorseRepository.DidNotReceive().AddHorse(Arg.Any<Horse>());
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AddHorse_ValidData_ReturnsSuccess()
    {
        const int ownerId = 1;
        var owner = CreatePerson(ownerId);
        var address = CreateAddress();
        var breeds = new List<HorseBreed> { CreateHorseBreed() };
        _uowMock.PersonRepository.GetPersonByIdAsync(ownerId).Returns(owner);

        var result = await _sut.AddHorse(
            "Hugo", new LocalDate(2015, 1, 1), 500M, 170M, HorseGender.Male, address, breeds, ownerId
        );

        result.IsT0.Should().BeTrue();
        var horse = result.AsT0.Value;
        horse.Name.Should().Be("Hugo");
        horse.Address.Should().Be(address);
        horse.HorseBreeds.Should().BeEquivalentTo(breeds);
    }

    [Fact]
    public async Task AddHorse_ValidData_AddsOwnerAndSaves()
    {
        const int ownerId = 1;
        var owner = CreatePerson(ownerId);
        _uowMock.PersonRepository.GetPersonByIdAsync(ownerId).Returns(owner);

        var result = await _sut.AddHorse(
            "Hugo", new LocalDate(2015, 1, 1), 500M, 170M, HorseGender.Male, CreateAddress(),
            [CreateHorseBreed()], ownerId
        );

        var horse = result.AsT0.Value;
        var personHorse = horse.Persons.Should().ContainSingle().Subject;
        personHorse.Person.Should().Be(owner);
        personHorse.Horse.Should().Be(horse);
        personHorse.IsOwner.Should().BeTrue();
        personHorse.IsHidden.Should().BeFalse();

        _uowMock.HorseRepository.Received(1).AddHorse(horse);
        await _uowMock.Received(1).SaveChangesAsync();
    }
}