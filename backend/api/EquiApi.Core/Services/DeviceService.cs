using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using OneOf;
using OneOf.Types;

namespace EquiApi.Core.Services;

public interface IDeviceService
{
    /// <summary>
    ///     Gets a device including its owner, category and users
    /// </summary>
    /// <param name="deviceId">The serial number of the device</param>
    /// <returns>The device or <see cref="NotFound" /> if it does not exist</returns>
    public ValueTask<OneOf<Success<MeasurementDevice>, NotFound>> GetDeviceByIdAsync(string deviceId);

    /// <summary>
    ///     Gets all devices a person owns or uses
    /// </summary>
    /// <param name="personId">The id of the person</param>
    /// <returns>The devices (may be empty) or <see cref="NotFound" /> if the person does not exist</returns>
    public ValueTask<OneOf<Success<IReadOnlyCollection<MeasurementDevice>>, NotFound>> GetDevicesOfPersonAsync(
        int personId);

    /// <summary>
    ///     Gets the owner of a device
    /// </summary>
    /// <param name="deviceId">The serial number of the device</param>
    /// <returns>The owner or <see cref="NotFound" /> if the device does not exist</returns>
    public ValueTask<OneOf<Success<Person>, NotFound>> GetOwnerOfDeviceAsync(string deviceId);

    /// <summary>
    ///     Gets the users of a device, the owner is not included
    /// </summary>
    /// <param name="deviceId">The serial number of the device</param>
    /// <returns>The users (may be empty) or <see cref="NotFound" /> if the device does not exist</returns>
    public ValueTask<OneOf<Success<IReadOnlyCollection<Person>>, NotFound>> GetUsersOfDeviceAsync(string deviceId);

    /// <summary>
    ///     Adds a new device
    /// </summary>
    /// <param name="deviceId">The serial number of the device</param>
    /// <param name="ownerId">The id of the owner</param>
    /// <param name="categoryId">The id of the device category</param>
    /// <returns>
    ///     The new device, <see cref="NotFound" /> if the owner or the category does not exist
    ///     or <see cref="DeviceAlreadyExists" /> if a device with the serial number already exists
    /// </returns>
    public ValueTask<OneOf<Success<MeasurementDevice>, NotFound, DeviceAlreadyExists>> AddDeviceAsync(
        string deviceId, int ownerId, int categoryId);

    /// <summary>
    ///     Adds a user to a device
    /// </summary>
    /// <param name="deviceId">The serial number of the device</param>
    /// <param name="userId">The id of the new user</param>
    /// <returns>
    ///     <see cref="Success" />, <see cref="NotFound" /> if the device or the person does not exist,
    ///     <see cref="UserAlreadyAssigned" /> if the person already is the owner or a user of the device
    ///     or <see cref="TooManyUsers" /> if the category of the device does not allow another user
    /// </returns>
    public ValueTask<OneOf<Success, NotFound, UserAlreadyAssigned, TooManyUsers>> AddUserToDeviceAsync(
        string deviceId, int userId);

    /// <summary>
    ///     Removes a user from a device
    /// </summary>
    /// <param name="deviceId">The serial number of the device</param>
    /// <param name="userId">The id of the user</param>
    /// <returns>
    ///     <see cref="Success" />, <see cref="NotFound" /> if the device does not exist or the person is not a user of it
    ///     or <see cref="OwnerCannotBeRemoved" /> if the person is the owner of the device
    /// </returns>
    public ValueTask<OneOf<Success, NotFound, OwnerCannotBeRemoved>> RemoveUserFromDeviceAsync(
        string deviceId, int userId);

    /// <summary>
    ///     Makes another person the owner of a device, the previous owner becomes a user of the device
    /// </summary>
    /// <param name="deviceId">The serial number of the device</param>
    /// <param name="newOwnerId">The id of the new owner</param>
    /// <returns>
    ///     <see cref="Success" />, <see cref="NotFound" /> if the device or the person does not exist
    ///     or <see cref="TooManyUsers" /> if the previous owner cannot become a user because of the user limit
    /// </returns>
    public ValueTask<OneOf<Success, NotFound, TooManyUsers>> ChangeOwnerOfDeviceAsync(string deviceId,
                                                                                    int newOwnerId);

    /// <summary>
    ///     Gets all device categories
    /// </summary>
    /// <returns>All device categories ordered by the number of allowed users, which may be empty</returns>
    public ValueTask<IReadOnlyCollection<DeviceCategory>> GetAllCategoriesAsync();

    /// <summary>
    ///     Removes a device including its users. A device that was used for measurements cannot be removed, because
    ///     its measurement groups would be deleted as well.
    /// </summary>
    /// <param name="deviceId">The serial number of the device</param>
    /// <returns>
    ///     <see cref="Success" />, <see cref="NotFound" /> if the device does not exist
    ///     or <see cref="DeviceInUse" /> if measurement groups were recorded with the device
    /// </returns>
    public ValueTask<OneOf<Success, NotFound, DeviceInUse>> DeleteDeviceAsync(string deviceId);

    /// <summary>
    ///     Returned if a device with the same serial number already exists
    /// </summary>
    public readonly record struct DeviceAlreadyExists;

    /// <summary>
    ///     Returned if a device cannot be removed because measurement groups were recorded with it
    /// </summary>
    public readonly record struct DeviceInUse;

    /// <summary>
    ///     Returned if a person already is the owner or a user of a device
    /// </summary>
    public readonly record struct UserAlreadyAssigned;

    /// <summary>
    ///     Returned if the category of a device does not allow another user
    /// </summary>
    public readonly record struct TooManyUsers;

    /// <summary>
    ///     Returned if someone tries to remove the owner from a device
    /// </summary>
    public readonly record struct OwnerCannotBeRemoved;
}

internal sealed class DeviceService(IUnitOfWork uow, ILogger<DeviceService> logger) : IDeviceService
{
    public async ValueTask<OneOf<Success<MeasurementDevice>, NotFound>> GetDeviceByIdAsync(string deviceId)
    {
        var device = await uow.DeviceRepository.GetDeviceByIdAsync(deviceId);
        if (device is null)
        {
            LogDeviceNotFound(deviceId);

            return new NotFound();
        }

        return new Success<MeasurementDevice>(device);
    }

    public async ValueTask<OneOf<Success<IReadOnlyCollection<MeasurementDevice>>, NotFound>> GetDevicesOfPersonAsync(
        int personId)
    {
        if (!await uow.PersonRepository.PersonExistsAsync(personId))
        {
            logger.LogInformation("Person {PersonId} not found", personId);

            return new NotFound();
        }

        var devices = await uow.DeviceRepository.GetDevicesOfPersonAsync(personId);

        return new Success<IReadOnlyCollection<MeasurementDevice>>(devices);
    }

    public async ValueTask<OneOf<Success<Person>, NotFound>> GetOwnerOfDeviceAsync(string deviceId)
    {
        // every device has an owner, so no owner means that the device does not exist
        var owner = await uow.DeviceRepository.GetOwnerOfDeviceAsync(deviceId);
        if (owner is null)
        {
            LogDeviceNotFound(deviceId);

            return new NotFound();
        }

        return new Success<Person>(owner);
    }

    public async ValueTask<OneOf<Success<IReadOnlyCollection<Person>>, NotFound>> GetUsersOfDeviceAsync(
        string deviceId)
    {
        if (!await uow.DeviceRepository.DeviceExistsAsync(deviceId))
        {
            LogDeviceNotFound(deviceId);

            return new NotFound();
        }

        var users = await uow.DeviceRepository.GetUsersOfDeviceAsync(deviceId);

        return new Success<IReadOnlyCollection<Person>>(users);
    }

    public async ValueTask<OneOf<Success<MeasurementDevice>, NotFound, IDeviceService.DeviceAlreadyExists>>
        AddDeviceAsync(string deviceId, int ownerId, int categoryId)
    {
        if (await uow.DeviceRepository.DeviceExistsAsync(deviceId))
        {
            logger.LogWarning("Cannot add device {DeviceId}, it already exists", deviceId);

            return new IDeviceService.DeviceAlreadyExists();
        }

        var owner = await uow.PersonRepository.GetPersonByIdAsync(ownerId);
        if (owner is null)
        {
            logger.LogInformation("Cannot add device {DeviceId}, owner {PersonId} not found", deviceId, ownerId);

            return new NotFound();
        }

        var category = await uow.DeviceRepository.GetCategoryByIdAsync(categoryId);
        if (category is null)
        {
            logger.LogInformation("Cannot add device {DeviceId}, category {CategoryId} not found", deviceId,
                                  categoryId);

            return new NotFound();
        }

        var device = new MeasurementDevice
        {
            Id = deviceId,
            Owner = owner,
            Category = category
        };

        uow.DeviceRepository.AddDevice(device);
        await uow.SaveChangesAsync();
        logger.LogInformation("Device {DeviceId} added for owner {PersonId}", deviceId, ownerId);

        return new Success<MeasurementDevice>(device);
    }

    public async ValueTask<OneOf<Success, NotFound, IDeviceService.UserAlreadyAssigned, IDeviceService.TooManyUsers>>
        AddUserToDeviceAsync(string deviceId, int userId)
    {
        var device = await uow.DeviceRepository.GetDeviceByIdAsync(deviceId);
        if (device is null)
        {
            LogDeviceNotFound(deviceId);

            return new NotFound();
        }

        if (!await uow.PersonRepository.PersonExistsAsync(userId))
        {
            logger.LogInformation("Person {PersonId} not found", userId);

            return new NotFound();
        }

        if (device.OwnerId == userId || device.Users.Any(du => du.UserId == userId))
        {
            logger.LogWarning("Person {PersonId} already is the owner or a user of device {DeviceId}", userId,
                              deviceId);

            return new IDeviceService.UserAlreadyAssigned();
        }

        // TODO: confirm with Flora that the owner does not count towards the limit
        if (device.Users.Count >= device.Category.NumOfAllowedPeople)
        {
            logger.LogWarning("Device {DeviceId} already has the maximum of {AllowedPeople} users", deviceId,
                              device.Category.NumOfAllowedPeople);

            return new IDeviceService.TooManyUsers();
        }

        device.Users.Add(new DeviceUser
        {
            DeviceId = deviceId,
            UserId = userId
        });

        await uow.SaveChangesAsync();
        logger.LogInformation("Person {PersonId} added as user of device {DeviceId}", userId, deviceId);

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound, IDeviceService.OwnerCannotBeRemoved>> RemoveUserFromDeviceAsync(
        string deviceId, int userId)
    {
        var device = await uow.DeviceRepository.GetDeviceByIdAsync(deviceId);
        if (device is null)
        {
            LogDeviceNotFound(deviceId);

            return new NotFound();
        }

        if (device.OwnerId == userId)
        {
            logger.LogWarning("Person {PersonId} is the owner of device {DeviceId} and cannot be removed", userId,
                              deviceId);

            return new IDeviceService.OwnerCannotBeRemoved();
        }

        var deviceUser = device.Users.FirstOrDefault(du => du.UserId == userId);
        if (deviceUser is null)
        {
            logger.LogInformation("Person {PersonId} is not a user of device {DeviceId}", userId, deviceId);

            return new NotFound();
        }

        device.Users.Remove(deviceUser);
        await uow.SaveChangesAsync();
        logger.LogInformation("Person {PersonId} removed from device {DeviceId}", userId, deviceId);

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound, IDeviceService.TooManyUsers>> ChangeOwnerOfDeviceAsync(
        string deviceId, int newOwnerId)
    {
        var device = await uow.DeviceRepository.GetDeviceByIdAsync(deviceId);
        if (device is null)
        {
            LogDeviceNotFound(deviceId);

            return new NotFound();
        }

        var newOwner = await uow.PersonRepository.GetPersonByIdAsync(newOwnerId);
        if (newOwner is null)
        {
            logger.LogInformation("Person {PersonId} not found", newOwnerId);

            return new NotFound();
        }

        if (device.OwnerId == newOwnerId)
        {
            return new Success();
        }

        int previousOwnerId = device.OwnerId;
        bool newOwnerIsUser = device.Users.Any(du => du.UserId == newOwnerId);
        bool previousOwnerIsUser = device.Users.Any(du => du.UserId == previousOwnerId);

        // the previous owner becomes a user, which is only possible if the new owner frees a spot or there is room left
        if (!newOwnerIsUser && !previousOwnerIsUser && device.Users.Count >= device.Category.NumOfAllowedPeople)
        {
            logger.LogWarning("Cannot change owner of device {DeviceId}, the previous owner cannot become a user",
                              deviceId);

            return new IDeviceService.TooManyUsers();
        }

        device.Users.RemoveAll(du => du.UserId == newOwnerId);

        if (!previousOwnerIsUser)
        {
            device.Users.Add(new DeviceUser
            {
                DeviceId = deviceId,
                UserId = previousOwnerId
            });
        }

        device.OwnerId = newOwnerId;
        device.Owner = newOwner;

        await uow.SaveChangesAsync();
        logger.LogInformation("Person {PersonId} is the new owner of device {DeviceId}", newOwnerId, deviceId);

        return new Success();
    }

    public async ValueTask<IReadOnlyCollection<DeviceCategory>> GetAllCategoriesAsync() =>
        await uow.DeviceRepository.GetAllCategoriesAsync();

    public async ValueTask<OneOf<Success, NotFound, IDeviceService.DeviceInUse>> DeleteDeviceAsync(string deviceId)
    {
        var device = await uow.DeviceRepository.GetDeviceByIdAsync(deviceId);
        if (device is null)
        {
            LogDeviceNotFound(deviceId);

            return new NotFound();
        }

        if (await uow.DeviceRepository.HasMeasurementGroupsAsync(deviceId))
        {
            logger.LogWarning("Cannot remove device {DeviceId}, measurement groups were recorded with it", deviceId);

            return new IDeviceService.DeviceInUse();
        }

        uow.DeviceRepository.RemoveDevice(device);
        await uow.SaveChangesAsync();
        logger.LogInformation("Device {DeviceId} removed", deviceId);

        return new Success();
    }

    private void LogDeviceNotFound(string deviceId)
    {
        logger.LogInformation("Device {DeviceId} not found", deviceId);
    }
}