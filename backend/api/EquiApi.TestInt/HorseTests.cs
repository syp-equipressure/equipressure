using System.Net;
using System.Net.Http.Json;
using EquiApi.Persistence.Model;
using EquiApi.TestInt.Util;
using EquiApi.Util;
using NodaTime;
using Xunit;

namespace EquiApi.TestInt;

public sealed class HorseTests(WebApiTestFixture webApiFixture) : WebApiTestBase(webApiFixture)
{
    private const int NonExistingId = 999999;

    private static Address CreateAddress(string plz = "4400", string city = "Steyr", string? addressName = null) => new()
    {
        AddressName = addressName,
        PLZ = plz,
        CityName = city,
        Persons = [],
        Horses = []
    };

    private static Breed CreateBreed(string name = "Haflinger") => new()
    {
        Name = name,
        HorseBreeds = []
    };

    private static Person CreatePerson(
        string firstName = "Max",
        string lastName = "Mustermann",
        decimal height = 180M,
        decimal weight = 75M,
        string? email = "max@reiter.at",
        Address? address = null) => new()
    {
        FirstName = firstName,
        LastName = lastName,
        Height = height,
        Weight = weight,
        DateOfBirth = new LocalDate(1995, 5, 20),
        Email = email,
        WebsiteLink = null,
        Description = null,
        Address = address ?? CreateAddress(),
        Relationships = [],
        Roles = [],
        Horses = [],
        MeasurementGroups = [],
        UserDevices = [],
        OwnerDevices = [],
        Releases = []
    };

    private static Horse CreateHorse(
        string name = "Storm",
        Address? address = null,
        HorseGender gender = HorseGender.Male,
        decimal weight = 550M,
        decimal height = 168M) => new()
    {
        Name = name,
        DateOfBirth = new LocalDate(2018, 6, 1),
        Weight = weight,
        Height = height,
        Gender = gender,
        Address = address ?? CreateAddress(),
        HorseBreeds = [],
        Persons = [],
        Saddles = [],
        MeasurementGroups = []
    };

    private static DataTransfer.AddHorseRequest CreateAddRequest(
        int ownerId,
        int breedId,
        string name = "Storm",
        decimal weight = 550M,
        decimal height = 168M,
        LocalDate? dateOfBirth = null,
        List<HorseBreed>? breeds = null) => new(
                                                Name: name,
                                                DateOfBirth: dateOfBirth ?? new LocalDate(2018, 6, 1),
                                                Weight: weight,
                                                Height: height,
                                                Gender: HorseGender.Male,
                                                Address: CreateAddress(),
                                                Breeds: breeds ?? [new HorseBreed { BreedId = breedId, Breed = null!, Horse = null! }],
                                                OwnerId: ownerId
                                                );

    private async Task<(Person Person, Horse Horse)> SeedHorseWithPersonAsync(
        string horseName = "Blitz", bool isOwner = true, bool isHidden = false)
    {
        var address = CreateAddress();
        var person = CreatePerson(address: address);
        var horse = CreateHorse(horseName, address: address);

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Addresses.Add(address);
            ctx.Persons.Add(person);
            ctx.Horses.Add(horse);
            await ctx.SaveChangesAsync();

            ctx.Add(new PersonHorse
            {
                PersonId = person.Id,
                HorseId = horse.Id,
                IsOwner = isOwner,
                IsHidden = isHidden
            });
            await ctx.SaveChangesAsync();
        });

        return (person, horse);
    }

    private async Task<(Person Person, Breed Breed)> SeedOwnerAndBreedAsync()
    {
        var person = CreatePerson();
        var breed = CreateBreed();

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.Add(person);
            ctx.Breeds.Add(breed);
            await ctx.SaveChangesAsync();
        });

        return (person, breed);
    }


    [Fact]
    public async ValueTask GetHorsesOfPerson_ExistingPersonWithHorses_Success()
    {
        var (person, _) = await SeedHorseWithPersonAsync("Blitz");

        var response = await ApiClient.GetAsync($"api/horses/person/{person.Id}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<DataTransfer.HorseListResponse>(JsonOptions, TestCancellationToken);

        content.Should().NotBeNull();
        content!.Horses.Should().ContainSingle(h => h.Name == "Blitz");
    }

    [Fact]
    public async ValueTask GetHorsesOfPerson_NonExistingPerson_ReturnsNotFound()
    {
        var response = await ApiClient.GetAsync($"api/horses/person/{NonExistingId}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetHorsesOfPerson_InvalidId_ReturnsBadRequest()
    {
        var response = await ApiClient.GetAsync("api/horses/person/-1", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }


    [Fact]
    public async ValueTask GetById_ExistingHorse_Success()
    {
        var address = CreateAddress();
        var horse = CreateHorse("Amigo", address: address);

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Addresses.Add(address);
            ctx.Horses.Add(horse);
            await ctx.SaveChangesAsync();
        });

        var response = await ApiClient.GetAsync($"api/horses/{horse.Id}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<DataTransfer.HorseDto>(JsonOptions, TestCancellationToken);

        content.Should().NotBeNull();
        content!.Name.Should().Be("Amigo");
        content.Weight.Should().Be(550M);
        content.Height.Should().Be(168M);
        content.Gender.Should().Be(HorseGender.Male.ToString());
    }

    [Fact]
    public async ValueTask GetById_NonExistingHorse_ReturnsNotFound()
    {
        var response = await ApiClient.GetAsync($"api/horses/{NonExistingId}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetById_InvalidId_ReturnsBadRequest()
    {
        var response = await ApiClient.GetAsync("api/horses/-1", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }


    [Fact]
    public async ValueTask CreateHorse_ValidData_ReturnsCreated()
    {
        var (owner, breed) = await SeedOwnerAndBreedAsync();
        var request = CreateAddRequest(owner.Id, breed.Id, name: "Neuer");

        var response = await ApiClient.PostAsJsonAsync("api/horses", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var getResponse = await ApiClient.GetAsync(response.Headers.Location, TestCancellationToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var horse = await getResponse.Content.ReadFromJsonAsync<DataTransfer.HorseDto>(JsonOptions, TestCancellationToken);
        horse!.Name.Should().Be("Neuer");
    }

    [Fact]
    public async ValueTask CreateHorse_ValidData_HorseAppearsInOwnersHorses()
    {
        var (owner, breed) = await SeedOwnerAndBreedAsync();
        var request = CreateAddRequest(owner.Id, breed.Id, name: "Neuer");

        var createResponse = await ApiClient.PostAsJsonAsync("api/horses", request, JsonOptions, TestCancellationToken);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var response = await ApiClient.GetAsync($"api/horses/person/{owner.Id}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<DataTransfer.HorseListResponse>(JsonOptions, TestCancellationToken);
        content!.Horses.Should().ContainSingle(h => h.Name == "Neuer");
    }

    [Fact]
    public async ValueTask CreateHorse_OwnerNotFound_ReturnsNotFound()
    {
        var (_, breed) = await SeedOwnerAndBreedAsync();
        var request = CreateAddRequest(NonExistingId, breed.Id);

        var response = await ApiClient.PostAsJsonAsync("api/horses", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask CreateHorse_InvalidHeight_ReturnsBadRequest()
    {
        var (owner, breed) = await SeedOwnerAndBreedAsync();
        var request = CreateAddRequest(owner.Id, breed.Id, height: 0M);

        var response = await ApiClient.PostAsJsonAsync("api/horses", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask CreateHorse_InvalidWeight_ReturnsBadRequest()
    {
        var (owner, breed) = await SeedOwnerAndBreedAsync();
        var request = CreateAddRequest(owner.Id, breed.Id, weight: -1M);

        var response = await ApiClient.PostAsJsonAsync("api/horses", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask CreateHorse_BirthDateInFuture_ReturnsBadRequest()
    {
        var (owner, breed) = await SeedOwnerAndBreedAsync();
        var request = CreateAddRequest(owner.Id, breed.Id, dateOfBirth: new LocalDate(2999, 1, 1));

        var response = await ApiClient.PostAsJsonAsync("api/horses", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask CreateHorse_NoBreeds_ReturnsBadRequest()
    {
        var (owner, breed) = await SeedOwnerAndBreedAsync();
        var request = CreateAddRequest(owner.Id, breed.Id, breeds: []);

        var response = await ApiClient.PostAsJsonAsync("api/horses", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }


    [Fact]
    public async ValueTask GetSaddlesOfHorse_InvalidId_ReturnsBadRequest()
    {
        var response = await ApiClient.GetAsync("api/horses/-1/saddles", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask GetSaddlesOfHorse_HorseWithoutSaddles_ReturnsNotFound()
    {
        var (_, horse) = await SeedHorseWithPersonAsync();

        var response = await ApiClient.GetAsync($"api/horses/{horse.Id}/saddles", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }


    [Fact]
    public async ValueTask GetRidersOfHorse_InvalidId_ReturnsBadRequest()
    {
        var response = await ApiClient.GetAsync("api/horses/-1/riders", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask GetRidersOfHorse_NoRiders_ReturnsNotFound()
    {
        var response = await ApiClient.GetAsync($"api/horses/{NonExistingId}/riders", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetRidersOfHorse_HorseWithRider_ReturnsOk()
    {
        var (_, horse) = await SeedHorseWithPersonAsync(isOwner: false, isHidden: false);

        var response = await ApiClient.GetAsync($"api/horses/{horse.Id}/riders", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---------- GET api/horses/{id}/hidden ----------

    [Fact]
    public async ValueTask GetHiddenUsersOfHorse_InvalidId_ReturnsBadRequest()
    {
        var response = await ApiClient.GetAsync("api/horses/-1/hidden", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask GetHiddenUsersOfHorse_NoHiddenUsers_ReturnsOk()
    {
        var (_, horse) = await SeedHorseWithPersonAsync(isHidden: false);

        var response = await ApiClient.GetAsync($"api/horses/{horse.Id}/hidden", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async ValueTask GetHiddenUsersOfHorse_WithHiddenUser_ReturnsOk()
    {
        var (_, horse) = await SeedHorseWithPersonAsync(isOwner: false, isHidden: true);

        var response = await ApiClient.GetAsync($"api/horses/{horse.Id}/hidden", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}