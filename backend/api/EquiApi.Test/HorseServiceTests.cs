using EquiApi.Core.Services;
using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using EquiApi.Test.Util;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OneOf.Types;

namespace EquiApi.Test;

public sealed class HorseServiceTests
{
    private const int HorseId = 1;

    private readonly IUnitOfWork _uowMock = Substitute.For<IUnitOfWork>();
    private readonly HorseService _sut;

    public HorseServiceTests()
    {
        _sut = new HorseService(_uowMock, NullLogger<HorseService>.Instance);
    }

    [Fact]
    public async Task GetHorseByIdAsync_HorseNotFound_ReturnsNotFound()
    {
        _uowMock.HorseRepository.GetHorseByIdAsync(HorseId).Returns((Horse?) null);

        var result = await _sut.GetHorseByIdAsync(HorseId);

        result.Value.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task GetHorseByIdAsync_HorseExists_ReturnsHorse()
    {
        var horse = TestData.CreateHorse(HorseId);
        _uowMock.HorseRepository.GetHorseByIdAsync(HorseId).Returns(horse);

        var result = await _sut.GetHorseByIdAsync(HorseId);

        result.Value.Should().BeOfType<Success<Horse>>().Which.Value.Should().Be(horse);
    }

    [Fact]
    public async Task GetOwnedHorsesOfPersonAsync_PersonNotFound_ReturnsNotFound()
    {
        const int PersonId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(PersonId).Returns(false);

        var result = await _sut.GetOwnedHorsesOfPersonAsync(PersonId);

        result.Value.Should().BeOfType<NotFound>();
        await _uowMock.HorseRepository.DidNotReceive().GetOwnedHorsesOfPersonAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task GetOwnedHorsesOfPersonAsync_PersonExists_ReturnsHorses()
    {
        const int PersonId = 1;
        var horses = new List<Horse> { TestData.CreateHorse(1), TestData.CreateHorse(2, "Blitz") };
        _uowMock.PersonRepository.PersonExistsAsync(PersonId).Returns(true);
        _uowMock.HorseRepository.GetOwnedHorsesOfPersonAsync(PersonId).Returns(horses);

        var result = await _sut.GetOwnedHorsesOfPersonAsync(PersonId);

        result.Value.Should().BeOfType<Success<IReadOnlyCollection<Horse>>>()
              .Which.Value.Should().BeEquivalentTo(horses);
    }

    [Fact]
    public async Task AddHorseAsync_OwnerNotFound_ReturnsNotFound()
    {
        const int OwnerId = 1;
        _uowMock.PersonRepository.GetPersonByIdAsync(OwnerId).Returns((Person?) null);

        var result = await _sut.AddHorseAsync(TestData.CreateNewHorseData(OwnerId));

        result.Value.Should().BeOfType<NotFound>();
        _uowMock.HorseRepository.DidNotReceive().AddHorse(Arg.Any<Horse>());
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AddHorseAsync_BreedNotFound_ReturnsNotFound()
    {
        const int OwnerId = 1;
        _uowMock.PersonRepository.GetPersonByIdAsync(OwnerId).Returns(TestData.CreatePerson(OwnerId));
        _uowMock.HorseRepository.GetBreedsByIdAsync(Arg.Any<IReadOnlyCollection<int>>())
                .Returns(new List<Breed> { TestData.CreateBreed(1) });

        var result = await _sut.AddHorseAsync(TestData.CreateNewHorseData(OwnerId, 1, 2));

        result.Value.Should().BeOfType<NotFound>();
        _uowMock.HorseRepository.DidNotReceive().AddHorse(Arg.Any<Horse>());
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AddHorseAsync_ValidData_AddsHorseWithOwnerAndBreeds()
    {
        const int OwnerId = 1;
        var owner = TestData.CreatePerson(OwnerId);
        var breed = TestData.CreateBreed(1);
        var address = TestData.CreateAddress();
        _uowMock.PersonRepository.GetPersonByIdAsync(OwnerId).Returns(owner);
        _uowMock.HorseRepository.GetBreedsByIdAsync(Arg.Any<IReadOnlyCollection<int>>())
                .Returns(new List<Breed> { breed });
        _uowMock.LocationRepository.FindAddressAsync(null, "4400", "Steyr").Returns(address);

        var result = await _sut.AddHorseAsync(TestData.CreateNewHorseData(OwnerId, 1));

        var horse = result.Value.Should().BeOfType<Success<Horse>>().Which.Value;
        horse.Name.Should().Be("Hugo");
        horse.Address.Should().Be(address);
        horse.HorseBreeds.Should().ContainSingle().Which.Breed.Should().Be(breed);
        var personHorse = horse.Persons.Should().ContainSingle().Subject;
        personHorse.Person.Should().Be(owner);
        personHorse.IsOwner.Should().BeTrue();
        personHorse.IsHidden.Should().BeFalse();
        _uowMock.HorseRepository.Received(1).AddHorse(horse);
        _uowMock.LocationRepository.DidNotReceive().AddAddress(Arg.Any<Address>());
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task AddHorseAsync_AddressDoesNotExist_AddsAddress()
    {
        const int OwnerId = 1;
        _uowMock.PersonRepository.GetPersonByIdAsync(OwnerId).Returns(TestData.CreatePerson(OwnerId));
        _uowMock.HorseRepository.GetBreedsByIdAsync(Arg.Any<IReadOnlyCollection<int>>())
                .Returns(new List<Breed> { TestData.CreateBreed(1) });
        _uowMock.LocationRepository.FindAddressAsync(null, "4400", "Steyr").Returns((Address?) null);

        var result = await _sut.AddHorseAsync(TestData.CreateNewHorseData(OwnerId, 1));

        var horse = result.Value.Should().BeOfType<Success<Horse>>().Which.Value;
        horse.Address.CityName.Should().Be("Steyr");
        horse.Address.PLZ.Should().Be("4400");
        _uowMock.LocationRepository.Received(1).AddAddress(horse.Address);
    }

    [Fact]
    public async Task GetSaddlesOfHorseAsync_HorseNotFound_ReturnsNotFound()
    {
        _uowMock.HorseRepository.HorseExistsAsync(HorseId).Returns(false);

        var result = await _sut.GetSaddlesOfHorseAsync(HorseId);

        result.Value.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task GetSaddlesOfHorseAsync_HorseWithoutSaddles_ReturnsEmptyList()
    {
        _uowMock.HorseRepository.HorseExistsAsync(HorseId).Returns(true);
        _uowMock.HorseRepository.GetSaddlesOfHorseAsync(HorseId).Returns(new List<Saddle>());

        var result = await _sut.GetSaddlesOfHorseAsync(HorseId);

        result.Value.Should().BeOfType<Success<IReadOnlyCollection<Saddle>>>().Which.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSaddlesOfHorseAsync_HorseWithSaddles_ReturnsSaddles()
    {
        var saddles = new List<Saddle> { TestData.CreateSaddle(1, HorseId), TestData.CreateSaddle(2, HorseId) };
        _uowMock.HorseRepository.HorseExistsAsync(HorseId).Returns(true);
        _uowMock.HorseRepository.GetSaddlesOfHorseAsync(HorseId).Returns(saddles);

        var result = await _sut.GetSaddlesOfHorseAsync(HorseId);

        result.Value.Should().BeOfType<Success<IReadOnlyCollection<Saddle>>>()
              .Which.Value.Should().BeEquivalentTo(saddles);
    }

    [Fact]
    public async Task GetRidersOfHorseAsync_HorseNotFound_ReturnsNotFound()
    {
        _uowMock.HorseRepository.HorseExistsAsync(HorseId).Returns(false);

        var result = await _sut.GetRidersOfHorseAsync(HorseId);

        result.Value.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task GetRidersOfHorseAsync_HorseExists_ReturnsRiders()
    {
        var riders = new List<Person> { TestData.CreatePerson(1), TestData.CreatePerson(2) };
        _uowMock.HorseRepository.HorseExistsAsync(HorseId).Returns(true);
        _uowMock.HorseRepository.GetRidersOfHorseAsync(HorseId).Returns(riders);

        var result = await _sut.GetRidersOfHorseAsync(HorseId);

        result.Value.Should().BeOfType<Success<IReadOnlyCollection<Person>>>()
              .Which.Value.Should().Equal(riders);
    }

    [Fact]
    public async Task GetHiddenRidersOfHorseAsync_HorseNotFound_ReturnsNotFound()
    {
        _uowMock.HorseRepository.HorseExistsAsync(HorseId).Returns(false);

        var result = await _sut.GetHiddenRidersOfHorseAsync(HorseId);

        result.Value.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task GetHiddenRidersOfHorseAsync_HorseExists_ReturnsHiddenRiders()
    {
        var riders = new List<Person> { TestData.CreatePerson(3) };
        _uowMock.HorseRepository.HorseExistsAsync(HorseId).Returns(true);
        _uowMock.HorseRepository.GetHiddenRidersOfHorseAsync(HorseId).Returns(riders);

        var result = await _sut.GetHiddenRidersOfHorseAsync(HorseId);

        result.Value.Should().BeOfType<Success<IReadOnlyCollection<Person>>>()
              .Which.Value.Should().Equal(riders);
    }

    [Fact]
    public async Task AddRiderToHorseAsync_HorseNotFound_ReturnsNotFound()
    {
        _uowMock.HorseRepository.HorseExistsAsync(HorseId).Returns(false);

        var result = await _sut.AddRiderToHorseAsync(HorseId, 2);

        result.Value.Should().BeOfType<NotFound>();
        _uowMock.HorseRepository.DidNotReceive().AddPersonToHorse(Arg.Any<PersonHorse>());
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AddRiderToHorseAsync_PersonNotFound_ReturnsNotFound()
    {
        _uowMock.HorseRepository.HorseExistsAsync(HorseId).Returns(true);
        _uowMock.PersonRepository.PersonExistsAsync(2).Returns(false);

        var result = await _sut.AddRiderToHorseAsync(HorseId, 2);

        result.Value.Should().BeOfType<NotFound>();
        _uowMock.HorseRepository.DidNotReceive().AddPersonToHorse(Arg.Any<PersonHorse>());
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AddRiderToHorseAsync_PersonAlreadyBelongsToHorse_ReturnsRiderAlreadyAssigned()
    {
        _uowMock.HorseRepository.HorseExistsAsync(HorseId).Returns(true);
        _uowMock.PersonRepository.PersonExistsAsync(2).Returns(true);
        _uowMock.HorseRepository.GetPersonOfHorseAsync(HorseId, 2)
                .Returns(new PersonHorse { HorseId = HorseId, PersonId = 2, IsHidden = true });

        var result = await _sut.AddRiderToHorseAsync(HorseId, 2);

        result.Value.Should().BeOfType<IHorseService.RiderAlreadyAssigned>();
        _uowMock.HorseRepository.DidNotReceive().AddPersonToHorse(Arg.Any<PersonHorse>());
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AddRiderToHorseAsync_NewRider_AddsVisibleRider()
    {
        _uowMock.HorseRepository.HorseExistsAsync(HorseId).Returns(true);
        _uowMock.PersonRepository.PersonExistsAsync(2).Returns(true);
        _uowMock.HorseRepository.GetPersonOfHorseAsync(HorseId, 2).Returns((PersonHorse?) null);

        var result = await _sut.AddRiderToHorseAsync(HorseId, 2);

        result.Value.Should().BeOfType<Success>();
        _uowMock.HorseRepository.Received(1).AddPersonToHorse(Arg.Is<PersonHorse>(ph =>
            ph.HorseId == HorseId && ph.PersonId == 2 && !ph.IsOwner && !ph.IsHidden));
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task SetRiderHiddenAsync_PersonIsNoRider_ReturnsNotFound()
    {
        _uowMock.HorseRepository.GetPersonOfHorseAsync(HorseId, 2).Returns((PersonHorse?) null);

        var result = await _sut.SetRiderHiddenAsync(HorseId, 2, true);

        result.Value.Should().BeOfType<NotFound>();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task SetRiderHiddenAsync_Owner_ReturnsOwnerCannotBeHidden()
    {
        var owner = new PersonHorse { HorseId = HorseId, PersonId = 2, IsOwner = true };
        _uowMock.HorseRepository.GetPersonOfHorseAsync(HorseId, 2).Returns(owner);

        var result = await _sut.SetRiderHiddenAsync(HorseId, 2, true);

        result.Value.Should().BeOfType<IHorseService.OwnerCannotBeHidden>();
        owner.IsHidden.Should().BeFalse();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetRiderHiddenAsync_Rider_SetsIsHidden(bool isHidden)
    {
        var rider = new PersonHorse { HorseId = HorseId, PersonId = 2, IsHidden = !isHidden };
        _uowMock.HorseRepository.GetPersonOfHorseAsync(HorseId, 2).Returns(rider);

        var result = await _sut.SetRiderHiddenAsync(HorseId, 2, isHidden);

        result.Value.Should().BeOfType<Success>();
        rider.IsHidden.Should().Be(isHidden);
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task AddSaddleToHorse_HorseNotFound_ReturnsNotFound()
    {
        _uowMock.HorseRepository.GetHorseByIdAsync(HorseId).Returns((Horse?) null);

        var result = await _sut.AddSaddleToHorse("Dressursattel", HorseId, 1);

        result.Value.Should().BeOfType<NotFound>();
        await _uowMock.SaddleRepository.DidNotReceive().GetSaddleCategoryByIdAsync(Arg.Any<int>());
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AddSaddleToHorse_CategoryNotFound_ReturnsNotFound()
    {
        const int CategoryId = 5;
        var horse = TestData.CreateHorse(HorseId);
        _uowMock.HorseRepository.GetHorseByIdAsync(HorseId).Returns(horse);
        _uowMock.SaddleRepository.GetSaddleCategoryByIdAsync(CategoryId).Returns((SaddleCategory?) null);

        var result = await _sut.AddSaddleToHorse("Dressursattel", HorseId, CategoryId);

        result.Value.Should().BeOfType<NotFound>();
        horse.Saddles.Should().BeEmpty();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AddSaddleToHorse_ValidData_AddsSaddleToHorse()
    {
        const int CategoryId = 5;
        var horse = TestData.CreateHorse(HorseId);
        _uowMock.HorseRepository.GetHorseByIdAsync(HorseId).Returns(horse);
        var category = TestData.CreateSaddleCategory(CategoryId);
        _uowMock.SaddleRepository.GetSaddleCategoryByIdAsync(CategoryId).Returns(category);

        var result = await _sut.AddSaddleToHorse("Dressursattel", HorseId, CategoryId);

        var saddle = result.Value.Should().BeOfType<Success<Saddle>>().Which.Value;
        saddle.Name.Should().Be("Dressursattel");
        saddle.HorseId.Should().Be(HorseId);
        saddle.CategoryId.Should().Be(CategoryId);
        // the response needs the name of the category
        saddle.Category.Should().Be(category);
        horse.Saddles.Should().ContainSingle().Which.Should().Be(saddle);
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task GetAllSaddleCategories_ReturnsCategories()
    {
        var categories = new List<SaddleCategory>
        {
            TestData.CreateSaddleCategory(1, "Dressage"),
            TestData.CreateSaddleCategory(2, "Jumping")
        };
        _uowMock.SaddleRepository.GetAllSaddleCategories().Returns(categories);

        var result = await _sut.GetAllSaddleCategories();

        result.Should().BeEquivalentTo(categories);
    }
}