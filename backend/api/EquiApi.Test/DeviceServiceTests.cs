using EquiApi.Core.Services;
using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using EquiApi.Test.Util;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OneOf.Types;

namespace EquiApi.Test;

public sealed class DeviceServiceTests
{
    private const string DeviceId = "device-1";
    private const string ExternalId = "0e6982b9-0a93-4fdd-8af5-65b85d73b52f";
    private const int CategoryId = 1;

    private readonly IUnitOfWork _uowMock = Substitute.For<IUnitOfWork>();
    private readonly DeviceService _sut;

    public DeviceServiceTests()
    {
        _sut = new DeviceService(_uowMock, NullLogger<DeviceService>.Instance);

        _uowMock.PersonRepository.GetRolesByNameAsync(Arg.Any<IReadOnlyCollection<RoleName>>())
                .Returns(call => call.Arg<IReadOnlyCollection<RoleName>>().Select(TestData.CreateRole).ToList());
    }

    [Fact]
    public async Task ClaimDeviceAsync_DeviceAlreadyExists_ReturnsDeviceAlreadyExists()
    {
        _uowMock.DeviceRepository.DeviceExistsAsync(DeviceId).Returns(true);

        var result = await _sut.ClaimDeviceAsync(DeviceId, ExternalId, CategoryId);

        result.Value.Should().BeOfType<IDeviceService.DeviceAlreadyExists>();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task ClaimDeviceAsync_NoLinkedPerson_ReturnsNotFound()
    {
        _uowMock.DeviceRepository.DeviceExistsAsync(DeviceId).Returns(false);
        _uowMock.PersonRepository.GetPersonByExternalIdAsync(ExternalId).Returns((Person?) null);

        var result = await _sut.ClaimDeviceAsync(DeviceId, ExternalId, CategoryId);

        result.Value.Should().BeOfType<NotFound>();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task ClaimDeviceAsync_CategoryNotFound_ReturnsNotFound()
    {
        _uowMock.DeviceRepository.DeviceExistsAsync(DeviceId).Returns(false);
        _uowMock.PersonRepository.GetPersonByExternalIdAsync(ExternalId).Returns(TestData.CreatePerson());
        _uowMock.DeviceRepository.GetCategoryByIdAsync(CategoryId).Returns((DeviceCategory?) null);

        var result = await _sut.ClaimDeviceAsync(DeviceId, ExternalId, CategoryId);

        result.Value.Should().BeOfType<NotFound>();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task ClaimDeviceAsync_SaddlerDeviceWithoutStreet_ReturnsStreetRequiredForSaddler()
    {
        SetUpClaim(TestData.CreatePerson(), RoleName.Saddler);

        var result = await _sut.ClaimDeviceAsync(DeviceId, ExternalId, CategoryId);

        result.Value.Should().BeOfType<IPersonService.StreetRequiredForSaddler>();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Theory]
    [InlineData(RoleName.Equestrian)]
    [InlineData(RoleName.Saddler)]
    public async Task ClaimDeviceAsync_ValidData_AddsDeviceAndRoleOfCategory(RoleName categoryRole)
    {
        var owner = TestData.CreatePerson(address: TestData.CreateAddress(addressName: "Hauptstraße 1"));
        SetUpClaim(owner, categoryRole);

        var result = await _sut.ClaimDeviceAsync(DeviceId, ExternalId, CategoryId);

        var device = result.Value.Should().BeOfType<Success<MeasurementDevice>>().Which.Value;
        device.Id.Should().Be(DeviceId);
        device.Owner.Should().Be(owner);
        owner.Roles.Should().ContainSingle().Which.Role.Name.Should().Be(categoryRole);
        _uowMock.DeviceRepository.Received(1).AddDevice(device);
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task ClaimDeviceAsync_PersonAlreadyHasRole_DoesNotAddRoleTwice()
    {
        var owner = TestData.CreatePerson(role: RoleName.Equestrian);
        SetUpClaim(owner, RoleName.Equestrian);

        var result = await _sut.ClaimDeviceAsync(DeviceId, ExternalId, CategoryId);

        result.Value.Should().BeOfType<Success<MeasurementDevice>>();
        owner.Roles.Should().ContainSingle();
    }

    private void SetUpClaim(Person owner, RoleName categoryRole)
    {
        _uowMock.DeviceRepository.DeviceExistsAsync(DeviceId).Returns(false);
        _uowMock.PersonRepository.GetPersonByExternalIdAsync(ExternalId).Returns(owner);
        _uowMock.DeviceRepository.GetCategoryByIdAsync(CategoryId)
                .Returns(new DeviceCategory
                {
                    Id = CategoryId, Name = "Standard", NumOfAllowedPeople = 5, Role = categoryRole
                });
    }

    [Fact]
    public async Task GetDeviceByIdAsync_DeviceNotFound_ReturnsNotFound()
    {
        _uowMock.DeviceRepository.GetDeviceByIdAsync(DeviceId).Returns((MeasurementDevice?) null);

        var result = await _sut.GetDeviceByIdAsync(DeviceId);

        result.Value.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task GetDeviceByIdAsync_DeviceExists_ReturnsDevice()
    {
        var device = TestData.CreateDevice(DeviceId);
        _uowMock.DeviceRepository.GetDeviceByIdAsync(DeviceId).Returns(device);

        var result = await _sut.GetDeviceByIdAsync(DeviceId);

        result.Value.Should().BeOfType<Success<MeasurementDevice>>().Which.Value.Should().Be(device);
    }

    [Fact]
    public async Task GetDevicesOfPersonAsync_PersonNotFound_ReturnsNotFound()
    {
        const int PersonId = 1;
        _uowMock.PersonRepository.PersonExistsAsync(PersonId).Returns(false);

        var result = await _sut.GetDevicesOfPersonAsync(PersonId);

        result.Value.Should().BeOfType<NotFound>();
        await _uowMock.DeviceRepository.DidNotReceive().GetDevicesOfPersonAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task GetDevicesOfPersonAsync_PersonExists_ReturnsDevices()
    {
        const int PersonId = 1;
        var devices = new List<MeasurementDevice> { TestData.CreateDevice("device-1"), TestData.CreateDevice("device-2") };
        _uowMock.PersonRepository.PersonExistsAsync(PersonId).Returns(true);
        _uowMock.DeviceRepository.GetDevicesOfPersonAsync(PersonId).Returns(devices);

        var result = await _sut.GetDevicesOfPersonAsync(PersonId);

        result.Value.Should().BeOfType<Success<IReadOnlyCollection<MeasurementDevice>>>()
              .Which.Value.Should().BeEquivalentTo(devices);
    }

    [Fact]
    public async Task GetOwnerOfDeviceAsync_DeviceNotFound_ReturnsNotFound()
    {
        _uowMock.DeviceRepository.GetOwnerOfDeviceAsync(DeviceId).Returns((Person?) null);

        var result = await _sut.GetOwnerOfDeviceAsync(DeviceId);

        result.Value.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task GetOwnerOfDeviceAsync_DeviceExists_ReturnsOwner()
    {
        var owner = TestData.CreatePerson();
        _uowMock.DeviceRepository.GetOwnerOfDeviceAsync(DeviceId).Returns(owner);

        var result = await _sut.GetOwnerOfDeviceAsync(DeviceId);

        result.Value.Should().BeOfType<Success<Person>>().Which.Value.Should().Be(owner);
    }

    [Fact]
    public async Task GetUsersOfDeviceAsync_DeviceNotFound_ReturnsNotFound()
    {
        _uowMock.DeviceRepository.DeviceExistsAsync(DeviceId).Returns(false);

        var result = await _sut.GetUsersOfDeviceAsync(DeviceId);

        result.Value.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task GetUsersOfDeviceAsync_NoUsers_ReturnsEmptyList()
    {
        _uowMock.DeviceRepository.DeviceExistsAsync(DeviceId).Returns(true);
        _uowMock.DeviceRepository.GetUsersOfDeviceAsync(DeviceId).Returns(new List<Person>());

        var result = await _sut.GetUsersOfDeviceAsync(DeviceId);

        result.Value.Should().BeOfType<Success<IReadOnlyCollection<Person>>>().Which.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUsersOfDeviceAsync_UsersExist_ReturnsUsers()
    {
        var users = new List<Person> { TestData.CreatePerson(2), TestData.CreatePerson(3) };
        _uowMock.DeviceRepository.DeviceExistsAsync(DeviceId).Returns(true);
        _uowMock.DeviceRepository.GetUsersOfDeviceAsync(DeviceId).Returns(users);

        var result = await _sut.GetUsersOfDeviceAsync(DeviceId);

        result.Value.Should().BeOfType<Success<IReadOnlyCollection<Person>>>()
              .Which.Value.Should().BeEquivalentTo(users);
    }

    [Fact]
    public async Task AddDeviceAsync_DeviceAlreadyExists_ReturnsDeviceAlreadyExists()
    {
        _uowMock.DeviceRepository.DeviceExistsAsync(DeviceId).Returns(true);

        var result = await _sut.AddDeviceAsync(DeviceId, 1, 1);

        result.Value.Should().BeOfType<IDeviceService.DeviceAlreadyExists>();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AddDeviceAsync_OwnerNotFound_ReturnsNotFound()
    {
        const int OwnerId = 1;
        _uowMock.DeviceRepository.DeviceExistsAsync(DeviceId).Returns(false);
        _uowMock.PersonRepository.GetPersonByIdAsync(OwnerId).Returns((Person?) null);

        var result = await _sut.AddDeviceAsync(DeviceId, OwnerId, 1);

        result.Value.Should().BeOfType<NotFound>();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AddDeviceAsync_CategoryNotFound_ReturnsNotFound()
    {
        const int OwnerId = 1;
        _uowMock.DeviceRepository.DeviceExistsAsync(DeviceId).Returns(false);
        _uowMock.PersonRepository.GetPersonByIdAsync(OwnerId).Returns(TestData.CreatePerson(OwnerId));
        _uowMock.DeviceRepository.GetCategoryByIdAsync(CategoryId).Returns((DeviceCategory?) null);

        var result = await _sut.AddDeviceAsync(DeviceId, OwnerId, CategoryId);

        result.Value.Should().BeOfType<NotFound>();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AddDeviceAsync_ValidData_AddsDeviceAndSaves()
    {
        const int OwnerId = 1;
        var owner = TestData.CreatePerson(OwnerId);
        var category = new DeviceCategory { Id = CategoryId, Name = "Standard", NumOfAllowedPeople = 5 };
        _uowMock.DeviceRepository.DeviceExistsAsync(DeviceId).Returns(false);
        _uowMock.PersonRepository.GetPersonByIdAsync(OwnerId).Returns(owner);
        _uowMock.DeviceRepository.GetCategoryByIdAsync(CategoryId).Returns(category);

        var result = await _sut.AddDeviceAsync(DeviceId, OwnerId, CategoryId);

        var device = result.Value.Should().BeOfType<Success<MeasurementDevice>>().Which.Value;
        device.Id.Should().Be(DeviceId);
        device.Owner.Should().Be(owner);
        device.Category.Should().Be(category);
        device.Users.Should().BeEmpty();
        _uowMock.DeviceRepository.Received(1).AddDevice(device);
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task AddUserToDeviceAsync_DeviceNotFound_ReturnsNotFound()
    {
        _uowMock.DeviceRepository.GetDeviceByIdAsync(DeviceId).Returns((MeasurementDevice?) null);

        var result = await _sut.AddUserToDeviceAsync(DeviceId, 2);

        result.Value.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task AddUserToDeviceAsync_UserNotFound_ReturnsNotFound()
    {
        const int UserId = 2;
        _uowMock.DeviceRepository.GetDeviceByIdAsync(DeviceId).Returns(TestData.CreateDevice(DeviceId));
        _uowMock.PersonRepository.PersonExistsAsync(UserId).Returns(false);

        var result = await _sut.AddUserToDeviceAsync(DeviceId, UserId);

        result.Value.Should().BeOfType<NotFound>();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AddUserToDeviceAsync_UserIsOwner_ReturnsUserAlreadyAssigned()
    {
        const int OwnerId = 1;
        _uowMock.DeviceRepository.GetDeviceByIdAsync(DeviceId).Returns(TestData.CreateDevice(DeviceId, OwnerId));
        _uowMock.PersonRepository.PersonExistsAsync(OwnerId).Returns(true);

        var result = await _sut.AddUserToDeviceAsync(DeviceId, OwnerId);

        result.Value.Should().BeOfType<IDeviceService.UserAlreadyAssigned>();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AddUserToDeviceAsync_UserAlreadyAssigned_ReturnsUserAlreadyAssigned()
    {
        const int UserId = 2;
        var device = TestData.CreateDevice(DeviceId, userIds: [UserId]);
        _uowMock.DeviceRepository.GetDeviceByIdAsync(DeviceId).Returns(device);
        _uowMock.PersonRepository.PersonExistsAsync(UserId).Returns(true);

        var result = await _sut.AddUserToDeviceAsync(DeviceId, UserId);

        result.Value.Should().BeOfType<IDeviceService.UserAlreadyAssigned>();
        device.Users.Should().ContainSingle();
    }

    [Fact]
    public async Task AddUserToDeviceAsync_LimitReached_ReturnsTooManyUsers()
    {
        const int UserId = 4;
        var device = TestData.CreateDevice(DeviceId, numOfAllowedPeople: 2, userIds: [2, 3]);
        _uowMock.DeviceRepository.GetDeviceByIdAsync(DeviceId).Returns(device);
        _uowMock.PersonRepository.PersonExistsAsync(UserId).Returns(true);

        var result = await _sut.AddUserToDeviceAsync(DeviceId, UserId);

        result.Value.Should().BeOfType<IDeviceService.TooManyUsers>();
        device.Users.Should().HaveCount(2);
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AddUserToDeviceAsync_OneBelowLimit_AddsUserAndSaves()
    {
        const int UserId = 3;
        var device = TestData.CreateDevice(DeviceId, numOfAllowedPeople: 2, userIds: [2]);
        _uowMock.DeviceRepository.GetDeviceByIdAsync(DeviceId).Returns(device);
        _uowMock.PersonRepository.PersonExistsAsync(UserId).Returns(true);

        var result = await _sut.AddUserToDeviceAsync(DeviceId, UserId);

        result.Value.Should().BeOfType<Success>();
        device.Users.Should().ContainSingle(du => du.UserId == UserId && du.DeviceId == DeviceId);
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task RemoveUserFromDeviceAsync_DeviceNotFound_ReturnsNotFound()
    {
        _uowMock.DeviceRepository.GetDeviceByIdAsync(DeviceId).Returns((MeasurementDevice?) null);

        var result = await _sut.RemoveUserFromDeviceAsync(DeviceId, 2);

        result.Value.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task RemoveUserFromDeviceAsync_UserIsOwner_ReturnsOwnerCannotBeRemoved()
    {
        const int OwnerId = 1;
        _uowMock.DeviceRepository.GetDeviceByIdAsync(DeviceId)
                .Returns(TestData.CreateDevice(DeviceId, OwnerId, userIds: [2]));

        var result = await _sut.RemoveUserFromDeviceAsync(DeviceId, OwnerId);

        result.Value.Should().BeOfType<IDeviceService.OwnerCannotBeRemoved>();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task RemoveUserFromDeviceAsync_UserNotAssigned_ReturnsNotFound()
    {
        _uowMock.DeviceRepository.GetDeviceByIdAsync(DeviceId).Returns(TestData.CreateDevice(DeviceId, userIds: [2]));

        var result = await _sut.RemoveUserFromDeviceAsync(DeviceId, 99);

        result.Value.Should().BeOfType<NotFound>();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task RemoveUserFromDeviceAsync_UserAssigned_RemovesUserAndSaves()
    {
        const int UserId = 2;
        var device = TestData.CreateDevice(DeviceId, userIds: [UserId, 3]);
        _uowMock.DeviceRepository.GetDeviceByIdAsync(DeviceId).Returns(device);

        var result = await _sut.RemoveUserFromDeviceAsync(DeviceId, UserId);

        result.Value.Should().BeOfType<Success>();
        device.Users.Should().ContainSingle().Which.UserId.Should().Be(3);
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task ChangeOwnerOfDeviceAsync_DeviceNotFound_ReturnsNotFound()
    {
        _uowMock.DeviceRepository.GetDeviceByIdAsync(DeviceId).Returns((MeasurementDevice?) null);

        var result = await _sut.ChangeOwnerOfDeviceAsync(DeviceId, 2);

        result.Value.Should().BeOfType<NotFound>();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task ChangeOwnerOfDeviceAsync_NewOwnerNotFound_ReturnsNotFound()
    {
        const int NewOwnerId = 2;
        _uowMock.DeviceRepository.GetDeviceByIdAsync(DeviceId).Returns(TestData.CreateDevice(DeviceId));
        _uowMock.PersonRepository.GetPersonByIdAsync(NewOwnerId).Returns((Person?) null);

        var result = await _sut.ChangeOwnerOfDeviceAsync(DeviceId, NewOwnerId);

        result.Value.Should().BeOfType<NotFound>();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task ChangeOwnerOfDeviceAsync_PersonIsAlreadyOwner_ReturnsSuccessWithoutSaving()
    {
        const int OwnerId = 1;
        var device = TestData.CreateDevice(DeviceId, OwnerId, userIds: [3]);
        _uowMock.DeviceRepository.GetDeviceByIdAsync(DeviceId).Returns(device);
        _uowMock.PersonRepository.GetPersonByIdAsync(OwnerId).Returns(TestData.CreatePerson(OwnerId));

        var result = await _sut.ChangeOwnerOfDeviceAsync(DeviceId, OwnerId);

        result.Value.Should().BeOfType<Success>();
        device.OwnerId.Should().Be(OwnerId);
        device.Users.Should().ContainSingle(du => du.UserId == 3);
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task ChangeOwnerOfDeviceAsync_NoRoomForPreviousOwner_ReturnsTooManyUsers()
    {
        const int PreviousOwnerId = 1;
        const int NewOwnerId = 2;
        var device = TestData.CreateDevice(DeviceId, PreviousOwnerId, numOfAllowedPeople: 2, userIds: [3, 4]);
        _uowMock.DeviceRepository.GetDeviceByIdAsync(DeviceId).Returns(device);
        _uowMock.PersonRepository.GetPersonByIdAsync(NewOwnerId).Returns(TestData.CreatePerson(NewOwnerId));

        var result = await _sut.ChangeOwnerOfDeviceAsync(DeviceId, NewOwnerId);

        result.Value.Should().BeOfType<IDeviceService.TooManyUsers>();
        device.OwnerId.Should().Be(PreviousOwnerId);
        device.Users.Should().HaveCount(2);
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task ChangeOwnerOfDeviceAsync_NewOwnerWasUserAndLimitReached_SwapsOwnerAndUser()
    {
        const int PreviousOwnerId = 1;
        const int NewOwnerId = 2;
        var device = TestData.CreateDevice(DeviceId, PreviousOwnerId, numOfAllowedPeople: 2, userIds: [NewOwnerId, 3]);
        _uowMock.DeviceRepository.GetDeviceByIdAsync(DeviceId).Returns(device);
        _uowMock.PersonRepository.GetPersonByIdAsync(NewOwnerId).Returns(TestData.CreatePerson(NewOwnerId));

        var result = await _sut.ChangeOwnerOfDeviceAsync(DeviceId, NewOwnerId);

        result.Value.Should().BeOfType<Success>();
        device.OwnerId.Should().Be(NewOwnerId);
        device.Users.Select(du => du.UserId).Should().BeEquivalentTo([PreviousOwnerId, 3]);
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task ChangeOwnerOfDeviceAsync_PreviousOwnerAlreadyUser_DoesNotAddDuplicate()
    {
        const int PreviousOwnerId = 1;
        const int NewOwnerId = 2;
        var device = TestData.CreateDevice(DeviceId, PreviousOwnerId, numOfAllowedPeople: 2,
                                           userIds: [PreviousOwnerId, 3]);
        _uowMock.DeviceRepository.GetDeviceByIdAsync(DeviceId).Returns(device);
        _uowMock.PersonRepository.GetPersonByIdAsync(NewOwnerId).Returns(TestData.CreatePerson(NewOwnerId));

        var result = await _sut.ChangeOwnerOfDeviceAsync(DeviceId, NewOwnerId);

        result.Value.Should().BeOfType<Success>();
        device.OwnerId.Should().Be(NewOwnerId);
        device.Users.Select(du => du.UserId).Should().BeEquivalentTo([PreviousOwnerId, 3]);
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task ChangeOwnerOfDeviceAsync_RoomLeft_ChangesOwnerAndMovesPreviousOwnerToUsers()
    {
        const int PreviousOwnerId = 1;
        const int NewOwnerId = 2;
        var newOwner = TestData.CreatePerson(NewOwnerId);
        var device = TestData.CreateDevice(DeviceId, PreviousOwnerId, numOfAllowedPeople: 2, userIds: [3]);
        _uowMock.DeviceRepository.GetDeviceByIdAsync(DeviceId).Returns(device);
        _uowMock.PersonRepository.GetPersonByIdAsync(NewOwnerId).Returns(newOwner);

        var result = await _sut.ChangeOwnerOfDeviceAsync(DeviceId, NewOwnerId);

        result.Value.Should().BeOfType<Success>();
        device.OwnerId.Should().Be(NewOwnerId);
        device.Owner.Should().Be(newOwner);
        device.Users.Select(du => du.UserId).Should().BeEquivalentTo([PreviousOwnerId, 3]);
        await _uowMock.Received(1).SaveChangesAsync();
    }
}