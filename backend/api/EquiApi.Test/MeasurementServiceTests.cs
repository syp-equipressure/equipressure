using System.Globalization;
using EquiApi.Core.Services;
using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using EquiApi.Test.Util;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OneOf.Types;

namespace EquiApi.Test;

public sealed class MeasurementServiceTests
{
    private const int GroupId = 1;
    private const int MeasurementId = 1;

    private readonly IUnitOfWork _uowMock = Substitute.For<IUnitOfWork>();
    private readonly MeasurementService _sut;

    public MeasurementServiceTests()
    {
        _sut = new MeasurementService(_uowMock, NullLogger<MeasurementService>.Instance);
    }

    [Fact]
    public async Task GetGroupsOfHorseAsync_HorseNotFound_ReturnsNotFound()
    {
        const int HorseId = 1;
        _uowMock.HorseRepository.HorseExistsAsync(HorseId).Returns(false);

        var result = await _sut.GetGroupsOfHorseAsync(HorseId);

        result.Value.Should().BeOfType<NotFound>();
        await _uowMock.MeasurementRepository.DidNotReceive().GetGroupsOfHorseAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task GetGroupsOfHorseAsync_HorseExists_ReturnsGroups()
    {
        const int HorseId = 1;
        var groups = new List<MeasurementGroup>
        {
            TestData.CreateMeasurementGroup(1, HorseId), TestData.CreateMeasurementGroup(2, HorseId)
        };
        _uowMock.HorseRepository.HorseExistsAsync(HorseId).Returns(true);
        _uowMock.MeasurementRepository.GetGroupsOfHorseAsync(HorseId).Returns(groups);

        var result = await _sut.GetGroupsOfHorseAsync(HorseId);

        result.Value.Should().BeOfType<Success<IReadOnlyCollection<MeasurementGroup>>>()
              .Which.Value.Should().BeEquivalentTo(groups);
    }

    [Fact]
    public async Task GetGroupsOfHorseAsync_HorseWithoutGroups_ReturnsEmptyList()
    {
        const int HorseId = 1;
        _uowMock.HorseRepository.HorseExistsAsync(HorseId).Returns(true);
        _uowMock.MeasurementRepository.GetGroupsOfHorseAsync(HorseId).Returns(new List<MeasurementGroup>());

        var result = await _sut.GetGroupsOfHorseAsync(HorseId);

        result.Value.Should().BeOfType<Success<IReadOnlyCollection<MeasurementGroup>>>()
              .Which.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetGroupByIdAsync_GroupNotFound_ReturnsNotFound()
    {
        _uowMock.MeasurementRepository.GetGroupByIdAsync(GroupId).Returns((MeasurementGroup?) null);

        var result = await _sut.GetGroupByIdAsync(GroupId);

        result.Value.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task GetGroupByIdAsync_GroupExists_ReturnsGroup()
    {
        var group = TestData.CreateMeasurementGroup(GroupId);
        _uowMock.MeasurementRepository.GetGroupByIdAsync(GroupId).Returns(group);

        var result = await _sut.GetGroupByIdAsync(GroupId);

        result.Value.Should().BeOfType<Success<MeasurementGroup>>().Which.Value.Should().Be(group);
    }

    [Fact]
    public async Task AddGroupAsync_PersonNotFound_ReturnsNotFound()
    {
        var data = TestData.CreateNewMeasurementGroupData();
        _uowMock.PersonRepository.PersonExistsAsync(data.PersonId).Returns(false);

        var result = await _sut.AddGroupAsync(data);

        result.Value.Should().BeOfType<NotFound>();
        _uowMock.MeasurementRepository.DidNotReceive().AddGroup(Arg.Any<MeasurementGroup>());
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AddGroupAsync_SaddleDoesNotBelongToHorse_ReturnsNotFound()
    {
        var data = TestData.CreateNewMeasurementGroupData();
        _uowMock.PersonRepository.PersonExistsAsync(data.PersonId).Returns(true);
        _uowMock.HorseRepository.SaddleOfHorseExistsAsync(data.HorseId, data.SaddleId).Returns(false);

        var result = await _sut.AddGroupAsync(data);

        result.Value.Should().BeOfType<NotFound>();
        _uowMock.MeasurementRepository.DidNotReceive().AddGroup(Arg.Any<MeasurementGroup>());
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AddGroupAsync_DeviceNotFound_ReturnsNotFound()
    {
        var data = TestData.CreateNewMeasurementGroupData();
        _uowMock.PersonRepository.PersonExistsAsync(data.PersonId).Returns(true);
        _uowMock.HorseRepository.SaddleOfHorseExistsAsync(data.HorseId, data.SaddleId).Returns(true);
        _uowMock.DeviceRepository.DeviceExistsAsync(data.DeviceId).Returns(false);

        var result = await _sut.AddGroupAsync(data);

        result.Value.Should().BeOfType<NotFound>();
        _uowMock.MeasurementRepository.DidNotReceive().AddGroup(Arg.Any<MeasurementGroup>());
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AddGroupAsync_PersonIsNoVisibleRiderOfHorse_ReturnsInvalidMeasurementGroup()
    {
        var data = TestData.CreateNewMeasurementGroupData();
        _uowMock.PersonRepository.PersonExistsAsync(data.PersonId).Returns(true);
        _uowMock.HorseRepository.SaddleOfHorseExistsAsync(data.HorseId, data.SaddleId).Returns(true);
        _uowMock.DeviceRepository.DeviceExistsAsync(data.DeviceId).Returns(true);
        _uowMock.HorseRepository.IsVisibleRiderOfHorseAsync(data.HorseId, data.PersonId).Returns(false);

        var result = await _sut.AddGroupAsync(data);

        result.Value.Should().BeOfType<IMeasurementService.InvalidMeasurementGroup>();
        _uowMock.MeasurementRepository.DidNotReceive().AddGroup(Arg.Any<MeasurementGroup>());
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AddGroupAsync_DeviceCannotMeasureHorse_ReturnsInvalidMeasurementGroup()
    {
        var data = TestData.CreateNewMeasurementGroupData();
        _uowMock.PersonRepository.PersonExistsAsync(data.PersonId).Returns(true);
        _uowMock.HorseRepository.SaddleOfHorseExistsAsync(data.HorseId, data.SaddleId).Returns(true);
        _uowMock.DeviceRepository.DeviceExistsAsync(data.DeviceId).Returns(true);
        _uowMock.HorseRepository.IsVisibleRiderOfHorseAsync(data.HorseId, data.PersonId).Returns(true);
        _uowMock.DeviceRepository.CanMeasureHorseAsync(data.DeviceId, data.HorseId).Returns(false);

        var result = await _sut.AddGroupAsync(data);

        result.Value.Should().BeOfType<IMeasurementService.InvalidMeasurementGroup>();
        _uowMock.MeasurementRepository.DidNotReceive().AddGroup(Arg.Any<MeasurementGroup>());
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AddGroupAsync_ValidData_AddsGroupAndReturnsItWithDetails()
    {
        var data = TestData.CreateNewMeasurementGroupData(2, 3, 4, "EP0001");
        var loadedGroup = TestData.CreateMeasurementGroup();
        _uowMock.PersonRepository.PersonExistsAsync(data.PersonId).Returns(true);
        _uowMock.HorseRepository.SaddleOfHorseExistsAsync(data.HorseId, data.SaddleId).Returns(true);
        _uowMock.DeviceRepository.DeviceExistsAsync(data.DeviceId).Returns(true);
        _uowMock.HorseRepository.IsVisibleRiderOfHorseAsync(data.HorseId, data.PersonId).Returns(true);
        _uowMock.DeviceRepository.CanMeasureHorseAsync(data.DeviceId, data.HorseId).Returns(true);
        _uowMock.MeasurementRepository.GetGroupByIdAsync(Arg.Any<int>()).Returns(loadedGroup);

        var result = await _sut.AddGroupAsync(data);

        result.Value.Should().BeOfType<Success<MeasurementGroup>>().Which.Value.Should().Be(loadedGroup);
        _uowMock.MeasurementRepository.Received(1).AddGroup(Arg.Is<MeasurementGroup>(group =>
            group.Name == "Dressurtraining"
            && group.Date == data.Date
            && group.Notes == "Sehr ausbalanciert"
            && group.PersonId == 2
            && group.HorseId == 3
            && group.SaddleId == 4
            && group.DeviceId == "EP0001"));
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task GetMeasurementsOfGroupAsync_GroupNotFound_ReturnsNotFound()
    {
        _uowMock.MeasurementRepository.GroupExistsAsync(GroupId).Returns(false);

        var result = await _sut.GetMeasurementsOfGroupAsync(GroupId);

        result.Value.Should().BeOfType<NotFound>();
        await _uowMock.MeasurementRepository.DidNotReceive().GetMeasurementsOfGroupAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task GetMeasurementsOfGroupAsync_GroupExists_ReturnsMeasurements()
    {
        var measurements = new List<Measurement>
        {
            TestData.CreateMeasurement(1, GroupId), TestData.CreateMeasurement(2, GroupId, hand: "Rechts")
        };
        _uowMock.MeasurementRepository.GroupExistsAsync(GroupId).Returns(true);
        _uowMock.MeasurementRepository.GetMeasurementsOfGroupAsync(GroupId).Returns(measurements);

        var result = await _sut.GetMeasurementsOfGroupAsync(GroupId);

        result.Value.Should().BeOfType<Success<IReadOnlyCollection<Measurement>>>()
              .Which.Value.Should().BeEquivalentTo(measurements);
    }

    [Fact]
    public async Task GetMeasurementByIdAsync_MeasurementNotFound_ReturnsNotFound()
    {
        _uowMock.MeasurementRepository.GetMeasurementByIdAsync(GroupId, MeasurementId).Returns((Measurement?) null);

        var result = await _sut.GetMeasurementByIdAsync(GroupId, MeasurementId);

        result.Value.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task GetMeasurementByIdAsync_MeasurementExists_ReturnsMeasurement()
    {
        var measurement = TestData.CreateMeasurement(MeasurementId, GroupId);
        _uowMock.MeasurementRepository.GetMeasurementByIdAsync(GroupId, MeasurementId).Returns(measurement);

        var result = await _sut.GetMeasurementByIdAsync(GroupId, MeasurementId);

        result.Value.Should().BeOfType<Success<Measurement>>().Which.Value.Should().Be(measurement);
    }

    [Fact]
    public async Task GetMeasurementByFilterAsync_GroupNotFound_ReturnsNotFound()
    {
        _uowMock.MeasurementRepository.GroupExistsAsync(GroupId).Returns(false);

        var result = await _sut.GetMeasurementByFilterAsync(GroupId, "Schritt", "Links");

        result.Value.Should().BeOfType<NotFound>();
        await _uowMock.MeasurementRepository.DidNotReceive()
                      .GetMeasurementByFilterAsync(Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task GetMeasurementByFilterAsync_NoMatchingMeasurement_ReturnsNotFound()
    {
        _uowMock.MeasurementRepository.GroupExistsAsync(GroupId).Returns(true);
        _uowMock.MeasurementRepository.GetMeasurementByFilterAsync(GroupId, "Galopp", null)
                .Returns((Measurement?) null);

        var result = await _sut.GetMeasurementByFilterAsync(GroupId, "Galopp", null);

        result.Value.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task GetMeasurementByFilterAsync_MatchingMeasurement_ReturnsMeasurement()
    {
        var measurement = TestData.CreateMeasurement(MeasurementId, GroupId, "Trab", "Rechts");
        _uowMock.MeasurementRepository.GroupExistsAsync(GroupId).Returns(true);
        _uowMock.MeasurementRepository.GetMeasurementByFilterAsync(GroupId, "Trab", "Rechts").Returns(measurement);

        var result = await _sut.GetMeasurementByFilterAsync(GroupId, "Trab", "Rechts");

        result.Value.Should().BeOfType<Success<Measurement>>().Which.Value.Should().Be(measurement);
    }

    [Fact]
    public async Task GetGroupAverageAsync_GroupNotFound_ReturnsNotFound()
    {
        _uowMock.MeasurementRepository.GroupExistsAsync(GroupId).Returns(false);

        var result = await _sut.GetGroupAverageAsync(GroupId, null, null);

        result.Value.Should().BeOfType<NotFound>();
        await _uowMock.MeasurementRepository.DidNotReceive()
                      .GetDataOfGroupAsync(Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>());
    }

    [Fact]
    public async Task GetGroupAverageAsync_NoMatchingData_ReturnsNotFound()
    {
        SetupGroupData(null, null);

        var result = await _sut.GetGroupAverageAsync(GroupId, null, null);

        result.Value.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task GetGroupAverageAsync_ValidData_ReturnsAverageOfAllValues()
    {
        SetupGroupData(null, null, "[1,2,3]", "4.5", " [10] ");

        var result = await _sut.GetGroupAverageAsync(GroupId, null, null);

        result.Value.Should().BeOfType<Success<double>>().Which.Value.Should().BeApproximately(4.1, 1e-9);
    }

    [Fact]
    public async Task GetGroupMinAsync_ValidData_ReturnsMinimumOfAllValues()
    {
        SetupGroupData(null, null, "[5,2,3]", "-1.5", "[10]");

        var result = await _sut.GetGroupMinAsync(GroupId, null, null);

        result.Value.Should().BeOfType<Success<double>>().Which.Value.Should().Be(-1.5);
    }

    [Fact]
    public async Task GetGroupMaxAsync_ValidData_ReturnsMaximumOfAllValues()
    {
        SetupGroupData(null, null, "[5,2,3]", "12.25", "[10]");

        var result = await _sut.GetGroupMaxAsync(GroupId, null, null);

        result.Value.Should().BeOfType<Success<double>>().Which.Value.Should().Be(12.25);
    }

    [Fact]
    public async Task GetGroupMaxAsync_WithFilter_OnlyUsesDataOfMatchingMeasurements()
    {
        SetupGroupData("Trab", "Links", "[7,8]");

        var result = await _sut.GetGroupMaxAsync(GroupId, "Trab", "Links");

        result.Value.Should().BeOfType<Success<double>>().Which.Value.Should().Be(8);
        await _uowMock.MeasurementRepository.Received(1).GetDataOfGroupAsync(GroupId, "Trab", "Links");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("1,5")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("[1,\"zwei\"]")]
    [InlineData("[[1,2]]")]
    [InlineData("[1,2")]
    [InlineData("null")]
    public async Task GetGroupAverageAsync_UnparsableData_ReturnsInvalidMeasurementData(string data)
    {
        SetupGroupData(null, null, "[1,2]", data);

        var result = await _sut.GetGroupAverageAsync(GroupId, null, null);

        result.Value.Should().BeOfType<IMeasurementService.InvalidMeasurementData>();
    }

    [Fact]
    public async Task GetGroupAverageAsync_GermanCulture_ParsesDecimalPointIndependentOfCulture()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-AT");
            SetupGroupData(null, null, "1.5", "[2.5]");

            var result = await _sut.GetGroupAverageAsync(GroupId, null, null);

            result.Value.Should().BeOfType<Success<double>>().Which.Value.Should().Be(2);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [Fact]
    public async Task GetMeasurementAverageAsync_MeasurementNotFound_ReturnsNotFound()
    {
        _uowMock.MeasurementRepository.MeasurementExistsAsync(GroupId, MeasurementId).Returns(false);

        var result = await _sut.GetMeasurementAverageAsync(GroupId, MeasurementId);

        result.Value.Should().BeOfType<NotFound>();
        await _uowMock.MeasurementRepository.DidNotReceive().GetDataOfMeasurementAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task GetMeasurementAverageAsync_NoData_ReturnsNotFound()
    {
        SetupMeasurementData();

        var result = await _sut.GetMeasurementAverageAsync(GroupId, MeasurementId);

        result.Value.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task GetMeasurementAverageAsync_ValidData_ReturnsAverage()
    {
        SetupMeasurementData("[0,4,8]", "[12]");

        var result = await _sut.GetMeasurementAverageAsync(GroupId, MeasurementId);

        result.Value.Should().BeOfType<Success<double>>().Which.Value.Should().Be(6);
    }

    [Fact]
    public async Task GetMeasurementMinAsync_ValidData_ReturnsMinimum()
    {
        SetupMeasurementData("[3,1,8]", "2");

        var result = await _sut.GetMeasurementMinAsync(GroupId, MeasurementId);

        result.Value.Should().BeOfType<Success<double>>().Which.Value.Should().Be(1);
    }

    [Fact]
    public async Task GetMeasurementMaxAsync_ValidData_ReturnsMaximum()
    {
        SetupMeasurementData("[3,1,8]", "2");

        var result = await _sut.GetMeasurementMaxAsync(GroupId, MeasurementId);

        result.Value.Should().BeOfType<Success<double>>().Which.Value.Should().Be(8);
    }

    [Fact]
    public async Task GetMeasurementMaxAsync_UnparsableData_ReturnsInvalidMeasurementData()
    {
        SetupMeasurementData("[3,1,8]", "kaputt");

        var result = await _sut.GetMeasurementMaxAsync(GroupId, MeasurementId);

        result.Value.Should().BeOfType<IMeasurementService.InvalidMeasurementData>();
    }

    [Fact]
    public async Task GetDataOfMeasurementAsync_MeasurementNotFound_ReturnsNotFound()
    {
        _uowMock.MeasurementRepository.MeasurementExistsAsync(GroupId, MeasurementId).Returns(false);

        var result = await _sut.GetDataOfMeasurementAsync(GroupId, MeasurementId);

        result.Value.Should().BeOfType<NotFound>();
        await _uowMock.MeasurementRepository.DidNotReceive().GetDataOfMeasurementAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task GetDataOfMeasurementAsync_MeasurementExists_ReturnsData()
    {
        var data = SetupMeasurementData("[1,2]", "[3,4]");

        var result = await _sut.GetDataOfMeasurementAsync(GroupId, MeasurementId);

        result.Value.Should().BeOfType<Success<IReadOnlyCollection<MeasurementData>>>()
              .Which.Value.Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task GetDataByIdAsync_MeasurementNotFound_ReturnsNotFound()
    {
        _uowMock.MeasurementRepository.MeasurementExistsAsync(GroupId, MeasurementId).Returns(false);

        var result = await _sut.GetDataByIdAsync(GroupId, MeasurementId, 1);

        result.Value.Should().BeOfType<NotFound>();
        await _uowMock.MeasurementRepository.DidNotReceive().GetDataByIdAsync(Arg.Any<int>(), Arg.Any<int>());
    }

    [Fact]
    public async Task GetDataByIdAsync_DataNotFound_ReturnsNotFound()
    {
        const int DataId = 5;
        _uowMock.MeasurementRepository.MeasurementExistsAsync(GroupId, MeasurementId).Returns(true);
        _uowMock.MeasurementRepository.GetDataByIdAsync(MeasurementId, DataId).Returns((MeasurementData?) null);

        var result = await _sut.GetDataByIdAsync(GroupId, MeasurementId, DataId);

        result.Value.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task GetDataByIdAsync_DataExists_ReturnsData()
    {
        var data = TestData.CreateMeasurementData(MeasurementId, "[1,2]")[0];
        _uowMock.MeasurementRepository.MeasurementExistsAsync(GroupId, MeasurementId).Returns(true);
        _uowMock.MeasurementRepository.GetDataByIdAsync(MeasurementId, data.Id).Returns(data);

        var result = await _sut.GetDataByIdAsync(GroupId, MeasurementId, data.Id);

        result.Value.Should().BeOfType<Success<MeasurementData>>().Which.Value.Should().Be(data);
    }

    [Fact]
    public async Task AddMeasurementAsync_GroupNotFound_ReturnsNotFound()
    {
        _uowMock.MeasurementRepository.GroupExistsAsync(GroupId).Returns(false);

        var result = await _sut.AddMeasurementAsync(GroupId, "Schritt", "Links", null);

        result.Value.Should().BeOfType<NotFound>();
        _uowMock.MeasurementRepository.DidNotReceive().AddMeasurement(Arg.Any<Measurement>());
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AddMeasurementAsync_PaceAndHandAlreadyExist_ReturnsMeasurementAlreadyExists()
    {
        _uowMock.MeasurementRepository.GroupExistsAsync(GroupId).Returns(true);
        _uowMock.MeasurementRepository.GetMeasurementByFilterAsync(GroupId, "Schritt", "Links")
                .Returns(TestData.CreateMeasurement(MeasurementId, GroupId));

        var result = await _sut.AddMeasurementAsync(GroupId, "Schritt", "Links", null);

        result.Value.Should().BeOfType<IMeasurementService.MeasurementAlreadyExists>();
        _uowMock.MeasurementRepository.DidNotReceive().AddMeasurement(Arg.Any<Measurement>());
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AddMeasurementAsync_ValidData_AddsMeasurement()
    {
        _uowMock.MeasurementRepository.GroupExistsAsync(GroupId).Returns(true);
        _uowMock.MeasurementRepository.GetMeasurementByFilterAsync(GroupId, "Galopp", "Rechts")
                .Returns((Measurement?) null);

        var result = await _sut.AddMeasurementAsync(GroupId, "Galopp", "Rechts", "Druckspitze");

        var measurement = result.Value.Should().BeOfType<Success<Measurement>>().Which.Value;
        measurement.GroupId.Should().Be(GroupId);
        measurement.Pace.Should().Be("Galopp");
        measurement.Hand.Should().Be("Rechts");
        measurement.Description.Should().Be("Druckspitze");
        _uowMock.MeasurementRepository.Received(1).AddMeasurement(measurement);
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task DeleteMeasurementAsync_MeasurementNotFound_ReturnsNotFound()
    {
        _uowMock.MeasurementRepository.GetMeasurementByIdAsync(GroupId, MeasurementId).Returns((Measurement?) null);

        var result = await _sut.DeleteMeasurementAsync(GroupId, MeasurementId);

        result.Value.Should().BeOfType<NotFound>();
        _uowMock.MeasurementRepository.DidNotReceive().RemoveMeasurement(Arg.Any<Measurement>());
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task DeleteMeasurementAsync_MeasurementExists_RemovesMeasurement()
    {
        var measurement = TestData.CreateMeasurement(MeasurementId, GroupId);
        _uowMock.MeasurementRepository.GetMeasurementByIdAsync(GroupId, MeasurementId).Returns(measurement);

        var result = await _sut.DeleteMeasurementAsync(GroupId, MeasurementId);

        result.Value.Should().BeOfType<Success>();
        _uowMock.MeasurementRepository.Received(1).RemoveMeasurement(measurement);
        await _uowMock.Received(1).SaveChangesAsync();
    }

    private void SetupGroupData(string? pace, string? hand, params string[] data)
    {
        _uowMock.MeasurementRepository.GroupExistsAsync(GroupId).Returns(true);
        _uowMock.MeasurementRepository.GetDataOfGroupAsync(GroupId, pace, hand)
                .Returns(TestData.CreateMeasurementData(MeasurementId, data));
    }

    private List<MeasurementData> SetupMeasurementData(params string[] data)
    {
        var measurementData = TestData.CreateMeasurementData(MeasurementId, data);
        _uowMock.MeasurementRepository.MeasurementExistsAsync(GroupId, MeasurementId).Returns(true);
        _uowMock.MeasurementRepository.GetDataOfMeasurementAsync(MeasurementId).Returns(measurementData);

        return measurementData;
    }
}