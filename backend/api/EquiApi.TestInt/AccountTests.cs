using System.Net;
using EquiApi.Dtos;
using EquiApi.Persistence.Model;
using EquiApi.TestInt.Util;
using EquiApi.Util;
using NSubstitute;
using OneOf.Types;

namespace EquiApi.TestInt;

public sealed class AccountTests(WebApiTestFixture webApiFixture) : WebApiTestBase(webApiFixture)
{
    private const string ExternalId = "0e6982b9-0a93-4fdd-8af5-65b85d73b52f";
    private const string Email = "anna@reiter.at";
    private const string Password = "Testpasswort1";

    [Fact]
    public async ValueTask GetIdentity_LoggedIn_ReturnsClaimsOfToken()
    {
        var response = await SendAsUserAsync(HttpMethod.Get, "api/account/identity");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<IdentityDto>(response);
        content.Should().Be(new IdentityDto(ExternalId, Email, null, null));
    }

    [Theory]
    [InlineData("api/account/identity")]
    [InlineData("api/account/me")]
    public async ValueTask Get_NotLoggedIn_ReturnsUnauthorized(string url)
    {
        var response = await AnonymousClient.GetAsync(url, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async ValueTask GetMe_LinkedPerson_ReturnsPerson()
    {
        var person = TestData.CreatePerson(email: Email);
        person.ExternalId = ExternalId;
        var otherPerson = TestData.CreatePerson("Other");
        otherPerson.ExternalId = "other-account";
        await SeedAsync(person, otherPerson);

        var response = await SendAsUserAsync(HttpMethod.Get, "api/account/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<PersonDetailDto>(response);
        content.Id.Should().Be(person.Id);
        content.Email.Should().Be(Email);
    }

    [Fact]
    public async ValueTask GetMe_NoLinkedPerson_ReturnsNotFound()
    {
        await SeedAsync(TestData.CreatePerson(email: Email));

        var response = await SendAsUserAsync(HttpMethod.Get, "api/account/me");

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask CreateAccount_ValidData_ReturnsCreatedPersonWithEmailFromToken()
    {
        var request = TestData.CreateAccountRequest();

        var response = await SendAsUserAsync(HttpMethod.Post, "api/account", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await ReadContentAsync<PersonDetailDto>(response);
        content.FirstName.Should().Be(request.FirstName);
        content.Email.Should().Be(Email);
        content.Roles.Should().BeEmpty("the role is assigned when claiming a device");
        content.CreatedAt.Should().NotBe(default, "the database sets the member since date");

        var meResponse = await SendAsUserAsync(HttpMethod.Get, "api/account/me");
        var me = await ReadContentAsync<PersonDetailDto>(meResponse);
        me.Should().BeEquivalentTo(content);
    }

    [Fact]
    public async ValueTask CreateAccount_AlreadySetUp_ReturnsConflict()
    {
        var person = TestData.CreatePerson(email: Email);
        person.ExternalId = ExternalId;
        await SeedAsync(person);

        var response = await SendAsUserAsync(HttpMethod.Post, "api/account", TestData.CreateAccountRequest());

        ShouldBeProblem(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask CreateAccount_EmailUsedByOtherPerson_ReturnsConflict()
    {
        await SeedAsync(TestData.CreatePerson(email: Email));

        var response = await SendAsUserAsync(HttpMethod.Post, "api/account", TestData.CreateAccountRequest());

        ShouldBeProblem(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask CreateAccount_InvalidData_ReturnsBadRequest()
    {
        var response = await SendAsUserAsync(HttpMethod.Post, "api/account",
                                              TestData.CreateAccountRequest(firstName: ""));

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask CreateAccount_NotLoggedIn_ReturnsUnauthorized()
    {
        var response = await AnonymousClient.PostAsJsonAsync("api/account", TestData.CreateAccountRequest(),
                                                             JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async ValueTask Register_ValidData_CreatesTrimmedUserWithoutLogin()
    {
        KeycloakAdminClient.CreateUserAsync(Arg.Any<NewKeycloakUser>(), Arg.Any<CancellationToken>())
                           .Returns(new Success());
        var request = new RegisterRequest(" neu@reiter.at ", Password, " Nora ", "Neu");

        var response = await AnonymousClient.PostAsJsonAsync("api/account/register", request, JsonOptions,
                                                             TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await KeycloakAdminClient.Received().CreateUserAsync(
            new NewKeycloakUser("neu@reiter.at", Password, "Nora", "Neu"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async ValueTask Register_EmailAlreadyUsed_ReturnsConflict()
    {
        KeycloakAdminClient.CreateUserAsync(Arg.Any<NewKeycloakUser>(), Arg.Any<CancellationToken>())
                           .Returns(new IKeycloakAdminClient.EmailAlreadyUsed());

        var response = await AnonymousClient.PostAsJsonAsync(
            "api/account/register", new RegisterRequest(Email, Password, "Nora", "Neu"), JsonOptions,
            TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask Register_PasswordRejectedByKeycloak_ReturnsBadRequest()
    {
        KeycloakAdminClient.CreateUserAsync(Arg.Any<NewKeycloakUser>(), Arg.Any<CancellationToken>())
                           .Returns(new IKeycloakAdminClient.PasswordRejected());

        var response = await AnonymousClient.PostAsJsonAsync(
            "api/account/register", new RegisterRequest(Email, Password, "Nora", "Neu"), JsonOptions,
            TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("keine-email", Password, "Nora", "Neu")]
    [InlineData("neu@reiter.at", "kurz", "Nora", "Neu")]
    [InlineData("neu@reiter.at", Password, "", "Neu")]
    [InlineData("neu@reiter.at", Password, "Nora", "")]
    public async ValueTask Register_InvalidData_ReturnsBadRequest(string email, string password, string firstName,
                                                                  string lastName)
    {
        var response = await AnonymousClient.PostAsJsonAsync(
            "api/account/register", new RegisterRequest(email, password, firstName, lastName), JsonOptions,
            TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    private async ValueTask<HttpResponseMessage> SendAsUserAsync(HttpMethod method, string url,
                                                                 object? body = null) =>
        await SendAsUserAsync(method, url, ExternalId, Email, body);
}