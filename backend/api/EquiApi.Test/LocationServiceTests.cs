using EquiApi.Core.Services;
using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using EquiApi.Test.Util;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OneOf.Types;

namespace EquiApi.Test;

public sealed class LocationServiceTests
{
    private readonly IUnitOfWork _uowMock = Substitute.For<IUnitOfWork>();
    private readonly LocationService _sut;

    public LocationServiceTests()
    {
        _sut = new LocationService(_uowMock, NullLogger<LocationService>.Instance);
    }

    [Fact]
    public async Task GetCitiesAsync_PassesFilterAndReturnsCities()
    {
        const int Length = 5;
        const string NameFilter = "st";
        var cities = new List<CityData> { new("Steyr", "4400"), new("Stadl-Paura", "4651") };
        _uowMock.LocationRepository.GetCitiesAsync(Length, NameFilter).Returns(cities);

        var result = await _sut.GetCitiesAsync(Length, NameFilter);

        result.Should().Equal(cities);
    }

    [Fact]
    public async Task GetAddressByIdAsync_AddressNotFound_ReturnsNotFound()
    {
        const int AddressId = 1;
        _uowMock.LocationRepository.GetAddressByIdAsync(AddressId).Returns((Address?) null);

        var result = await _sut.GetAddressByIdAsync(AddressId);

        result.Value.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task GetAddressByIdAsync_AddressExists_ReturnsAddress()
    {
        const int AddressId = 1;
        var address = TestData.CreateAddress(AddressId);
        _uowMock.LocationRepository.GetAddressByIdAsync(AddressId).Returns(address);

        var result = await _sut.GetAddressByIdAsync(AddressId);

        result.Value.Should().BeOfType<Success<Address>>().Which.Value.Should().Be(address);
    }

    [Fact]
    public async Task AddAddressAsync_AddressExists_ReturnsAddressAlreadyExists()
    {
        var existingAddress = TestData.CreateAddress(7);
        _uowMock.LocationRepository.FindAddressAsync(null, "4400", "Steyr").Returns(existingAddress);

        var result = await _sut.AddAddressAsync(TestData.CreateAddressData());

        result.Value.Should().BeOfType<ILocationService.AddressAlreadyExists>()
              .Which.ExistingAddressId.Should().Be(7);
        _uowMock.LocationRepository.DidNotReceive().AddAddress(Arg.Any<Address>());
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AddAddressAsync_NewAddress_AddsAddressAndSaves()
    {
        _uowMock.LocationRepository.FindAddressAsync("Hauptplatz 1", "4020", "Linz").Returns((Address?) null);

        var result = await _sut.AddAddressAsync(TestData.CreateAddressData("Hauptplatz 1", "4020", "Linz"));

        var address = result.Value.Should().BeOfType<Success<Address>>().Which.Value;
        address.AddressName.Should().Be("Hauptplatz 1");
        address.PLZ.Should().Be("4020");
        address.CityName.Should().Be("Linz");
        _uowMock.LocationRepository.Received(1).AddAddress(address);
        await _uowMock.Received(1).SaveChangesAsync();
    }
}