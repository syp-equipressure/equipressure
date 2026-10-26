using System.Net;
using EquiApi.Dtos;
using EquiApi.TestInt.Util;

namespace EquiApi.TestInt;

public sealed class LocationTests(WebApiTestFixture webApiFixture) : WebApiTestBase(webApiFixture)
{
    [Fact]
    public async ValueTask GetCities_AddressesInSeveralCities_ReturnsEachCityOnceMostUsedFirst()
    {
        await SeedAsync(TestData.CreateAddress("Stadtplatz 1", "4400", "Steyr"),
                        TestData.CreatePerson("Anna", address: TestData.CreateAddress("Hauptplatz 1", "4020", "Linz")),
                        TestData.CreatePerson("Berta", address: TestData.CreateAddress("Landstraße 5", "4020", "Linz")));

        var response = await ApiClient.GetAsync("api/locations", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<IReadOnlyList<CityDto>>(response);
        content.Should().Equal(new CityDto("Linz", "4020"), new CityDto("Steyr", "4400"));
    }

    [Fact]
    public async ValueTask GetCities_WithFilter_ReturnsMatchingCitiesCaseInsensitive()
    {
        await SeedAsync(TestData.CreateAddress(cityName: "Steyr", plz: "4400"),
                        TestData.CreateAddress(cityName: "Linz", plz: "4020"));

        var response = await ApiClient.GetAsync("api/locations?nameFilter=STEY", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<IReadOnlyList<CityDto>>(response);
        content.Should().Equal(new CityDto("Steyr", "4400"));
    }

    [Fact]
    public async ValueTask GetCities_WithLength_LimitsNumberOfCities()
    {
        await SeedAsync(TestData.CreateAddress(cityName: "Steyr", plz: "4400"),
                        TestData.CreateAddress(cityName: "Linz", plz: "4020"),
                        TestData.CreateAddress(cityName: "Wels", plz: "4600"));

        var response = await ApiClient.GetAsync("api/locations?length=2", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<IReadOnlyList<CityDto>>(response);
        content.Should().HaveCount(2);
    }

    [Fact]
    public async ValueTask GetCities_NoMatchingCity_ReturnsEmptyList()
    {
        await SeedAsync(TestData.CreateAddress(cityName: "Steyr"));

        var response = await ApiClient.GetAsync("api/locations?nameFilter=NonExistingCity", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<IReadOnlyList<CityDto>>(response);
        content.Should().BeEmpty();
    }

    [Fact]
    public async ValueTask GetCities_InvalidLength_ReturnsBadRequest()
    {
        var response = await ApiClient.GetAsync("api/locations?length=0", TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask GetAddressById_ExistingAddress_ReturnsAddress()
    {
        var address = TestData.CreateAddress("Hauptplatz 1", "4020", "Linz");
        await SeedAsync(address);

        var response = await ApiClient.GetAsync($"api/locations/{address.Id}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<AddressDto>(response);
        content.Should().Be(new AddressDto(address.Id, "Hauptplatz 1", "Linz", "4020"));
    }

    [Fact]
    public async ValueTask GetAddressById_NonExistingAddress_ReturnsNotFound()
    {
        var response = await ApiClient.GetAsync($"api/locations/{TestData.NonExistingId}", TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask AddAddress_ValidData_ReturnsCreatedAddress()
    {
        var request = TestData.CreateAddressRequest("Linzer Straße 5", "Linz", "4020");

        var response = await ApiClient.PostAsJsonAsync("api/locations", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await ReadContentAsync<AddressDto>(response);
        content.Should().Be(new AddressDto(content.Id, "Linzer Straße 5", "Linz", "4020"));

        response.Headers.Location.Should().NotBeNull();
        var getResponse = await ApiClient.GetAsync(response.Headers.Location, TestCancellationToken);
        var storedAddress = await ReadContentAsync<AddressDto>(getResponse);
        storedAddress.Should().Be(content);
    }

    [Fact]
    public async ValueTask AddAddress_AddressExists_ReturnsConflict()
    {
        await SeedAsync(TestData.CreateAddress("Linzer Straße 5", "4020", "Linz"));
        var request = TestData.CreateAddressRequest("linzer straße 5", "LINZ", "4020");

        var response = await ApiClient.PostAsJsonAsync("api/locations", request, JsonOptions, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask AddAddress_InvalidData_ReturnsBadRequest()
    {
        var request = TestData.CreateAddressRequest("", "", "");

        var response = await ApiClient.PostAsJsonAsync("api/locations", request, JsonOptions, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }
}