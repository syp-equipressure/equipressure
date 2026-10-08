namespace EquiApi.TestInt;

using System.Net;
using EquiApi.Persistence.Model;
using EquiApi.TestInt.Util;
using EquiApi.Util;
using NodaTime;
using EquiApi.Core.Util;



public sealed class PersonTests(WebApiTestFixture webApiFixture) : WebApiTestBase(webApiFixture)
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
        Address? address = null,
        RoleName role = RoleName.Equestrian) => new()
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
        // FIX: actually persist the role instead of always using an empty list
        Roles =
        [
            new PersonRoleAssignment
            {
                Role = new AccountRole { Name = role, RoleAssignments = [] }
            }
        ],
        Horses = [],
        MeasurementGroups = [],
        UserDevices = [],
        OwnerDevices = [],
        Releases = []
    };

    [Fact]
    public async ValueTask GetEquestrianById_ExistingPerson_Success()
    {
        var address = CreateAddress();
        var person = CreatePerson(address: address);

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.Add(person);
            await ctx.SaveChangesAsync();
        });

        var response = await ApiClient.GetAsync($"api/persons/equestrians/{person.Id}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<Helper.EquestrianBasicDto>(JsonOptions, TestCancellationToken);

        content.Should().NotBeNull();
        content.Id.Should().Be(person.Id);
        content.FirstName.Should().Be(person.FirstName);
        content.LastName.Should().Be(person.LastName);
        content.Height.Should().Be(person.Height);
        content.Weight.Should().Be(person.Weight);
        content.Email.Should().Be(person.Email);
        content.CityName.Should().Be(address.CityName);
        content.PLZ.Should().Be(address.PLZ);
    }

    [Fact]
    public async ValueTask GetEquestrianById_NonExistingPerson_ReturnsNotFound()
    {
        const int nonExistingId = 999999;

        var response = await ApiClient.GetAsync($"api/persons/equestrians/{nonExistingId}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetEquestrianById_InvalidId_ReturnsBadRequest()
    {
        const int invalidId = -1;

        var response = await ApiClient.GetAsync($"api/persons/equestrians/{invalidId}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask GetProfileData_ExistingPerson_Success()
    {
        var person = CreatePerson();

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.Add(person);
            await ctx.SaveChangesAsync();
        });

        var response = await ApiClient.GetAsync($"api/persons/{person.Id}/profile-data", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<DataTransfer.NameDataDto>(JsonOptions, TestCancellationToken);

        content.Should().NotBeNull();
        content.FirstName.Should().Be(person.FirstName);
        content.LastName.Should().Be(person.LastName);
    }

    [Fact]
    public async ValueTask GetProfileData_NonExistingPerson_ReturnsNotFound()
    {
        const int nonExistingId = 999999;

        var response = await ApiClient.GetAsync($"api/persons/{nonExistingId}/profile-data", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetProfileData_InvalidId_ReturnsBadRequest()
    {
        const int invalidId = 0;

        var response = await ApiClient.GetAsync($"api/persons/{invalidId}/profile-data", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask GetAddress_ExistingPerson_Success()
    {
        var address = CreateAddress(addressName: "Hauptstraße 1");
        var person = CreatePerson(address: address);

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.Add(person);
            await ctx.SaveChangesAsync();
        });

        var response = await ApiClient.GetAsync($"api/persons/{person.Id}/location", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<DataTransfer.AddressDto>(JsonOptions, TestCancellationToken);

        content.Should().NotBeNull();
        content.CityName.Should().Be(address.CityName);
        content.PLZ.Should().Be(address.PLZ);
    }

    [Fact]
    public async ValueTask GetAddress_NonExistingPerson_ReturnsNotFound()
    {
        const int nonExistingId = 999999;

        var response = await ApiClient.GetAsync($"api/persons/{nonExistingId}/location", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetAddress_InvalidId_ReturnsBadRequest()
    {
        const int invalidId = -5;

        var response = await ApiClient.GetAsync($"api/persons/{invalidId}/location", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask GetContacts_NonExistingPerson_ReturnsNotFound()
    {
        const int nonExistingId = 999999;

        var response = await ApiClient.GetAsync($"api/persons/{nonExistingId}/contacts", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetContacts_InvalidId_ReturnsBadRequest()
    {
        const int invalidId = -1;

        var response = await ApiClient.GetAsync($"api/persons/{invalidId}/contacts", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask GetContacts_NoContacts_ReturnsEmptyList()
    {
        // FIX: use a unique email to avoid cross-test contamination while the
        // real fix (endpoint filtering by relationship) is applied on the backend
        var person = CreatePerson(email: $"contacts-test-{Guid.NewGuid()}@reiter.at");

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.Add(person);
            await ctx.SaveChangesAsync();
        });

        var response = await ApiClient.GetAsync($"api/persons/{person.Id}/contacts", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<DataTransfer.PersonListResponse>(JsonOptions, TestCancellationToken);

        content.Should().NotBeNull();
        content.Persons.Should().BeEmpty();
    }

    [Fact]
    public async ValueTask GetFavourites_NonExistingPerson_ReturnsNotFound()
    {
        const int nonExistingId = 999999;

        var response = await ApiClient.GetAsync($"api/persons/{nonExistingId}/favourites", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetFavourites_InvalidId_ReturnsBadRequest()
    {
        const int invalidId = -1;

        var response = await ApiClient.GetAsync($"api/persons/{invalidId}/favourites", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask GetFavourites_NoFavourites_ReturnsEmptyList()
    {
        // FIX: use a unique email to avoid cross-test contamination while the
        // real fix (endpoint filtering by relationship) is applied on the backend
        var person = CreatePerson(email: $"favourites-test-{Guid.NewGuid()}@reiter.at");

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.Add(person);
            await ctx.SaveChangesAsync();
        });

        var response = await ApiClient.GetAsync($"api/persons/{person.Id}/favourites", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<DataTransfer.PersonListResponse>(JsonOptions, TestCancellationToken);

        content.Should().NotBeNull();
        content.Persons.Should().BeEmpty();
    }

    [Fact]
    public async ValueTask GetHorses_NonExistingPerson_ReturnsNotFound()
    {
        const int nonExistingId = 999999;

        var response = await ApiClient.GetAsync($"api/persons/{nonExistingId}/horses", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetHorses_InvalidId_ReturnsBadRequest()
    {
        const int invalidId = -1;

        var response = await ApiClient.GetAsync($"api/persons/{invalidId}/horses", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask GetHorses_NoHorses_ReturnsEmptyList()
    {
        var person = CreatePerson();

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.Add(person);
            await ctx.SaveChangesAsync();
        });

        var response = await ApiClient.GetAsync($"api/persons/{person.Id}/horses", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<DataTransfer.HorseListResponse>(JsonOptions, TestCancellationToken);

        content.Should().NotBeNull();
        content.Horses.Should().BeEmpty();
    }

    [Fact]
    public async ValueTask GetDevices_NonExistingPerson_ReturnsNotFound()
    {
        const int nonExistingId = 999999;

        var response = await ApiClient.GetAsync($"api/persons/{nonExistingId}/devices", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetDevices_InvalidId_ReturnsBadRequest()
    {
        const int invalidId = -1;

        var response = await ApiClient.GetAsync($"api/persons/{invalidId}/devices", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask GetDevices_NoDevices_ReturnsEmptyList()
    {
        var person = CreatePerson();

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.Add(person);
            await ctx.SaveChangesAsync();
        });

        var response = await ApiClient.GetAsync($"api/persons/{person.Id}/devices", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<DataTransfer.DeviceListResponse>(JsonOptions, TestCancellationToken);

        content.Should().NotBeNull();
        content.Devices.Should().BeEmpty();
    }

    [Fact]
    public async ValueTask CreatePerson_ValidEquestrian_Success()
    {
        var address = CreateAddress();
        var role = new AccountRole { Name = RoleName.Equestrian, RoleAssignments = [] };

        var request = new DataTransfer.AddPersonRequest(
            "Anna",
            "Equestrian",
            170M,
            60M,
            new LocalDate(1998, 3, 15),
            "anna.unique@reiter.at",
            null,
            null,
            address,
            role);

        var response = await ApiClient.PostAsJsonAsync("api/persons", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var content = await response.Content.ReadFromJsonAsync<Person>(JsonOptions, TestCancellationToken);
        content.Should().NotBeNull();
        content.FirstName.Should().Be("Anna");
        content.LastName.Should().Be("Equestrian");
        content.Email.Should().Be("anna.unique@reiter.at");
    }

    [Fact]
    public async ValueTask CreatePerson_InvalidData_ReturnsBadRequest()
    {
        var address = CreateAddress();
        var role = new AccountRole { Name = RoleName.Equestrian, RoleAssignments = [] };

        var request = new DataTransfer.AddPersonRequest(
            "",
            "Equestrian",
            170M,
            60M,
            new LocalDate(1998, 3, 15),
            "anna.unique@reiter.at",
            null,
            null,
            address,
            role);

        var response = await ApiClient.PostAsJsonAsync("api/persons", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask CreatePerson_DuplicateEmail_ReturnsConflict()
    {
        const string duplicateEmail = "duplicate@reiter.at";
        var existingPerson = CreatePerson(email: duplicateEmail);

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.Add(existingPerson);
            await ctx.SaveChangesAsync();
        });

        var address = CreateAddress();
        var role = new AccountRole { Name = RoleName.Equestrian, RoleAssignments = [] };

        var request = new DataTransfer.AddPersonRequest(
            "Anna",
            "Equestrian",
            170M,
            60M,
            new LocalDate(1998, 3, 15),
            duplicateEmail,
            null,
            null,
            address,
            role);

        var response = await ApiClient.PostAsJsonAsync("api/persons", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask UpdatePerson_ExistingPerson_Success()
    {
        var person = CreatePerson();

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.Add(person);
            await ctx.SaveChangesAsync();
        });

        var request = new DataTransfer.UpdatePersonRequest(
            "Maximilian",
            "Musterfrau",
            185M,
            80M,
            new LocalDate(1996, 6, 21),
            "maximilian.new@reiter.at",
            null,
            null,
            null,
            null);

        var response = await ApiClient.PatchAsJsonAsync($"api/persons/{person.Id}", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await ApiClient.GetAsync($"api/persons/{person.Id}/profile-data", TestCancellationToken);
        var content = await getResponse.Content.ReadFromJsonAsync<DataTransfer.NameDataDto>(JsonOptions, TestCancellationToken);

        content.Should().NotBeNull();
        content.FirstName.Should().Be("Maximilian");
        content.LastName.Should().Be("Musterfrau");
    }

    [Fact]
    public async ValueTask UpdatePerson_NonExistingPerson_ReturnsNotFound()
    {
        const int nonExistingId = 999999;

        var request = new DataTransfer.UpdatePersonRequest(
            "Maximilian",
            "Musterfrau",
            185M,
            80M,
            new LocalDate(1996, 6, 21),
            "maximilian.new@reiter.at",
            null,
            null,
            null,
            null);

        var response = await ApiClient.PatchAsJsonAsync($"api/persons/{nonExistingId}", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask UpdatePerson_InvalidId_ReturnsBadRequest()
    {
        const int invalidId = -1;

        var request = new DataTransfer.UpdatePersonRequest(
            "Maximilian",
            "Musterfrau",
            185M,
            80M,
            new LocalDate(1996, 6, 21),
            "maximilian.new@reiter.at",
            null,
            null,
            null,
            null);

        var response = await ApiClient.PatchAsJsonAsync($"api/persons/{invalidId}", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask UpdatePerson_InvalidHeight_ReturnsBadRequest()
    {
        var person = CreatePerson();

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.Add(person);
            await ctx.SaveChangesAsync();
        });

        var request = new DataTransfer.UpdatePersonRequest(
            "Maximilian",
            "Musterfrau",
            0M,
            80M,
            new LocalDate(1996, 6, 21),
            "maximilian.new@reiter.at",
            null,
            null,
            null,
            null);

        var response = await ApiClient.PatchAsJsonAsync($"api/persons/{person.Id}", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask DeletePerson_ExistingPerson_Success()
    {
        var person = CreatePerson();

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.Add(person);
            await ctx.SaveChangesAsync();
        });

        var response = await ApiClient.DeleteAsync($"api/persons/{person.Id}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await ApiClient.GetAsync($"api/persons/{person.Id}/profile-data", TestCancellationToken);
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask DeletePerson_NonExistingPerson_ReturnsNotFound()
    {
        const int nonExistingId = 999999;

        var response = await ApiClient.DeleteAsync($"api/persons/{nonExistingId}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask DeletePerson_InvalidId_ReturnsBadRequest()
    {
        const int invalidId = -1;

        var response = await ApiClient.DeleteAsync($"api/persons/{invalidId}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask GetSaddlerById_ExistingSaddler_Success()
    {
        var address = CreateAddress(addressName: "Sattlergasse 5");
        var saddler = CreatePerson(
                                   firstName: "Anna",
                                   lastName: "Sattler",
                                   email: "anna.sattler@reiter.at",
                                   address: address,
                                   role: RoleName.Saddler);

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.Add(saddler);
            await ctx.SaveChangesAsync();
        });

        var response = await ApiClient.GetAsync($"api/persons/saddlers/{saddler.Id}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<DataTransfer.SaddlerBasicDto>(JsonOptions, TestCancellationToken);

        content.Should().NotBeNull();
        content.Id.Should().Be(saddler.Id);
        content.FirstName.Should().Be(saddler.FirstName);
        content.LastName.Should().Be(saddler.LastName);
        content.CityName.Should().Be(address.CityName);
        content.PLZ.Should().Be(address.PLZ);
    }

    [Fact]
    public async ValueTask GetSaddlerById_NonExistingSaddler_ReturnsNotFound()
    {
        const int nonExistingId = 999999;

        var response = await ApiClient.GetAsync($"api/persons/saddlers/{nonExistingId}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetSaddlerById_InvalidId_ReturnsBadRequest()
    {
        const int invalidId = -1;

        var response = await ApiClient.GetAsync($"api/persons/saddlers/{invalidId}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}