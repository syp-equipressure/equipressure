using EquiApi.Core.Services;
using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using NSubstitute;

namespace EquiApi.Test;

public sealed class DeviceTests
{
    private readonly IUnitOfWork _uowMock = Substitute.For<IUnitOfWork>();
    private readonly ILogger<DeviceService> _loggerMock = Substitute.For<ILogger<DeviceService>>();
    private readonly DeviceService _sut;

    public DeviceTests()
    {
        _sut = new DeviceService(_uowMock, _loggerMock);
    }

    private static Person CreatePerson(int id = 1) => new()
    {
        Id = id,
        FirstName = "Max",
        LastName = "Mustermann",
        Height = 180M,
        Weight = 75M,
        DateOfBirth = new LocalDate(1995, 5, 20),
        Email = "max@reiter.at",
        WebsiteLink = null,
        Description = null,
        AddressId = 1,
        Address = new Address
        {
            Id = 1,
            AddressName = null,
            PLZ = "4400",
            CityName = "Steyr",
            Persons = [],
            Horses = []
        },
        Relationships = [],
        Roles = [],
        Horses = [],
        MeasurementGroups = [],
        UserDevices = [],
        OwnerDevices = [],
        Releases = []
    };

    private static DeviceCategory CreateCategory(int id = 1, int numOfAllowedPeople = 5) => new()
    {
        Id = id,
        NumOfAllowedPeople = numOfAllowedPeople,
        Name = "Standard",
        Devices = []
    };

    private static MeasurementDevice CreateDevice(string id = "device-1", int ownerId = 1,
        int numOfAllowedPeople = 5, List<DeviceUser>? users = null) => new()
    {
        Id = id,
        OwnerId = ownerId,
        Owner = CreatePerson(ownerId),
        CategoryId = 1,
        Category = CreateCategory(1, numOfAllowedPeople),
        MeasurementGroups = [],
        Users = users ?? []
    };

    private static DeviceUser CreateDeviceUser(int userId, string deviceId) => new()
    {
        UserId = userId,
        User = CreatePerson(userId),
        DeviceId = deviceId,
        Device = null!
    };

    [Fact]
    public async Task GetDevicesFromUserIdAsync_UserNotFound_ReturnsNotFound()
    {
        const int userId = 1;
        _uowMock.PersonRepository.GetPersonByIdAsync(userId).Returns((Person?)null);

        var result = await _sut.GetDevicesFromUserIdAsync(userId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task GetDevicesFromUserIdAsync_UserExists_ReturnsDevices()
    {
        const int userId = 1;
        _uowMock.PersonRepository.GetPersonByIdAsync(userId).Returns(CreatePerson(userId));
        _uowMock.DeviceRepository.GetDevicesFromUserIdAsync(userId)
            .Returns(new List<MeasurementDevice> { CreateDevice() });

        var result = await _sut.GetDevicesFromUserIdAsync(userId);

        result.IsT0.Should().BeTrue();
    }

    [Fact]
    public async Task GetOwnerOfDevice_DeviceNotFound_ReturnsNotFound()
    {
        const string deviceId = "device-1";
        _uowMock.DeviceRepository.DeviceExistsAsync(deviceId).Returns(false);

        var result = await _sut.GetOwnerOfDevice(deviceId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task GetOwnerOfDevice_NoOwnerRegistered_ReturnsNoOwnerFound()
    {
        const string deviceId = "device-1";
        _uowMock.DeviceRepository.DeviceExistsAsync(deviceId).Returns(true);
        _uowMock.DeviceRepository.GetOwnerOfDeviceAsync(deviceId).Returns((Person?)null);

        var result = await _sut.GetOwnerOfDevice(deviceId);

        result.IsT2.Should().BeTrue();
    }

    [Fact]
    public async Task GetOwnerOfDevice_OwnerExists_ReturnsSuccess()
    {
        const string deviceId = "device-1";
        _uowMock.DeviceRepository.DeviceExistsAsync(deviceId).Returns(true);
        _uowMock.DeviceRepository.GetOwnerOfDeviceAsync(deviceId).Returns(CreatePerson());

        var result = await _sut.GetOwnerOfDevice(deviceId);

        result.IsT0.Should().BeTrue();
    }

    [Fact]
    public async Task GetUsersOfDevice_DeviceNotFound_ReturnsNotFound()
    {
        const string deviceId = "device-1";
        _uowMock.DeviceRepository.DeviceExistsAsync(deviceId).Returns(false);

        var result = await _sut.GetUsersOfDevice(deviceId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task GetUsersOfDevice_NoUsersRegistered_ReturnsNoUsersFound()
    {
        const string deviceId = "device-1";
        _uowMock.DeviceRepository.DeviceExistsAsync(deviceId).Returns(true);
        _uowMock.DeviceRepository.GetPersonsOfDeviceAsync(deviceId)
            .Returns(new List<Person>());

        var result = await _sut.GetUsersOfDevice(deviceId);

        result.IsT2.Should().BeTrue();
    }

    [Fact]
    public async Task GetUsersOfDevice_UsersExist_ReturnsSuccess()
    {
        const string deviceId = "device-1";
        _uowMock.DeviceRepository.DeviceExistsAsync(deviceId).Returns(true);
        _uowMock.DeviceRepository.GetPersonsOfDeviceAsync(deviceId)
            .Returns(new List<Person> { CreatePerson() });

        var result = await _sut.GetUsersOfDevice(deviceId);

        result.IsT0.Should().BeTrue();
    }

    [Fact]
    public async Task AddDeviceAsync_OwnerNotFound_ReturnsNotFound()
    {
        const string deviceId = "device-1";
        const int ownerId = 1;
        const int categoryId = 1;

        _uowMock.PersonRepository.PersonExistsAsync(ownerId).Returns(false);

        var result = await _sut.AddDeviceAsync(deviceId, ownerId, categoryId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task AddDeviceAsync_CategoryNotFound_ReturnsNotFound()
    {
        const string deviceId = "device-1";
        const int ownerId = 1;
        const int categoryId = 1;

        _uowMock.PersonRepository.PersonExistsAsync(ownerId).Returns(true);
        _uowMock.DeviceRepository.CategoryExistsAsync(categoryId).Returns(false);

        var result = await _sut.AddDeviceAsync(deviceId, ownerId, categoryId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task AddDeviceAsync_ValidData_SavesToDatabaseAndReturnsSuccess()
    {
        const string deviceId = "device-1";
        const int ownerId = 1;
        const int categoryId = 1;

        _uowMock.PersonRepository.PersonExistsAsync(ownerId).Returns(true);
        _uowMock.DeviceRepository.CategoryExistsAsync(categoryId).Returns(true);

        var result = await _sut.AddDeviceAsync(deviceId, ownerId, categoryId);

        result.IsT0.Should().BeTrue();

        _uowMock.DeviceRepository.Received(1).AddDevice(Arg.Any<MeasurementDevice>());
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task AddUserToDevice_DeviceNotFound_ReturnsNotFound()
    {
        const int userId = 1;
        const string deviceId = "device-1";

        _uowMock.DeviceRepository.GetDeviceByIdAsync(deviceId).Returns((MeasurementDevice?)null);

        var result = await _sut.AddUserToDevice(userId, deviceId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task AddUserToDevice_UserNotFound_ReturnsNotFound()
    {
        const int userId = 1;
        const string deviceId = "device-1";

        var device = CreateDevice(deviceId, numOfAllowedPeople: 5);

        _uowMock.DeviceRepository.GetDeviceByIdAsync(deviceId).Returns(device);
        _uowMock.PersonRepository.PersonExistsAsync(userId).Returns(false);

        var result = await _sut.AddUserToDevice(userId, deviceId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task AddUserToDevice_TooManyUsers_ReturnsTooManyUsers()
    {
        const int userId = 1;
        const string deviceId = "device-1";

        var device = CreateDevice(deviceId, numOfAllowedPeople: 1, users:
        [
            CreateDeviceUser(2, deviceId),
            CreateDeviceUser(3, deviceId)
        ]);

        _uowMock.DeviceRepository.GetDeviceByIdAsync(deviceId).Returns(device);
        _uowMock.PersonRepository.PersonExistsAsync(userId).Returns(true);

        var result = await _sut.AddUserToDevice(userId, deviceId);

        result.IsT2.Should().BeTrue();
    }

    [Fact]
    public async Task AddUserToDevice_ValidData_SavesToDatabaseAndReturnsSuccess()
    {
        const int userId = 1;
        const string deviceId = "device-1";

        var device = CreateDevice(deviceId, numOfAllowedPeople: 5);

        _uowMock.DeviceRepository.GetDeviceByIdAsync(deviceId).Returns(device);
        _uowMock.PersonRepository.PersonExistsAsync(userId).Returns(true);

        var result = await _sut.AddUserToDevice(userId, deviceId);

        result.IsT0.Should().BeTrue();
        device.Users.Should().ContainSingle(u => u.UserId == userId && u.DeviceId == deviceId);
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task RemoveUserFromDevice_DeviceNotFound_ReturnsNotFound()
    {
        const int userId = 1;
        const string deviceId = "device-1";

        _uowMock.DeviceRepository.GetDeviceByIdAsync(deviceId).Returns((MeasurementDevice?)null);

        var result = await _sut.RemoveUserFromDevice(userId, deviceId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveUserFromDevice_UserNotFound_ReturnsNotFound()
    {
        const int userId = 1;
        const string deviceId = "device-1";

        var device = CreateDevice(deviceId, ownerId: 2, users:
        [
            CreateDeviceUser(userId, deviceId),
            CreateDeviceUser(99, deviceId)
        ]);

        _uowMock.DeviceRepository.GetDeviceByIdAsync(deviceId).Returns(device);
        _uowMock.PersonRepository.PersonExistsAsync(userId).Returns(false);

        var result = await _sut.RemoveUserFromDevice(userId, deviceId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveUserFromDevice_OnlyOneUserLeft_ReturnsTooLittleUsers()
    {
        const int userId = 1;
        const string deviceId = "device-1";

        var device = CreateDevice(deviceId, ownerId: 2, users:
        [
            CreateDeviceUser(userId, deviceId)
        ]);

        _uowMock.DeviceRepository.GetDeviceByIdAsync(deviceId).Returns(device);
        _uowMock.PersonRepository.PersonExistsAsync(userId).Returns(true);

        var result = await _sut.RemoveUserFromDevice(userId, deviceId);

        result.IsT2.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveUserFromDevice_UserIsOwner_ReturnsOwnerCantBeDeleted()
    {
        const int userId = 1;
        const string deviceId = "device-1";

        var device = CreateDevice(deviceId, ownerId: userId, users:
        [
            CreateDeviceUser(userId, deviceId),
            CreateDeviceUser(99, deviceId)
        ]);

        _uowMock.DeviceRepository.GetDeviceByIdAsync(deviceId).Returns(device);
        _uowMock.PersonRepository.PersonExistsAsync(userId).Returns(true);

        var result = await _sut.RemoveUserFromDevice(userId, deviceId);

        result.IsT3.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveUserFromDevice_DeviceUserEntryNotFound_ReturnsNotFound()
    {
        const int userId = 1;
        const string deviceId = "device-1";

        var device = CreateDevice(deviceId, ownerId: 2, users:
        [
            CreateDeviceUser(userId, deviceId),
            CreateDeviceUser(99, deviceId)
        ]);

        _uowMock.DeviceRepository.GetDeviceByIdAsync(deviceId).Returns(device);
        _uowMock.PersonRepository.PersonExistsAsync(userId).Returns(true);
        _uowMock.DeviceRepository.GetDeviceUserEntryAsync(deviceId, userId).Returns((DeviceUser?)null);

        var result = await _sut.RemoveUserFromDevice(userId, deviceId);

        result.IsT1.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveUserFromDevice_ValidData_SavesToDatabaseAndReturnsSuccess()
    {
        const int userId = 1;
        const string deviceId = "device-1";

        var dU = CreateDeviceUser(userId, deviceId);
        var device = CreateDevice(deviceId, ownerId: 2, users:
        [
            dU,
            CreateDeviceUser(99, deviceId)
        ]);

        _uowMock.DeviceRepository.GetDeviceByIdAsync(deviceId).Returns(device);
        _uowMock.PersonRepository.PersonExistsAsync(userId).Returns(true);
        _uowMock.DeviceRepository.GetDeviceUserEntryAsync(deviceId, userId).Returns(dU);

        var result = await _sut.RemoveUserFromDevice(userId, deviceId);

        result.IsT0.Should().BeTrue();
        device.Users.Should().NotContain(dU);
        await _uowMock.Received(1).SaveChangesAsync();
    }
}