using System.Net;
using System.Net.Http.Json;
using EquiApi.Persistence.Model;
using EquiApi.TestInt.Util;
using EquiApi.Util;
using Xunit;

namespace EquiApi.TestInt;

public sealed class LocationTests(WebApiTestFixture webApiFixture) : WebApiTestBase(webApiFixture)
{
    private static Address CreateAddress(string plz = "4400", string city = "Steyr", string? street = "Hauptstraße 1") => new()
    {
        AddressName = street,
        PLZ = plz,
        CityName = city,
        Persons = [],
        Horses = []
    };

    [Fact]
    public async ValueTask GetCities_ExistingCitiesWithoutFilter_Success()
    {
        var address1 = CreateAddress(plz: "4400", city: "Steyr");
        var address2 = CreateAddress(plz: "4020", city: "Linz");

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Addresses.AddRange(address1, address2);
            await ctx.SaveChangesAsync();
        });

        var response = await ApiClient.GetAsync("api/locations", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<DataTransfer.AddressDto>>(JsonOptions, TestCancellationToken);

        content.Should().NotBeNull();
        content.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async ValueTask GetCities_WithFilter_ReturnsFilteredSuccess()
    {
        var address = CreateAddress(plz: "4400", city: "Steyr");

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Addresses.Add(address);
            await ctx.SaveChangesAsync();
        });

        var response = await ApiClient.GetAsync("api/locations?nameFilter=Steyr", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<IReadOnlyCollection<DataTransfer.AddressDto>>(JsonOptions, TestCancellationToken);

        content.Should().NotBeNull();
        content.Should().Contain(c => c.CityName == "Steyr");
    }

    [Fact]
    public async ValueTask GetCities_NoCitiesFound_ReturnsNotFound()
    {
        var response = await ApiClient.GetAsync("api/locations?nameFilter=NichtExistierendeStadtXYZ", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask AddAddress_ValidData_Success()
    {
        var request = new DataTransfer.AddressDto
        ("Linzer Straße 5", "Linz", "4020");

        var response = await ApiClient.PostAsJsonAsync("api/locations", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async ValueTask AddAddress_InvalidData_ReturnsBadRequest()
    {
        var request = new DataTransfer.AddressDto
        (
             "",
             "",
             ""
        );

        var response = await ApiClient.PostAsJsonAsync("api/locations", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}