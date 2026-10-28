using System.Net;
using System.Text.Json;
using EquiApi.Dtos;
using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using EquiApi.TestInt.Util;
using Microsoft.EntityFrameworkCore;

namespace EquiApi.TestInt;

/// <summary>
///     Makes sure that the development test data fits the database (constraints, required values) and the documented ids
/// </summary>
public sealed class DevelopmentSeedDataTests(WebApiTestFixture webApiFixture) : WebApiTestBase(webApiFixture)
{
    [Fact]
    public async ValueTask SeedAsync_EmptyDatabase_AddsTestDataToEveryTable()
    {
        bool seeded = false;

        await ModifyDatabaseContentAsync(async context => seeded = await DevelopmentSeedData.SeedAsync(context));

        seeded.Should().BeTrue();
        await ModifyDatabaseContentAsync(async context =>
        {
            (await context.Addresses.CountAsync(TestCancellationToken)).Should().Be(10);
            (await context.Persons.CountAsync(TestCancellationToken)).Should().Be(10);
            (await context.PersonRoleAssignments.CountAsync(TestCancellationToken)).Should().Be(11);
            (await context.PersonRelationships.CountAsync(TestCancellationToken)).Should().Be(8);
            (await context.Breeds.CountAsync(TestCancellationToken)).Should().Be(8);
            (await context.Horses.CountAsync(TestCancellationToken)).Should().Be(8);
            (await context.HorseBreeds.CountAsync(TestCancellationToken)).Should().Be(9);
            (await context.PersonHorses.CountAsync(TestCancellationToken)).Should().Be(11);
            (await context.SaddleCategories.CountAsync(TestCancellationToken)).Should().Be(5);
            (await context.Saddles.CountAsync(TestCancellationToken)).Should().Be(11);
            (await context.DeviceCategories.CountAsync(TestCancellationToken)).Should().Be(5);
            (await context.Devices.CountAsync(TestCancellationToken)).Should().Be(6);
            (await context.DeviceUsers.CountAsync(TestCancellationToken)).Should().Be(6);
            (await context.MeasurementGroups.CountAsync(TestCancellationToken)).Should().Be(12);
            (await context.Measurements.CountAsync(TestCancellationToken)).Should().Be(32);
            // a recording every 3 seconds: 3579 seconds in total, plus the first recording of each measurement
            (await context.MeasurementDates.CountAsync(TestCancellationToken)).Should().Be(1225);
            (await context.Releases.CountAsync(TestCancellationToken)).Should().Be(5);
            (await context.MeasurementEligibilities.CountAsync(TestCancellationToken)).Should().Be(7);
            (await context.ChatMessages.CountAsync(TestCancellationToken)).Should().Be(6);
        });
    }

    [Fact]
    public async ValueTask SeedAsync_DatabaseWithData_DoesNotAddTestData()
    {
        await SeedAsync(TestData.CreatePerson());
        bool seeded = true;

        await ModifyDatabaseContentAsync(async context => seeded = await DevelopmentSeedData.SeedAsync(context));

        seeded.Should().BeFalse();
        await ModifyDatabaseContentAsync(async context =>
            (await context.Persons.CountAsync(TestCancellationToken)).Should().Be(1));
    }

    [Fact]
    public async ValueTask SeedAsync_EmptyDatabase_IdsMatchTheDocumentation()
    {
        await ModifyDatabaseContentAsync(async context => await DevelopmentSeedData.SeedAsync(context));

        var sophie = await ReadContentAsync<PersonDetailDto>(await ApiClient.GetAsync("api/persons/1",
                                                                                         TestCancellationToken));
        sophie.LastName.Should().Be("Grüneis");
        sophie.Roles.Should().Equal(RoleName.Saddler);
        sophie.Address.AddressName.Should().Be("Lenaustraße 18");

        var lenasHorses = await ReadContentAsync<HorseListResponse>(await ApiClient.GetAsync("api/persons/6/horses",
                                                                                                TestCancellationToken));
        lenasHorses.Horses.Select(h => h.Name).Should().Equal("Apollo", "Luna");

        var lenasFavourites = await ReadContentAsync<SaddlerListResponse>(
            await ApiClient.GetAsync("api/persons/6/favourites", TestCancellationToken));
        lenasFavourites.Saddlers.Select(s => s.Id).Should().BeEquivalentTo([1, 2]);

        var device = await ApiClient.GetAsync("api/devices/EP0701", TestCancellationToken);
        device.StatusCode.Should().Be(HttpStatusCode.OK);

        var sternchensGroup = await ReadContentAsync<MeasurementGroupDetailDto>(
            await ApiClient.GetAsync("api/measurementgroups/9", TestCancellationToken));
        sternchensGroup.HorseName.Should().Be("Sternchen");
        sternchensGroup.Name.Should().Be("Ausritt Sternchen");
    }

    [Fact]
    public async ValueTask SeedAsync_EmptyDatabase_RecordingsAreFullSensorFrames()
    {
        await ModifyDatabaseContentAsync(async context => await DevelopmentSeedData.SeedAsync(context));

        // measurement 1 of group 1: Schritt Links, 2 minutes
        var data = await ReadContentAsync<MeasurementDataListResponse>(
            await ApiClient.GetAsync("api/measurementgroups/1/measurements/1/measurementData",
                                     TestCancellationToken));
        data.Data.Should().HaveCount(41);
        (data.Data[^1].Timestamp - data.Data[0].Timestamp).Should().Be(Duration.FromMinutes(2));
        data.Data.Should().AllSatisfy(entry =>
            JsonSerializer.Deserialize<int[]>(entry.Data).Should().HaveCount(400));
    }
}