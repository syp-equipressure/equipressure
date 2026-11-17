using System.Net;
using EquiApi.TestInt.Util;

namespace EquiApi.TestInt;

public sealed class AuthenticationTests(WebApiTestFixture webApiFixture) : WebApiTestBase(webApiFixture)
{
    [Theory]
    [InlineData("api/persons/1")]
    [InlineData("api/persons/saddlers")]
    [InlineData("api/horses/1")]
    [InlineData("api/horses/1/image")]
    [InlineData("api/persons/1/image")]
    [InlineData("api/locations")]
    [InlineData("api/devices/EP0001")]
    [InlineData("api/measurementgroups/1")]
    public async ValueTask Get_NotLoggedIn_ReturnsUnauthorized(string url)
    {
        var response = await AnonymousClient.GetAsync(url, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}