using System.Net;
using EquiApi.Dtos;
using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using EquiApi.TestInt.Util;

namespace EquiApi.TestInt;

/// <summary>
///     Checks who may access which data, every user is a real person with a Keycloak account (see TestAuthHandler):
///     Anna owns the horse Storm and the device EP1000, Ben uses that device, Sam is Anna's saddler,
///     Carla is a stranger with her own horse, Kurt is a customer of Sam without an account
/// </summary>
public sealed class AuthorizationTests(WebApiTestFixture webApiFixture) : WebApiTestBase(webApiFixture)
{
    private const string DeviceId = "EP1000";

    private readonly Dictionary<string, Person> _persons = [];
    private Horse _storm = null!;
    private Horse _luna = null!;
    private Breed _breed = null!;
    private MeasurementGroup _group = null!;
    private Measurement _measurement = null!;

    protected override async ValueTask ImportSeedDataAsync(DatabaseContext context)
    {
        var anna = CreateAccountPerson("anna");
        var ben = CreateAccountPerson("ben");
        var carla = CreateAccountPerson("carla");
        var sam = CreateAccountPerson("sam", RoleName.Saddler);
        var kurt = TestData.CreatePerson("Kurt");
        _storm = TestData.CreateHorse();
        _luna = TestData.CreateHorse("Luna");
        _breed = TestData.CreateBreed();
        var device = TestData.CreateDevice(DeviceId, anna, TestData.CreateDeviceCategory(), ben);
        _group = TestData.CreateMeasurementGroup(anna, _storm, TestData.CreateSaddle(_storm), device);
        _measurement = TestData.CreateMeasurement(_group, data: "10");

        context.AddRange(anna, ben, carla, sam, kurt, _breed, device, _group, _measurement,
                         TestData.CreatePersonHorse(anna, _storm, true),
                         TestData.CreatePersonHorse(carla, _luna, true),
                         TestData.CreateRelationship(anna, sam, true),
                         TestData.CreateRelationship(kurt, sam, true));
        await context.SaveChangesAsync();

        _persons["kurt"] = kurt;
    }

    [Theory]
    // persons
    [InlineData("anna", "GET", "api/persons/{anna}", HttpStatusCode.OK)]
    [InlineData("sam", "GET", "api/persons/{anna}", HttpStatusCode.OK)]
    [InlineData("ben", "GET", "api/persons/{anna}", HttpStatusCode.OK)]
    [InlineData("carla", "GET", "api/persons/{anna}", HttpStatusCode.Forbidden)]
    [InlineData("carla", "GET", "api/persons/{sam}", HttpStatusCode.OK)]
    [InlineData("carla", "GET", "api/persons/{anna}/location", HttpStatusCode.Forbidden)]
    [InlineData("anna", "GET", "api/persons/{anna}/contacts", HttpStatusCode.OK)]
    [InlineData("sam", "GET", "api/persons/{anna}/contacts", HttpStatusCode.Forbidden)]
    [InlineData("carla", "GET", "api/persons/{anna}/devices", HttpStatusCode.Forbidden)]
    [InlineData("sam", "GET", "api/persons/{anna}/horses", HttpStatusCode.OK)]
    [InlineData("carla", "GET", "api/persons/{anna}/horses", HttpStatusCode.Forbidden)]
    [InlineData("carla", "DELETE", "api/persons/{anna}", HttpStatusCode.Forbidden)]
    [InlineData("sam", "DELETE", "api/persons/{anna}", HttpStatusCode.Forbidden)]
    [InlineData("sam", "DELETE", "api/persons/{kurt}", HttpStatusCode.NoContent)]
    [InlineData("carla", "PUT", "api/persons/{anna}/favourites/{sam}", HttpStatusCode.Forbidden)]
    [InlineData("carla", "PUT", "api/persons/{carla}/favourites/{sam}", HttpStatusCode.NoContent)]
    // horses
    [InlineData("anna", "GET", "api/horses/{storm}", HttpStatusCode.OK)]
    [InlineData("sam", "GET", "api/horses/{storm}", HttpStatusCode.OK)]
    [InlineData("ben", "GET", "api/horses/{storm}", HttpStatusCode.Forbidden)]
    [InlineData("carla", "GET", "api/horses/{storm}", HttpStatusCode.Forbidden)]
    [InlineData("carla", "GET", "api/horses/{storm}/saddles", HttpStatusCode.Forbidden)]
    [InlineData("sam", "DELETE", "api/horses/{storm}", HttpStatusCode.Forbidden)]
    [InlineData("anna", "DELETE", "api/horses/{storm}", HttpStatusCode.NoContent)]
    // devices
    [InlineData("ben", "GET", "api/devices/EP1000", HttpStatusCode.OK)]
    [InlineData("carla", "GET", "api/devices/EP1000/users", HttpStatusCode.Forbidden)]
    [InlineData("ben", "POST", "api/devices/EP1000/users/{carla}", HttpStatusCode.Forbidden)]
    [InlineData("anna", "POST", "api/devices/EP1000/users/{carla}", HttpStatusCode.NoContent)]
    [InlineData("ben", "DELETE", "api/devices/EP1000/users/{ben}", HttpStatusCode.NoContent)]
    [InlineData("ben", "PATCH", "api/devices/EP1000/owner/{ben}", HttpStatusCode.Forbidden)]
    // measurements
    [InlineData("sam", "GET", "api/measurementgroups/{group}", HttpStatusCode.OK)]
    [InlineData("carla", "GET", "api/measurementgroups/{group}", HttpStatusCode.Forbidden)]
    [InlineData("carla", "GET", "api/measurementgroups/horse/{storm}", HttpStatusCode.Forbidden)]
    [InlineData("carla", "GET", "api/measurementgroups/{group}/measurements/average", HttpStatusCode.Forbidden)]
    [InlineData("carla", "DELETE", "api/measurementgroups/{group}/measurements/{measurement}",
                HttpStatusCode.Forbidden)]
    [InlineData("anna", "DELETE", "api/measurementgroups/{group}/measurements/{measurement}",
                HttpStatusCode.NoContent)]
    public async ValueTask Request_DependingOnUser_ChecksAccess(string user, string method, string url,
                                                                HttpStatusCode expectedStatusCode)
    {
        var response = await SendAsUserAsync(new HttpMethod(method), ResolveUrl(url), user);

        response.StatusCode.Should().Be(expectedStatusCode);
    }

    [Fact]
    public async ValueTask Request_NoProfile_ReturnsForbidden()
    {
        var response = await SendAsUserAsync(HttpMethod.Get, ResolveUrl("api/persons/{sam}"), "without-profile");

        ShouldBeProblem(response, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async ValueTask Request_DataDoesNotExist_ReturnsNotFound()
    {
        var response = await SendAsUserAsync(HttpMethod.Get, $"api/horses/{TestData.NonExistingId}", "carla");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("anna", "{anna}", HttpStatusCode.NoContent)]
    [InlineData("sam", "{kurt}", HttpStatusCode.NoContent)]
    [InlineData("sam", "{anna}", HttpStatusCode.Forbidden)]
    [InlineData("carla", "{anna}", HttpStatusCode.Forbidden)]
    public async ValueTask UpdatePerson_DependingOnUser_ChecksAccess(string user, string person,
                                                                     HttpStatusCode expectedStatusCode)
    {
        var request = new UpdatePersonRequest("Moritz", null, null, null, null, null, null, null, null, null);

        var response = await SendAsUserAsync(HttpMethod.Patch, ResolveUrl($"api/persons/{person}"), user,
                                             body: request);

        response.StatusCode.Should().Be(expectedStatusCode);
    }

    [Fact]
    public async ValueTask UpdatePerson_ChangeOwnRoles_ReturnsForbidden()
    {
        var request = new UpdatePersonRequest(null, null, null, null, null, null, null, null, null,
                                              [RoleName.Saddler]);

        var response = await SendAsUserAsync(HttpMethod.Patch, ResolveUrl("api/persons/{anna}"), "anna",
                                             body: request);

        ShouldBeProblem(response, HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(RoleName.Equestrian, HttpStatusCode.Created)]
    [InlineData(RoleName.Saddler, HttpStatusCode.Forbidden)]
    public async ValueTask CreatePerson_DependingOnRole_ChecksAccess(RoleName role, HttpStatusCode expectedStatusCode)
    {
        var request = TestData.CreateAddPersonRequest(role: role);

        var response = await SendAsUserAsync(HttpMethod.Post, "api/persons", "sam", body: request);

        response.StatusCode.Should().Be(expectedStatusCode);
    }

    [Theory]
    [InlineData("anna", "{anna}", HttpStatusCode.Created)]
    [InlineData("sam", "{anna}", HttpStatusCode.Created)]
    [InlineData("carla", "{anna}", HttpStatusCode.Forbidden)]
    public async ValueTask CreateHorse_DependingOnOwner_ChecksAccess(string user, string owner,
                                                                     HttpStatusCode expectedStatusCode)
    {
        var request = TestData.CreateAddHorseRequest(int.Parse(ResolveUrl(owner)), [_breed.Id]);

        var response = await SendAsUserAsync(HttpMethod.Post, "api/horses", user, body: request);

        response.StatusCode.Should().Be(expectedStatusCode);
    }

    [Theory]
    [InlineData("sam", "{storm}", HttpStatusCode.OK)]
    [InlineData("carla", "{storm}", HttpStatusCode.Forbidden)]
    public async ValueTask PatchHorse_DependingOnUser_ChecksAccess(string user, string horse,
                                                                   HttpStatusCode expectedStatusCode)
    {
        var request = new PatchHorseRequest("Blitz", null, null, null, null, null, null);

        var response = await SendAsUserAsync(HttpMethod.Patch, ResolveUrl($"api/horses/{horse}"), user,
                                             body: request);

        response.StatusCode.Should().Be(expectedStatusCode);
    }

    [Theory]
    [InlineData("anna", "{anna}", "{storm}", HttpStatusCode.Created)]
    [InlineData("ben", "{anna}", "{storm}", HttpStatusCode.Forbidden)]
    [InlineData("carla", "{carla}", "{luna}", HttpStatusCode.Forbidden)]
    public async ValueTask CreateMeasurementGroup_DependingOnUser_ChecksAccess(string user, string person,
                                                                               string horse,
                                                                               HttpStatusCode expectedStatusCode)
    {
        var request = TestData.CreateAddMeasurementGroupRequest(int.Parse(ResolveUrl(person)),
                                                                int.Parse(ResolveUrl(horse)), _group.SaddleId,
                                                                DeviceId);

        var response = await SendAsUserAsync(HttpMethod.Post, "api/measurementgroups", user, body: request);

        response.StatusCode.Should().Be(expectedStatusCode);
    }

    private Person CreateAccountPerson(string externalId, RoleName role = RoleName.Equestrian)
    {
        var person = TestData.CreatePerson(char.ToUpperInvariant(externalId[0]) + externalId[1..], role: role);
        person.ExternalId = externalId;
        _persons[externalId] = person;

        return person;
    }

    /// <summary>
    ///     Replaces the placeholders like {anna} or {storm} with the ids generated by the database
    /// </summary>
    private string ResolveUrl(string url)
    {
        foreach (var (name, person) in _persons)
        {
            url = url.Replace($"{{{name}}}", person.Id.ToString());
        }

        return url.Replace("{storm}", _storm.Id.ToString())
                  .Replace("{luna}", _luna.Id.ToString())
                  .Replace("{group}", _group.Id.ToString())
                  .Replace("{measurement}", _measurement.Id.ToString());
    }
}
