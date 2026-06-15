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
    private static Address CreateAddress(string plz = "4400", string city = "Steyr", string? addressName = null) => new()
    {
        AddressName = addressName,
        PLZ = plz,
        CityName = city,
        Persons = [],
        Horses = []
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

    [Fact]
    public async ValueTask GetHorsesOfPerson_ExistingPersonWithHorses_Success()
    {
        var address = CreateAddress();
        var person = CreatePerson(address: address);
        var horse = CreateHorse("Blitz", address: address);

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Addresses.Add(address);
            ctx.Persons.Add(person);
            ctx.Horses.Add(horse);
            await ctx.SaveChangesAsync();

            ctx.Add(new PersonHorse { PersonId = person.Id, HorseId = horse.Id, IsOwner = true});
            await ctx.SaveChangesAsync();
        });

        var response = await ApiClient.GetAsync($"api/horses/person/{person.Id}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<DataTransfer.HorseListResponse>(JsonOptions, TestCancellationToken);

        content.Should().NotBeNull();
        content!.Horses.Should().ContainSingle(h => h.Name == "Blitz");
    }

    [Fact]
    public async ValueTask GetHorsesOfPerson_NonExistingPerson_ReturnsNotFound()
    {
        const int nonExistingPersonId = 999999;

        var response = await ApiClient.GetAsync($"api/horses/person/{nonExistingPersonId}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetHorsesOfPerson_InvalidId_ReturnsBadRequest()
    {
        const int invalidPersonId = -1;

        var response = await ApiClient.GetAsync($"api/horses/person/{invalidPersonId}", TestCancellationToken);

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
        const int nonExistingHorseId = 999999;

        var response = await ApiClient.GetAsync($"api/horses/{nonExistingHorseId}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}