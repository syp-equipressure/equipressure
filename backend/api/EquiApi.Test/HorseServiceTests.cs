using EquiApi.Core.Services;
using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using NSubstitute;
using HorseService = EquiApi.Persistence.Model.Horse;

namespace EquiApi.Test;

public sealed class HorseServiceTests
{
    private readonly IUnitOfWork _uowMock = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();
    private readonly ILogger<Core.Services.HorseService> _loggerMock = Substitute.For<ILogger<Core.Services.HorseService>>();
    private readonly Core.Services.HorseService _sut;

    public HorseServiceTests()
    {
        _sut = new Core.Services.HorseService(_uowMock, _loggerMock, _dateTimeProviderMock);

        _dateTimeProviderMock.GetCurrentDate().Returns(new LocalDate(2026, 1, 1));
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

    private static HorseService CreateHorse(int id = 1, Address? address = null) => new()
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
    

    [Fact]
    public async Task GetHorseByIdAsync_HorseNotFound_ReturnsNotFound()
    {
        const int horseId = 1;
        _uowMock.HorseRepository.GetHorseByIdAsync(horseId).Returns((HorseService?)null);

        var result = await _sut.GetHorseByIdAsync(horseId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task GetHorseByIdAsync_HorseExists_ReturnsHorse()
    {
        const int horseId = 1;
        var horse = CreateHorse(horseId);
        _uowMock.HorseRepository.GetHorseByIdAsync(horseId).Returns(horse);

        var result = await _sut.GetHorseByIdAsync(horseId);

        result.IsT0.Should().BeTrue();
        result.AsT0.Should().Be(horse);
    }

    [Fact]
    public async Task AddHorse_WithInvalidHeight_ReturnsInvalidData()
    {
        var result = await _sut.AddHorse(
            "Hugo", new LocalDate(2015, 1, 1), 500M, 0M, HorseGender.Male, CreateAddress(),
            [CreateHorseBreed()]
        );

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task AddHorse_WithInvalidWeight_ReturnsInvalidData()
    {
        var result = await _sut.AddHorse(
            "Hugo", new LocalDate(2015, 1, 1), -1M, 170M, HorseGender.Male, CreateAddress(),
            [CreateHorseBreed()]
        );

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task AddHorse_WithBirthDateInFuture_ReturnsInvalidData()
    {
        var futureDate = new LocalDate(2026, 5, 20);

        var result = await _sut.AddHorse(
            "Hugo", futureDate, 500M, 170M, HorseGender.Male, CreateAddress(),
            [CreateHorseBreed()]
        );

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task AddHorse_WithNoBreeds_ReturnsInvalidData()
    {
        var result = await _sut.AddHorse(
            "Hugo", new LocalDate(2015, 1, 1), 500M, 170M, HorseGender.Male, CreateAddress(),
            []
        );

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task AddHorse_ValidData_ReturnsSuccess()
    {
        var address = CreateAddress();
        var breeds = new List<HorseBreed> { CreateHorseBreed() };

        var result = await _sut.AddHorse(
            "Hugo", new LocalDate(2015, 1, 1), 500M, 170M, HorseGender.Male, address, breeds
        );

        result.IsT0.Should().BeTrue();
        result.AsT0.Value.Name.Should().Be("Hugo");
        result.AsT0.Value.Address.Should().Be(address);
        result.AsT0.Value.HorseBreeds.Should().BeEquivalentTo(breeds);
    }
}