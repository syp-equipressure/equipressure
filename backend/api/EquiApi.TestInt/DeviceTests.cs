using System.Net;
using EquiApi.Persistence.Model;
using EquiApi.TestInt.Util;
using EquiApi.Util;
using NodaTime;

namespace EquiApi.TestInt;

public sealed class DeviceTests(WebApiTestFixture webApiFixture) : WebApiTestBase(webApiFixture)
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

    private static DeviceCategory CreateCategory(int numOfAllowedPeople = 5, string name = "Standard") => new()
    {
        NumOfAllowedPeople = numOfAllowedPeople,
        Name = name,
        Devices = []
    };

    private static MeasurementDevice CreateDevice(string id, Person owner, DeviceCategory category) => new()
    {
        Id = id,
        OwnerId = owner.Id,
        Owner = owner,
        CategoryId = category.Id,
        Category = category,
        MeasurementGroups = [],
        Users = []
    };

    private static DeviceUser CreateDeviceUser(Person user, MeasurementDevice device) => new()
    {
        UserId = user.Id,
        User = user,
        DeviceId = device.Id,
        Device = device
    };

    [Fact]
    public async ValueTask GetDevicesByUserId_UserWithDevices_Success()
    {
        var owner = CreatePerson();
        var category = CreateCategory();

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.Add(owner);
            ctx.DeviceCategories.Add(category);
            await ctx.SaveChangesAsync();

            var device = CreateDevice("device-1", owner, category);
            ctx.Devices.Add(device);
            await ctx.SaveChangesAsync();

            // FIX: Explicitly link the owner to the device in the intermediate table
            // because the GET endpoint filters by the registered Users collection.
            ctx.DeviceUsers.Add(CreateDeviceUser(owner, device));
            await ctx.SaveChangesAsync();
        });

        var response = await ApiClient.GetAsync($"api/devices/{owner.Id}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<DataTransfer.DeviceListResponse>(JsonOptions, TestCancellationToken);

        content.Should().NotBeNull();
        content.Devices.Should().ContainSingle(d => d.Id == "device-1");
    }

    [Fact]
    public async ValueTask GetDevicesByUserId_NonExistingUser_ReturnsNotFound()
    {
        const int nonExistingUserId = 999999;

        var response = await ApiClient.GetAsync($"api/devices/{nonExistingUserId}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetDevicesByUserId_InvalidId_ReturnsBadRequest()
    {
        const int invalidUserId = -1;

        var response = await ApiClient.GetAsync($"api/devices/{invalidUserId}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask GetOwnerByDeviceId_ExistingDevice_Success()
    {
        var owner = CreatePerson();
        var category = CreateCategory();

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.Add(owner);
            ctx.DeviceCategories.Add(category);
            await ctx.SaveChangesAsync();

            ctx.Devices.Add(CreateDevice("device-1", owner, category));
            await ctx.SaveChangesAsync();
        });

        var response = await ApiClient.GetAsync("api/devices/device-1/owner", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<DataTransfer.PersonDto>(JsonOptions, TestCancellationToken);

        content.Should().NotBeNull();
        content.Id.Should().Be(owner.Id);
        content.FirstName.Should().Be(owner.FirstName);
        content.LastName.Should().Be(owner.LastName);
    }

    [Fact]
    public async ValueTask GetOwnerByDeviceId_NonExistingDevice_ReturnsNotFound()
    {
        var response = await ApiClient.GetAsync("api/devices/non-existing-device/owner", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetUsersByDeviceId_ExistingDeviceWithUsers_Success()
    {
        var owner = CreatePerson();
        var secondUser = CreatePerson(firstName: "Anna", lastName: "Helferin", email: "anna.helferin@reiter.at");
        var category = CreateCategory();

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.AddRange(owner, secondUser);
            ctx.DeviceCategories.Add(category);
            await ctx.SaveChangesAsync();

            var device = CreateDevice("device-1", owner, category);
            ctx.Devices.Add(device);
            await ctx.SaveChangesAsync();

            ctx.DeviceUsers.AddRange(
                CreateDeviceUser(owner, device),
                CreateDeviceUser(secondUser, device));
            await ctx.SaveChangesAsync();
        });

        var response = await ApiClient.GetAsync("api/devices/device-1/users", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<DataTransfer.PersonListResponse>(JsonOptions, TestCancellationToken);

        content.Should().NotBeNull();
        content.Persons.Should().HaveCount(2);
        content.Persons.Should().Contain(p => p.Id == owner.Id);
        content.Persons.Should().Contain(p => p.Id == secondUser.Id);
    }

    [Fact]
    public async ValueTask GetUsersByDeviceId_NonExistingDevice_ReturnsNotFound()
    {
        var response = await ApiClient.GetAsync("api/devices/non-existing-device/users", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetUsersByDeviceId_DeviceWithoutUsers_ReturnsUnprocessableEntity()
    {
        var owner = CreatePerson();
        var category = CreateCategory();

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.Add(owner);
            ctx.DeviceCategories.Add(category);
            await ctx.SaveChangesAsync();

            ctx.Devices.Add(CreateDevice("device-1", owner, category));
            await ctx.SaveChangesAsync();
        });

        var response = await ApiClient.GetAsync("api/devices/device-1/users", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async ValueTask CreateDevice_ValidData_Success()
    {
        var owner = CreatePerson();
        var category = CreateCategory();

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.Add(owner);
            ctx.DeviceCategories.Add(category);
            await ctx.SaveChangesAsync();
        });

        var request = new DataTransfer.AddDeviceRequest("device-new", owner.Id, category.Id);

        var response = await ApiClient.PostAsJsonAsync("api/devices", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        

        var getResponse = await ApiClient.GetAsync($"api/devices/{owner.Id}", TestCancellationToken);
        var content = await getResponse.Content.ReadFromJsonAsync<DataTransfer.DeviceListResponse>(JsonOptions, TestCancellationToken);

        content.Should().NotBeNull();
        content.Devices.Should().ContainSingle(d => d.Id == "device-new");
    }

    [Fact]
    public async ValueTask CreateDevice_OwnerNotFound_ReturnsNotFound()
    {
        var category = CreateCategory();

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.DeviceCategories.Add(category);
            await ctx.SaveChangesAsync();
        });

        const int nonExistingOwnerId = 999999;
        var request = new DataTransfer.AddDeviceRequest("device-new", nonExistingOwnerId, category.Id);

        var response = await ApiClient.PostAsJsonAsync("api/devices", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask CreateDevice_CategoryNotFound_ReturnsNotFound()
    {
        var owner = CreatePerson();

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.Add(owner);
            await ctx.SaveChangesAsync();
        });

        const int nonExistingCategoryId = 999999;
        var request = new DataTransfer.AddDeviceRequest("device-new", owner.Id, nonExistingCategoryId);

        var response = await ApiClient.PostAsJsonAsync("api/devices", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask CreateDevice_InvalidData_ReturnsBadRequest()
    {
        var owner = CreatePerson();
        var category = CreateCategory();

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.Add(owner);
            ctx.DeviceCategories.Add(category);
            await ctx.SaveChangesAsync();
        });

        var request = new DataTransfer.AddDeviceRequest("", owner.Id, category.Id);

        var response = await ApiClient.PostAsJsonAsync("api/devices", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask AddUserToDevice_ValidData_Success()
    {
        var owner = CreatePerson();
        var newUser = CreatePerson(firstName: "Anna", lastName: "Helferin", email: "anna.helferin@reiter.at");
        var category = CreateCategory(numOfAllowedPeople: 5);

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.AddRange(owner, newUser);
            ctx.DeviceCategories.Add(category);
            await ctx.SaveChangesAsync();

            ctx.Devices.Add(CreateDevice("device-1", owner, category));
            await ctx.SaveChangesAsync();
        });

        var response = await ApiClient.PostAsync($"api/devices/device-1/add/{newUser.Id}", null, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var getResponse = await ApiClient.GetAsync("api/devices/device-1/users", TestCancellationToken);
        var content = await getResponse.Content.ReadFromJsonAsync<DataTransfer.PersonListResponse>(JsonOptions, TestCancellationToken);

        content.Should().NotBeNull();
        content.Persons.Should().Contain(p => p.Id == newUser.Id);
    }

    [Fact]
    public async ValueTask AddUserToDevice_DeviceNotFound_ReturnsNotFound()
    {
        var newUser = CreatePerson();

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.Add(newUser);
            await ctx.SaveChangesAsync();
        });

        var response = await ApiClient.PostAsync($"api/devices/non-existing-device/add/{newUser.Id}", null, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask AddUserToDevice_UserNotFound_ReturnsNotFound()
    {
        var owner = CreatePerson();
        var category = CreateCategory();

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.Add(owner);
            ctx.DeviceCategories.Add(category);
            await ctx.SaveChangesAsync();

            ctx.Devices.Add(CreateDevice("device-1", owner, category));
            await ctx.SaveChangesAsync();
        });

        const int nonExistingUserId = 999999;

        var response = await ApiClient.PostAsync($"api/devices/device-1/add/{nonExistingUserId}", null, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask AddUserToDevice_InvalidUserId_ReturnsBadRequest()
    {
        const int invalidUserId = -1;

        var response = await ApiClient.PostAsync($"api/devices/device-1/add/{invalidUserId}", null, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask RemoveUserFromDevice_ValidData_Success()
    {
        var owner = CreatePerson();
        var secondUser = CreatePerson(firstName: "Anna", lastName: "Helferin", email: "anna.helferin@reiter.at");
        var category = CreateCategory();

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.AddRange(owner, secondUser);
            ctx.DeviceCategories.Add(category);
            await ctx.SaveChangesAsync();

            var device = CreateDevice("device-1", owner, category);
            ctx.Devices.Add(device);
            await ctx.SaveChangesAsync();

            ctx.DeviceUsers.AddRange(
                CreateDeviceUser(owner, device),
                CreateDeviceUser(secondUser, device));
            await ctx.SaveChangesAsync();
        });

        var response = await ApiClient.DeleteAsync($"api/devices/device-1/remove/{secondUser.Id}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var getResponse = await ApiClient.GetAsync("api/devices/device-1/users", TestCancellationToken);
        var content = await getResponse.Content.ReadFromJsonAsync<DataTransfer.PersonListResponse>(JsonOptions, TestCancellationToken);

        content.Should().NotBeNull();
        content.Persons.Should().NotContain(p => p.Id == secondUser.Id);
    }

    [Fact]
    public async ValueTask RemoveUserFromDevice_DeviceNotFound_ReturnsNotFound()
    {
        var user = CreatePerson();

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.Add(user);
            await ctx.SaveChangesAsync();
        });

        var response = await ApiClient.DeleteAsync($"api/devices/non-existing-device/remove/{user.Id}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask RemoveUserFromDevice_UserNotFound_ReturnsNotFound()
    {
        var owner = CreatePerson();
        var secondUser = CreatePerson(firstName: "Anna", lastName: "Helferin", email: "anna.helferin@reiter.at");
        var category = CreateCategory();

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.AddRange(owner, secondUser);
            ctx.DeviceCategories.Add(category);
            await ctx.SaveChangesAsync();

            var device = CreateDevice("device-1", owner, category);
            ctx.Devices.Add(device);
            await ctx.SaveChangesAsync();

            ctx.DeviceUsers.AddRange(
                CreateDeviceUser(owner, device),
                CreateDeviceUser(secondUser, device));
            await ctx.SaveChangesAsync();
        });

        const int nonExistingUserId = 999999;

        var response = await ApiClient.DeleteAsync($"api/devices/device-1/remove/{nonExistingUserId}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask RemoveUserFromDevice_OnlyOneUserLeft_ReturnsConflict()
    {
        var owner = CreatePerson();
        var category = CreateCategory();

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.Add(owner);
            ctx.DeviceCategories.Add(category);
            await ctx.SaveChangesAsync();

            var device = CreateDevice("device-1", owner, category);
            ctx.Devices.Add(device);
            await ctx.SaveChangesAsync();

            ctx.DeviceUsers.Add(CreateDeviceUser(owner, device));
            await ctx.SaveChangesAsync();
        });

        var response = await ApiClient.DeleteAsync($"api/devices/device-1/remove/{owner.Id}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask RemoveUserFromDevice_OwnerCannotBeDeleted_ReturnsConflict()
    {
        var owner = CreatePerson();
        var secondUser = CreatePerson(firstName: "Anna", lastName: "Helferin", email: "anna.helferin@reiter.at");
        var category = CreateCategory();

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.AddRange(owner, secondUser);
            ctx.DeviceCategories.Add(category);
            await ctx.SaveChangesAsync();

            var device = CreateDevice("device-1", owner, category);
            ctx.Devices.Add(device);
            await ctx.SaveChangesAsync();

            ctx.DeviceUsers.AddRange(
                CreateDeviceUser(owner, device),
                CreateDeviceUser(secondUser, device));
            await ctx.SaveChangesAsync();
        });

        var response = await ApiClient.DeleteAsync($"api/devices/device-1/remove/{owner.Id}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask RemoveUserFromDevice_InvalidUserId_ReturnsBadRequest()
    {
        const int invalidUserId = -1;

        var response = await ApiClient.DeleteAsync($"api/devices/device-1/remove/{invalidUserId}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}