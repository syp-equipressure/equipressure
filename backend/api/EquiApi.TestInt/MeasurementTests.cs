using System.Net;
using EquiApi.Dtos;
using EquiApi.Persistence.Model;
using EquiApi.TestInt.Util;
using Microsoft.EntityFrameworkCore;

namespace EquiApi.TestInt;

public sealed class MeasurementTests(WebApiTestFixture webApiFixture) : WebApiTestBase(webApiFixture)
{
    [Fact]
    public async ValueTask GetGroupsOfHorse_HorseWithGroups_ReturnsGroups()
    {
        var group = CreateGroup(name: "Dressurtraining");
        var otherGroup = TestData.CreateMeasurementGroup(group.Person, group.Horse, group.Saddle, group.Device,
                                                         "Springtraining", new LocalDate(2026, 9, 15));
        var groupOfOtherHorse = CreateGroup(TestData.CreateHorse("Blitz"), "EP0002");
        await SeedAsync(group, otherGroup, groupOfOtherHorse);

        var response = await ApiClient.GetAsync($"api/measurementgroups/horse/{group.Horse.Id}",
                                                TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<MeasurementGroupListResponse>(response);
        content.Groups.Should().BeEquivalentTo(
        [
            new MeasurementGroupSummaryDto(group.Id, group.Date, "Max", "Mustermann", "Dressage Saddle"),
            new MeasurementGroupSummaryDto(otherGroup.Id, otherGroup.Date, "Max", "Mustermann", "Dressage Saddle")
        ]);
    }

    [Fact]
    public async ValueTask GetGroupsOfHorse_HorseWithoutGroups_ReturnsEmptyList()
    {
        var horse = TestData.CreateHorse();
        await SeedAsync(horse);

        var response = await ApiClient.GetAsync($"api/measurementgroups/horse/{horse.Id}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<MeasurementGroupListResponse>(response);
        content.Groups.Should().BeEmpty();
    }

    [Fact]
    public async ValueTask GetGroupsOfHorse_NonExistingHorse_ReturnsNotFound()
    {
        var response = await ApiClient.GetAsync($"api/measurementgroups/horse/{TestData.NonExistingId}",
                                                TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetGroupsOfHorse_InvalidId_ReturnsBadRequest()
    {
        var response = await ApiClient.GetAsync("api/measurementgroups/horse/0", TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask GetGroupById_ExistingGroup_ReturnsDetails()
    {
        var horse = TestData.CreateHorse("Luna", null, TestData.CreateBreed("Haflinger"));
        var group = CreateGroup(horse, notes: "Sehr ausbalanciert");
        await SeedAsync(group);

        var response = await ApiClient.GetAsync($"api/measurementgroups/{group.Id}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<MeasurementGroupDetailDto>(response);
        content.Should().Be(new MeasurementGroupDetailDto(group.Id, group.Date, "Dressurtraining",
                                                          "Sehr ausbalanciert", "Luna", "Haflinger",
                                                          horse.DateOfBirth, horse.Weight, "Max", "Mustermann",
                                                          180M, 75M, "Dressage Saddle"));
    }

    [Fact]
    public async ValueTask GetGroupById_NonExistingGroup_ReturnsNotFound()
    {
        var response = await ApiClient.GetAsync($"api/measurementgroups/{TestData.NonExistingId}",
                                                TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetGroupById_InvalidId_ReturnsBadRequest()
    {
        var response = await ApiClient.GetAsync("api/measurementgroups/-1", TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask CreateGroup_ValidData_ReturnsCreatedGroupWithDetails()
    {
        var (person, horse, saddle, device) = await SeedGroupPartsAsync();
        var request = TestData.CreateAddMeasurementGroupRequest(person.Id, horse.Id, saddle.Id, device.Id);

        var response = await ApiClient.PostAsJsonAsync("api/measurementgroups", request, JsonOptions,
                                                       TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await ReadContentAsync<MeasurementGroupDetailDto>(response);
        content.Should().Be(new MeasurementGroupDetailDto(content.Id, request.Date, "Galopptraining", "Erste Messung",
                                                          "Luna", "Haflinger", horse.DateOfBirth, horse.Weight, "Max",
                                                          "Mustermann", 180M, 75M, "Dressage Saddle"));

        response.Headers.Location.Should().NotBeNull();
        var getResponse = await ApiClient.GetAsync(response.Headers.Location, TestCancellationToken);
        var storedGroup = await ReadContentAsync<MeasurementGroupDetailDto>(getResponse);
        storedGroup.Should().Be(content);
    }

    [Fact]
    public async ValueTask CreateGroup_ValidData_GroupIsListedForHorse()
    {
        var (person, horse, saddle, device) = await SeedGroupPartsAsync();
        var request = TestData.CreateAddMeasurementGroupRequest(person.Id, horse.Id, saddle.Id, device.Id);

        var createResponse = await ApiClient.PostAsJsonAsync("api/measurementgroups", request, JsonOptions,
                                                             TestCancellationToken);
        var createdGroup = await ReadContentAsync<MeasurementGroupDetailDto>(createResponse);

        var response = await ApiClient.GetAsync($"api/measurementgroups/horse/{horse.Id}", TestCancellationToken);
        var content = await ReadContentAsync<MeasurementGroupListResponse>(response);
        content.Groups.Should().ContainSingle().Which.Id.Should().Be(createdGroup.Id);
    }

    [Fact]
    public async ValueTask CreateGroup_PersonNotFound_ReturnsNotFound()
    {
        var (_, horse, saddle, device) = await SeedGroupPartsAsync();
        var request = TestData.CreateAddMeasurementGroupRequest(TestData.NonExistingId, horse.Id, saddle.Id,
                                                                device.Id);

        var response = await ApiClient.PostAsJsonAsync("api/measurementgroups", request, JsonOptions,
                                                       TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask CreateGroup_SaddleOfOtherHorse_ReturnsNotFound()
    {
        var (person, horse, _, device) = await SeedGroupPartsAsync();
        var saddleOfOtherHorse = TestData.CreateSaddle(TestData.CreateHorse("Blitz"));
        await SeedAsync(saddleOfOtherHorse);
        var request = TestData.CreateAddMeasurementGroupRequest(person.Id, horse.Id, saddleOfOtherHorse.Id,
                                                                device.Id);

        var response = await ApiClient.PostAsJsonAsync("api/measurementgroups", request, JsonOptions,
                                                       TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask CreateGroup_NonExistingHorse_ReturnsNotFound()
    {
        var (person, _, saddle, device) = await SeedGroupPartsAsync();
        var request = TestData.CreateAddMeasurementGroupRequest(person.Id, TestData.NonExistingId, saddle.Id,
                                                                device.Id);

        var response = await ApiClient.PostAsJsonAsync("api/measurementgroups", request, JsonOptions,
                                                       TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask CreateGroup_DeviceNotFound_ReturnsNotFound()
    {
        var (person, horse, saddle, _) = await SeedGroupPartsAsync();
        var request = TestData.CreateAddMeasurementGroupRequest(person.Id, horse.Id, saddle.Id, "EP9999");

        var response = await ApiClient.PostAsJsonAsync("api/measurementgroups", request, JsonOptions,
                                                       TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask CreateGroup_DateInFuture_ReturnsBadRequest()
    {
        var request = TestData.CreateAddMeasurementGroupRequest(1, 1, 1, "EP0001",
                                                                date: WebAppFactory.CurrentDate.PlusDays(1));

        var response = await ApiClient.PostAsJsonAsync("api/measurementgroups", request, JsonOptions,
                                                       TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask CreateGroup_EmptyName_ReturnsBadRequest()
    {
        var request = TestData.CreateAddMeasurementGroupRequest(1, 1, 1, "EP0001", "");

        var response = await ApiClient.PostAsJsonAsync("api/measurementgroups", request, JsonOptions,
                                                       TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask GetMeasurementsOfGroup_ExistingGroup_ReturnsMeasurements()
    {
        var group = CreateGroup();
        var walk = TestData.CreateMeasurement(group);
        var trot = TestData.CreateMeasurement(group, "Trab", "Rechts");
        await SeedAsync(walk, trot);

        var response = await ApiClient.GetAsync($"api/measurementgroups/{group.Id}/measurements",
                                                TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<MeasurementListResponse>(response);
        content.Measurements.Should().BeEquivalentTo(
        [
            new MeasurementDto(walk.Id, "Schritt", "Links", null),
            new MeasurementDto(trot.Id, "Trab", "Rechts", null)
        ]);
    }

    [Fact]
    public async ValueTask GetMeasurementsOfGroup_NonExistingGroup_ReturnsNotFound()
    {
        var response = await ApiClient.GetAsync($"api/measurementgroups/{TestData.NonExistingId}/measurements",
                                                TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetMeasurementById_ExistingMeasurement_ReturnsMeasurement()
    {
        var measurement = TestData.CreateMeasurement(CreateGroup(), "Trab", "Rechts");
        await SeedAsync(measurement);

        var response = await ApiClient.GetAsync(
            $"api/measurementgroups/{measurement.GroupId}/measurements/{measurement.Id}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<MeasurementDto>(response);
        content.Should().Be(new MeasurementDto(measurement.Id, "Trab", "Rechts", null));
    }

    [Fact]
    public async ValueTask GetMeasurementById_MeasurementOfOtherGroup_ReturnsNotFound()
    {
        var group = CreateGroup();
        var otherGroup = TestData.CreateMeasurementGroup(group.Person, group.Horse, group.Saddle, group.Device);
        var measurement = TestData.CreateMeasurement(otherGroup);
        await SeedAsync(group, measurement);

        var response = await ApiClient.GetAsync($"api/measurementgroups/{group.Id}/measurements/{measurement.Id}",
                                                TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetMeasurementByFilter_MatchingMeasurement_ReturnsMeasurement()
    {
        var group = CreateGroup();
        var walk = TestData.CreateMeasurement(group);
        var trot = TestData.CreateMeasurement(group, "Trab", "Rechts");
        await SeedAsync(walk, trot);

        var response = await ApiClient.GetAsync(
            $"api/measurementgroups/{group.Id}/measurements/filter?pace=Trab&hand=Rechts", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<MeasurementDto>(response);
        content.Id.Should().Be(trot.Id);
    }

    [Fact]
    public async ValueTask GetMeasurementByFilter_NoMatchingMeasurement_ReturnsNotFound()
    {
        var measurement = TestData.CreateMeasurement(CreateGroup());
        await SeedAsync(measurement);

        var response = await ApiClient.GetAsync(
            $"api/measurementgroups/{measurement.GroupId}/measurements/filter?pace=Galopp", TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("average", 4.1)]
    [InlineData("min", 1)]
    [InlineData("max", 10)]
    public async ValueTask GetGroupAggregate_WithoutFilter_UsesAllValuesOfGroup(string aggregate, double expected)
    {
        var group = CreateGroup();
        await SeedAsync(TestData.CreateMeasurement(group, "Schritt", "Links", "[1,2,3]"),
                        TestData.CreateMeasurement(group, "Trab", "Links", "4.5", "[10]"));

        var response = await ApiClient.GetAsync($"api/measurementgroups/{group.Id}/measurements/{aggregate}",
                                                TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<AggregateResponse>(response);
        content.Value.Should().BeApproximately(expected, 1e-9);
    }

    [Theory]
    [InlineData("average", 15)]
    [InlineData("min", 10)]
    [InlineData("max", 20)]
    public async ValueTask GetGroupAggregate_WithFilter_OnlyUsesValuesOfMatchingMeasurements(string aggregate,
        double expected)
    {
        var group = CreateGroup();
        await SeedAsync(TestData.CreateMeasurement(group, "Schritt", "Links", "[1,2,3]"),
                        TestData.CreateMeasurement(group, "Trab", "Links", "[10,20]"),
                        TestData.CreateMeasurement(group, "Trab", "Rechts", "[100]"));

        var response = await ApiClient.GetAsync(
            $"api/measurementgroups/{group.Id}/measurements/{aggregate}?pace=Trab&hand=Links", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<AggregateResponse>(response);
        content.Value.Should().BeApproximately(expected, 1e-9);
    }

    [Fact]
    public async ValueTask GetGroupAverage_NoMatchingData_ReturnsNotFound()
    {
        var measurement = TestData.CreateMeasurement(CreateGroup(), "Schritt", "Links", "[1,2,3]");
        await SeedAsync(measurement);

        var response = await ApiClient.GetAsync(
            $"api/measurementgroups/{measurement.GroupId}/measurements/average?pace=Galopp", TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetGroupAverage_UnparsableData_ReturnsBadRequest()
    {
        var measurement = TestData.CreateMeasurement(CreateGroup(), "Schritt", "Links", "[1,2,3]", "kaputt");
        await SeedAsync(measurement);

        var response = await ApiClient.GetAsync(
            $"api/measurementgroups/{measurement.GroupId}/measurements/average", TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask GetGroupMax_NonExistingGroup_ReturnsNotFound()
    {
        var response = await ApiClient.GetAsync($"api/measurementgroups/{TestData.NonExistingId}/measurements/max",
                                                TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("average", 6)]
    [InlineData("min", 0)]
    [InlineData("max", 12)]
    public async ValueTask GetMeasurementAggregate_ExistingMeasurement_UsesOnlyValuesOfMeasurement(string aggregate,
        double expected)
    {
        var group = CreateGroup();
        var measurement = TestData.CreateMeasurement(group, "Schritt", "Links", "[0,4,8]", "[12]");
        await SeedAsync(measurement, TestData.CreateMeasurement(group, "Trab", "Links", "[100]"));

        var response = await ApiClient.GetAsync(
            $"api/measurementgroups/{group.Id}/measurements/{measurement.Id}/{aggregate}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<AggregateResponse>(response);
        content.Value.Should().BeApproximately(expected, 1e-9);
    }

    [Fact]
    public async ValueTask GetMeasurementMin_MeasurementWithoutData_ReturnsNotFound()
    {
        var measurement = TestData.CreateMeasurement(CreateGroup());
        await SeedAsync(measurement);

        var response = await ApiClient.GetAsync(
            $"api/measurementgroups/{measurement.GroupId}/measurements/{measurement.Id}/min", TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetMeasurementMax_UnparsableData_ReturnsBadRequest()
    {
        var measurement = TestData.CreateMeasurement(CreateGroup(), "Schritt", "Links", "[1,\"zwei\"]");
        await SeedAsync(measurement);

        var response = await ApiClient.GetAsync(
            $"api/measurementgroups/{measurement.GroupId}/measurements/{measurement.Id}/max", TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask GetMeasurementAverage_NonExistingMeasurement_ReturnsNotFound()
    {
        var group = CreateGroup();
        await SeedAsync(group);

        var response = await ApiClient.GetAsync(
            $"api/measurementgroups/{group.Id}/measurements/{TestData.NonExistingId}/average", TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetDataOfMeasurement_ExistingMeasurement_ReturnsDataOrderedByTimestamp()
    {
        var measurement = TestData.CreateMeasurement(CreateGroup());
        measurement.MeasurementDates =
        [
            new MeasurementData { Data = "[2]", Timestamp = Instant.FromUtc(2026, 9, 2, 14, 5) },
            new MeasurementData { Data = "[1]", Timestamp = Instant.FromUtc(2026, 9, 2, 14, 0) }
        ];
        await SeedAsync(measurement);

        var response = await ApiClient.GetAsync(
            $"api/measurementgroups/{measurement.GroupId}/measurements/{measurement.Id}/measurementData",
            TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<MeasurementDataListResponse>(response);
        content.Data.Select(d => d.Data).Should().Equal("[1]", "[2]");
        content.Data[0].Timestamp.Should().Be(Instant.FromUtc(2026, 9, 2, 14, 0));
    }

    [Fact]
    public async ValueTask GetDataOfMeasurement_MeasurementWithoutData_ReturnsEmptyList()
    {
        var measurement = TestData.CreateMeasurement(CreateGroup());
        await SeedAsync(measurement);

        var response = await ApiClient.GetAsync(
            $"api/measurementgroups/{measurement.GroupId}/measurements/{measurement.Id}/measurementData",
            TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<MeasurementDataListResponse>(response);
        content.Data.Should().BeEmpty();
    }

    [Fact]
    public async ValueTask GetDataOfMeasurement_InvalidMeasurementId_ReturnsBadRequest()
    {
        var response = await ApiClient.GetAsync("api/measurementgroups/1/measurements/0/measurementData",
                                                TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask GetDataById_ExistingData_ReturnsData()
    {
        var measurement = TestData.CreateMeasurement(CreateGroup(), "Schritt", "Links", "[1,2,3]");
        await SeedAsync(measurement);
        var data = measurement.MeasurementDates[0];

        var response = await ApiClient.GetAsync(
            $"api/measurementgroups/{measurement.GroupId}/measurements/{measurement.Id}/measurementData/{data.Id}",
            TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<MeasurementDataDto>(response);
        content.Should().Be(new MeasurementDataDto(data.Id, "[1,2,3]", data.Timestamp));
    }

    [Fact]
    public async ValueTask GetDataById_DataOfOtherMeasurement_ReturnsNotFound()
    {
        var group = CreateGroup();
        var measurement = TestData.CreateMeasurement(group);
        var otherMeasurement = TestData.CreateMeasurement(group, "Trab", "Links", "[1]");
        await SeedAsync(measurement, otherMeasurement);
        var dataId = otherMeasurement.MeasurementDates[0].Id;

        var response = await ApiClient.GetAsync(
            $"api/measurementgroups/{group.Id}/measurements/{measurement.Id}/measurementData/{dataId}",
            TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask CreateMeasurement_ValidData_ReturnsCreatedMeasurement()
    {
        var group = CreateGroup();
        await SeedAsync(group);
        var request = TestData.CreateAddMeasurementRequest(description: "Gleichmäßiger Galopp");

        var response = await ApiClient.PostAsJsonAsync($"api/measurementgroups/{group.Id}/measurements", request,
                                                       JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await ReadContentAsync<MeasurementDto>(response);
        content.Pace.Should().Be("Galopp");
        content.Hand.Should().Be("Links");
        content.Description.Should().Be("Gleichmäßiger Galopp");

        response.Headers.Location.Should().NotBeNull();
        var getResponse = await ApiClient.GetAsync(response.Headers.Location, TestCancellationToken);
        var storedMeasurement = await ReadContentAsync<MeasurementDto>(getResponse);
        storedMeasurement.Should().Be(content);
    }

    [Fact]
    public async ValueTask CreateMeasurement_PaceAndHandAlreadyExist_ReturnsConflict()
    {
        var measurement = TestData.CreateMeasurement(CreateGroup(), "Galopp", "Links");
        await SeedAsync(measurement);

        var response = await ApiClient.PostAsJsonAsync($"api/measurementgroups/{measurement.GroupId}/measurements",
                                                       TestData.CreateAddMeasurementRequest("Galopp", "Links"),
                                                       JsonOptions, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask CreateMeasurement_NonExistingGroup_ReturnsNotFound()
    {
        var response = await ApiClient.PostAsJsonAsync(
            $"api/measurementgroups/{TestData.NonExistingId}/measurements", TestData.CreateAddMeasurementRequest(),
            JsonOptions, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask CreateMeasurement_EmptyPace_ReturnsBadRequest()
    {
        var response = await ApiClient.PostAsJsonAsync("api/measurementgroups/1/measurements",
                                                       TestData.CreateAddMeasurementRequest(""), JsonOptions,
                                                       TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask DeleteMeasurement_ExistingMeasurement_RemovesMeasurementAndItsData()
    {
        var measurement = TestData.CreateMeasurement(CreateGroup(), "Schritt", "Links", "[1,2,3]");
        await SeedAsync(measurement);
        var url = $"api/measurementgroups/{measurement.GroupId}/measurements/{measurement.Id}";

        var response = await ApiClient.DeleteAsync(url, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var getResponse = await ApiClient.GetAsync(url, TestCancellationToken);
        ShouldBeProblem(getResponse, HttpStatusCode.NotFound);
        var dataCount = 0;
        await ModifyDatabaseContentAsync(async context =>
        {
            dataCount = await context.MeasurementDates.CountAsync(d => d.MeasurementId == measurement.Id,
                                                                  TestCancellationToken);
        });
        dataCount.Should().Be(0);
    }

    [Fact]
    public async ValueTask DeleteMeasurement_NonExistingMeasurement_ReturnsNotFound()
    {
        var group = CreateGroup();
        await SeedAsync(group);

        var response = await ApiClient.DeleteAsync(
            $"api/measurementgroups/{group.Id}/measurements/{TestData.NonExistingId}", TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    /// <summary>
    ///     Seeds everything a new measurement group needs: a rider, the horse Luna (Haflinger) with a saddle and a device
    /// </summary>
    private async ValueTask<(Person Person, Horse Horse, Saddle Saddle, MeasurementDevice Device)>
        SeedGroupPartsAsync()
    {
        var person = TestData.CreatePerson();
        var horse = TestData.CreateHorse("Luna", null, TestData.CreateBreed("Haflinger"));
        var saddle = TestData.CreateSaddle(horse);
        var device = TestData.CreateDevice("EP0001", person, TestData.CreateDeviceCategory());
        await SeedAsync(saddle, device);

        return (person, horse, saddle, device);
    }

    /// <summary>
    ///     Creates a measurement group with a new person, saddle and device for the given (or a new) horse
    /// </summary>
    private static MeasurementGroup CreateGroup(Horse? horse = null, string deviceId = "EP0001",
                                                string name = "Dressurtraining", string? notes = null)
    {
        var person = TestData.CreatePerson();
        horse ??= TestData.CreateHorse();
        var device = TestData.CreateDevice(deviceId, person, TestData.CreateDeviceCategory());

        return TestData.CreateMeasurementGroup(person, horse, TestData.CreateSaddle(horse), device, name,
                                               notes: notes);
    }
}