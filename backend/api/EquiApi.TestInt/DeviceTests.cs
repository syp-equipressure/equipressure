using System.Net;
using EquiApi.Persistence.Model;
using EquiApi.TestInt.Util;
using EquiApi.Util;

namespace EquiApi.TestInt;

public sealed class DeviceTests(WebApiTestFixture webApiFixture) : WebApiTestBase(webApiFixture)
{
    // Note: make sure you have created a migration before running the tests!
    private const string BaseUrl = "/api/devices";

    [Fact]
    public async ValueTask GetAllDevicesByUserId_Success()
    {
        Person owner = new Person()
        {
            Id = 1, FirstName = "Flora", LastName = "Dellinger", Height = 60,
            Weight = 3,
            DateOfBirth = new LocalDate(2008, 08, 11)
        };

        Person user = new Person()
        {
            Id = 2, FirstName = "Niklaus", LastName = "Michaelson", Height = 120,
            Weight = 67,
            DateOfBirth = new LocalDate(1920, 02, 06)
        };

        DeviceCategory category = new DeviceCategory()
        {
            Id = 1,
            NumOfAllowedPeople = 5,
            Name = "BasicDevice"
        };

        MeasurementDevice device = new MeasurementDevice()
        {
            Id = "AB13CH",
            OwnerId = owner.Id,
            CategoryId = 1,
            Users = new List<DeviceUser>([new DeviceUser() { DeviceId = "AB13CH", UserId = 1 }]),
        };

        await ModifyDatabaseContentAsync(async ctx =>
        {
            ctx.Persons.Add(owner);
            ctx.Persons.Add(user);
            ctx.DeviceCategories.Add(category);
            ctx.Devices.Add(device);
            await ctx.SaveChangesAsync();
        });

        var response = await ApiClient.GetAsync(BaseUrl + "/" + user.Id, TestCancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<DataTransfer.DeviceListResponse>(JsonOptions, TestCancellationToken);
        
        content.Should().NotBeNull();
        content.Devices.Should().NotBeEmpty().And.HaveCount(1);
        content.Devices.Should().ContainSingle(d => d.Id == "AB13CH"
                                                    && d.Owner == owner
                                                    && d.CategoryId == 1
                                                    && d.DeviceUser.Count == 1);
    }
}
