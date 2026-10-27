using System.Net;
using EquiApi.Dtos;
using EquiApi.Persistence.Model;
using EquiApi.TestInt.Util;

namespace EquiApi.TestInt;

public sealed class DeviceTests(WebApiTestFixture webApiFixture) : WebApiTestBase(webApiFixture)
{
    private const string DeviceId = "device-1";
    private const string AccountExternalId = "0e6982b9-0a93-4fdd-8af5-65b85d73b52f";

    [Fact]
    public async ValueTask GetDeviceById_ExistingDevice_ReturnsDeviceWithOwnerAndUsers()
    {
        var owner = TestData.CreatePerson("Max");
        var user = TestData.CreatePerson("Anna");
        var category = TestData.CreateDeviceCategory();
        await SeedAsync(TestData.CreateDevice(DeviceId, owner, category, user));

        var response = await ApiClient.GetAsync($"api/devices/{DeviceId}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<DeviceDto>(response);
        content.Id.Should().Be(DeviceId);
        content.CategoryId.Should().Be(category.Id);
        content.CategoryName.Should().Be(category.Name);
        content.Owner.Id.Should().Be(owner.Id);
        content.Users.Should().ContainSingle().Which.Id.Should().Be(user.Id);
    }

    [Fact]
    public async ValueTask GetDeviceById_NonExistingDevice_ReturnsNotFound()
    {
        var response = await ApiClient.GetAsync("api/devices/non-existing-device", TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetOwner_ExistingDevice_ReturnsOwner()
    {
        var owner = TestData.CreatePerson("Max", "Mustermann", "max@reiter.at");
        await SeedAsync(TestData.CreateDevice(DeviceId, owner, TestData.CreateDeviceCategory()));

        var response = await ApiClient.GetAsync($"api/devices/{DeviceId}/owner", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<PersonDto>(response);
        content.Should().Be(new PersonDto(owner.Id, "Max", "Mustermann", "max@reiter.at"));
    }

    [Fact]
    public async ValueTask GetOwner_NonExistingDevice_ReturnsNotFound()
    {
        var response = await ApiClient.GetAsync("api/devices/non-existing-device/owner", TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetUsers_DeviceWithUsers_ReturnsUsersWithoutOwner()
    {
        var owner = TestData.CreatePerson("Max");
        var firstUser = TestData.CreatePerson("Anna", "Auer");
        var secondUser = TestData.CreatePerson("Berta", "Berger");
        await SeedAsync(TestData.CreateDevice(DeviceId, owner, TestData.CreateDeviceCategory(), secondUser,
                                              firstUser));

        var response = await ApiClient.GetAsync($"api/devices/{DeviceId}/users", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<PersonListResponse>(response);
        content.Persons.Select(p => p.Id).Should().Equal(firstUser.Id, secondUser.Id);
    }

    [Fact]
    public async ValueTask GetUsers_DeviceWithoutUsers_ReturnsEmptyList()
    {
        await SeedAsync(TestData.CreateDevice(DeviceId, TestData.CreatePerson(), TestData.CreateDeviceCategory()));

        var response = await ApiClient.GetAsync($"api/devices/{DeviceId}/users", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<PersonListResponse>(response);
        content.Persons.Should().BeEmpty();
    }

    [Fact]
    public async ValueTask GetUsers_NonExistingDevice_ReturnsNotFound()
    {
        var response = await ApiClient.GetAsync("api/devices/non-existing-device/users", TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask CreateDevice_ValidData_ReturnsCreatedDevice()
    {
        var owner = TestData.CreatePerson();
        var category = TestData.CreateDeviceCategory();
        await SeedAsync(owner, category);
        var request = new AddDeviceRequest("device-new", owner.Id, category.Id);

        var response = await ApiClient.PostAsJsonAsync("api/devices", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await ReadContentAsync<DeviceDto>(response);
        content.Id.Should().Be("device-new");
        content.Owner.Id.Should().Be(owner.Id);
        content.CategoryId.Should().Be(category.Id);
        content.Users.Should().BeEmpty();

        response.Headers.Location.Should().NotBeNull();
        var getResponse = await ApiClient.GetAsync(response.Headers.Location, TestCancellationToken);
        var storedDevice = await ReadContentAsync<DeviceDto>(getResponse);
        storedDevice.Should().BeEquivalentTo(content);
    }

    [Fact]
    public async ValueTask CreateDevice_OwnerNotFound_ReturnsNotFound()
    {
        var category = TestData.CreateDeviceCategory();
        await SeedAsync(category);
        var request = new AddDeviceRequest("device-new", TestData.NonExistingId, category.Id);

        var response = await ApiClient.PostAsJsonAsync("api/devices", request, JsonOptions, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask CreateDevice_CategoryNotFound_ReturnsNotFound()
    {
        var owner = TestData.CreatePerson();
        await SeedAsync(owner);
        var request = new AddDeviceRequest("device-new", owner.Id, TestData.NonExistingId);

        var response = await ApiClient.PostAsJsonAsync("api/devices", request, JsonOptions, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask CreateDevice_DeviceAlreadyExists_ReturnsConflict()
    {
        var owner = TestData.CreatePerson();
        var category = TestData.CreateDeviceCategory();
        await SeedAsync(TestData.CreateDevice(DeviceId, owner, category));
        var request = new AddDeviceRequest(DeviceId, owner.Id, category.Id);

        var response = await ApiClient.PostAsJsonAsync("api/devices", request, JsonOptions, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask CreateDevice_InvalidData_ReturnsBadRequest()
    {
        var request = new AddDeviceRequest("", 1, 1);

        var response = await ApiClient.PostAsJsonAsync("api/devices", request, JsonOptions, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask AddUser_ValidData_AddsUser()
    {
        var newUser = TestData.CreatePerson("Anna");
        await SeedAsync(newUser, TestData.CreateDevice(DeviceId, TestData.CreatePerson(), TestData.CreateDeviceCategory()));

        var response = await ApiClient.PostAsync($"api/devices/{DeviceId}/users/{newUser.Id}", null,
                                                 TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var usersResponse = await ApiClient.GetAsync($"api/devices/{DeviceId}/users", TestCancellationToken);
        var content = await ReadContentAsync<PersonListResponse>(usersResponse);
        content.Persons.Should().ContainSingle().Which.Id.Should().Be(newUser.Id);
    }

    [Fact]
    public async ValueTask AddUser_NonExistingDevice_ReturnsNotFound()
    {
        var newUser = TestData.CreatePerson();
        await SeedAsync(newUser);

        var response = await ApiClient.PostAsync($"api/devices/non-existing-device/users/{newUser.Id}", null,
                                                 TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask AddUser_NonExistingPerson_ReturnsNotFound()
    {
        await SeedAsync(TestData.CreateDevice(DeviceId, TestData.CreatePerson(), TestData.CreateDeviceCategory()));

        var response = await ApiClient.PostAsync($"api/devices/{DeviceId}/users/{TestData.NonExistingId}", null,
                                                 TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask AddUser_PersonAlreadyUser_ReturnsConflict()
    {
        var user = TestData.CreatePerson("Anna");
        await SeedAsync(TestData.CreateDevice(DeviceId, TestData.CreatePerson(), TestData.CreateDeviceCategory(),
                                              user));

        var response = await ApiClient.PostAsync($"api/devices/{DeviceId}/users/{user.Id}", null,
                                                 TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask AddUser_PersonIsOwner_ReturnsConflict()
    {
        var owner = TestData.CreatePerson();
        await SeedAsync(TestData.CreateDevice(DeviceId, owner, TestData.CreateDeviceCategory()));

        var response = await ApiClient.PostAsync($"api/devices/{DeviceId}/users/{owner.Id}", null,
                                                 TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask AddUser_LimitReached_ReturnsConflict()
    {
        var newUser = TestData.CreatePerson("Berta");
        await SeedAsync(newUser, TestData.CreateDevice(DeviceId, TestData.CreatePerson(),
                                                       TestData.CreateDeviceCategory(numOfAllowedPeople: 1),
                                                       TestData.CreatePerson("Anna")));

        var response = await ApiClient.PostAsync($"api/devices/{DeviceId}/users/{newUser.Id}", null,
                                                 TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask AddUser_InvalidUserId_ReturnsBadRequest()
    {
        var response = await ApiClient.PostAsync($"api/devices/{DeviceId}/users/-1", null, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask RemoveUser_ExistingUser_RemovesUser()
    {
        var user = TestData.CreatePerson("Anna");
        var remainingUser = TestData.CreatePerson("Berta");
        await SeedAsync(TestData.CreateDevice(DeviceId, TestData.CreatePerson(), TestData.CreateDeviceCategory(),
                                              user, remainingUser));

        var response = await ApiClient.DeleteAsync($"api/devices/{DeviceId}/users/{user.Id}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var usersResponse = await ApiClient.GetAsync($"api/devices/{DeviceId}/users", TestCancellationToken);
        var content = await ReadContentAsync<PersonListResponse>(usersResponse);
        content.Persons.Should().ContainSingle().Which.Id.Should().Be(remainingUser.Id);
    }

    [Fact]
    public async ValueTask RemoveUser_PersonIsOwner_ReturnsConflict()
    {
        var owner = TestData.CreatePerson();
        await SeedAsync(TestData.CreateDevice(DeviceId, owner, TestData.CreateDeviceCategory(),
                                              TestData.CreatePerson("Anna")));

        var response = await ApiClient.DeleteAsync($"api/devices/{DeviceId}/users/{owner.Id}", TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask RemoveUser_PersonIsNotUser_ReturnsNotFound()
    {
        var otherPerson = TestData.CreatePerson("Anna");
        await SeedAsync(otherPerson,
                        TestData.CreateDevice(DeviceId, TestData.CreatePerson(), TestData.CreateDeviceCategory()));

        var response = await ApiClient.DeleteAsync($"api/devices/{DeviceId}/users/{otherPerson.Id}",
                                                   TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask RemoveUser_NonExistingDevice_ReturnsNotFound()
    {
        var response = await ApiClient.DeleteAsync($"api/devices/non-existing-device/users/{TestData.NonExistingId}",
                                                   TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask ChangeOwner_UserBecomesOwner_SwapsOwnerAndUser()
    {
        var previousOwner = TestData.CreatePerson("Max");
        var newOwner = TestData.CreatePerson("Anna");
        await SeedAsync(TestData.CreateDevice(DeviceId, previousOwner, TestData.CreateDeviceCategory(), newOwner));

        var response = await ApiClient.PatchAsync($"api/devices/{DeviceId}/owner/{newOwner.Id}", null,
                                                  TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var deviceResponse = await ApiClient.GetAsync($"api/devices/{DeviceId}", TestCancellationToken);
        var content = await ReadContentAsync<DeviceDto>(deviceResponse);
        content.Owner.Id.Should().Be(newOwner.Id);
        content.Users.Should().ContainSingle().Which.Id.Should().Be(previousOwner.Id);
    }

    [Fact]
    public async ValueTask ChangeOwner_NonExistingPerson_ReturnsNotFound()
    {
        await SeedAsync(TestData.CreateDevice(DeviceId, TestData.CreatePerson(), TestData.CreateDeviceCategory()));

        var response = await ApiClient.PatchAsync($"api/devices/{DeviceId}/owner/{TestData.NonExistingId}", null,
                                                  TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask ChangeOwner_NoRoomForPreviousOwner_ReturnsConflict()
    {
        var newOwner = TestData.CreatePerson("Berta");
        await SeedAsync(newOwner, TestData.CreateDevice(DeviceId, TestData.CreatePerson("Max"),
                                                        TestData.CreateDeviceCategory(numOfAllowedPeople: 1),
                                                        TestData.CreatePerson("Anna")));

        var response = await ApiClient.PatchAsync($"api/devices/{DeviceId}/owner/{newOwner.Id}", null,
                                                  TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData(RoleName.Equestrian)]
    [InlineData(RoleName.Saddler)]
    public async ValueTask ClaimDevice_ValidData_ReturnsCreatedDeviceAndAssignsRoleOfCategory(RoleName categoryRole)
    {
        var person = await SeedAccountPersonAsync("Hauptstraße 1");
        var category = TestData.CreateDeviceCategory(role: categoryRole);
        await SeedAsync(category);

        var response = await ClaimAsync(category.Id);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await ReadContentAsync<DeviceDto>(response);
        content.Id.Should().Be(DeviceId);
        content.CategoryId.Should().Be(category.Id);
        content.Owner.Id.Should().Be(person.Id);

        var meResponse = await SendAsUserAsync(HttpMethod.Get, "api/account/me", AccountExternalId);
        var me = await ReadContentAsync<PersonDetailDto>(meResponse);
        me.Roles.Should().Equal(categoryRole);
    }

    [Fact]
    public async ValueTask ClaimDevice_SaddlerDeviceWithoutStreet_ReturnsConflict()
    {
        await SeedAccountPersonAsync(null);
        var category = TestData.CreateDeviceCategory(role: RoleName.Saddler);
        await SeedAsync(category);

        var response = await ClaimAsync(category.Id);

        ShouldBeProblem(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask ClaimDevice_AlreadyClaimed_ReturnsConflict()
    {
        await SeedAccountPersonAsync(null);
        var category = TestData.CreateDeviceCategory();
        await SeedAsync(TestData.CreateDevice(DeviceId, TestData.CreatePerson("Other"), category));

        var response = await ClaimAsync(category.Id);

        ShouldBeProblem(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask ClaimDevice_ProfileNotSetUp_ReturnsNotFound()
    {
        var category = TestData.CreateDeviceCategory();
        await SeedAsync(category);

        var response = await ClaimAsync(category.Id);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask ClaimDevice_CategoryNotFound_ReturnsNotFound()
    {
        await SeedAccountPersonAsync(null);

        var response = await ClaimAsync(TestData.NonExistingId);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask ClaimDevice_InvalidCategoryId_ReturnsBadRequest()
    {
        await SeedAccountPersonAsync(null);

        var response = await ClaimAsync(0);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask ClaimDevice_NotLoggedIn_ReturnsUnauthorized()
    {
        var response = await AnonymousClient.PostAsJsonAsync($"api/devices/{DeviceId}/claim",
                                                             new ClaimDeviceRequest(1), JsonOptions,
                                                             TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    ///     Seeds the person of a new account: linked to Keycloak, but without role until a device is claimed
    /// </summary>
    private async ValueTask<Person> SeedAccountPersonAsync(string? street)
    {
        var person = TestData.CreatePerson("Anna", address: TestData.CreateAddress(street));
        person.ExternalId = AccountExternalId;
        person.Roles = [];
        await SeedAsync(person);

        return person;
    }

    private async ValueTask<HttpResponseMessage> ClaimAsync(int categoryId) =>
        await SendAsUserAsync(HttpMethod.Post, $"api/devices/{DeviceId}/claim", AccountExternalId,
                              body: new ClaimDeviceRequest(categoryId));
}
