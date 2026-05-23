using System.Net;
using EquiApi.Persistence.Model;
using EquiApi.TestInt.Util;
using EquiApi.Util;

namespace EquiApi.TestInt;

public sealed class DeviceTests(WebApiTestFixture webApiFixture) : WebApiTestBase(webApiFixture)
{
    // Note: make sure you have created a migration before running the tests!
    private const string BaseUrl = "/api/devices";

    private async ValueTask<(Person owner, Person user, DeviceCategory category, MeasurementDevice device)>
        SeedDefaultDataAsync(string deviceId = "AB13CH")
    {
        Address address = new()
        {
            Id = 1,
            PLZ = "4040",
            CityName = "Linz"
        };

        Person owner = new()
        {
            Id = 1, FirstName = "Flora", LastName = "Dellinger",
            Height = 60, Weight = 3,
            DateOfBirth = new LocalDate(2008, 08, 11),
            AddressId = address.Id
        };

        Person user = new()
        {
            Id = 2, FirstName = "Niklaus", LastName = "Michaelson",
            Height = 120, Weight = 67,
            DateOfBirth = new LocalDate(1920, 02, 06),
            AddressId = address.Id
        };

        DeviceCategory category = new()
        {
            Id = 1,
            NumOfAllowedPeople = 5,
            Name = "BasicDevice"
        };

        MeasurementDevice device = new()
        {
            Id = deviceId,
            OwnerId = owner.Id,
            CategoryId = category.Id,
            Users = [new DeviceUser { DeviceId = deviceId, UserId = owner.Id }]
        };

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Addresses.Add(address);  // zuerst, wegen FK
            ctx.Persons.Add(owner);
            ctx.Persons.Add(user);
            ctx.DeviceCategories.Add(category);
            ctx.Devices.Add(device);
            await ctx.SaveChangesAsync(TestCancellationToken);
        });

        return (owner, user, category, device);
    }

    [Fact]
    public async ValueTask GetAllDevicesByUserId_Success()
    {
        var (owner, _, _, _) = await SeedDefaultDataAsync();
 
        var response = await ApiClient.GetAsync($"{BaseUrl}/{owner.Id}", TestCancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
 
        var content = await response.Content
                                    .ReadFromJsonAsync<DataTransfer.DeviceListResponse>(JsonOptions, TestCancellationToken);
 
        content.Should().NotBeNull();
        content.Devices.Should().NotBeEmpty().And.HaveCount(1);
        content.Devices.Should().ContainSingle(d =>
                                                   d.Id == "AB13CH"
                                                   && d.Owner.Id == owner.Id
                                                   && d.CategoryId == 1
                                                   && d.DeviceUser.Count == 1);
    }
    
        [Fact]
    public async ValueTask GetAllDevicesByUserId_NotFound()
    {
        var response = await ApiClient.GetAsync($"{BaseUrl}/9999", TestCancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
 
    [Fact]
    public async ValueTask GetAllDevicesByUserId_InvalidId_BadRequest()
    {
        var response = await ApiClient.GetAsync($"{BaseUrl}/-1", TestCancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

}
