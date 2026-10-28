using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace EquiApi.Persistence.Repositories;

public interface IDeviceRepository
{
    /// <summary>
    ///     Checks if a device exists
    /// </summary>
    /// <param name="deviceId">The serial number of the device</param>
    /// <returns>True if the device exists, false otherwise</returns>
    public ValueTask<bool> DeviceExistsAsync(string deviceId);

    /// <summary>
    ///     Gets a device including its owner, category and users
    /// </summary>
    /// <param name="deviceId">The serial number of the device</param>
    /// <returns>The tracked device or null if it does not exist</returns>
    public ValueTask<MeasurementDevice?> GetDeviceByIdAsync(string deviceId);

    /// <summary>
    ///     Gets all devices a person owns or uses, including their owner, category and users
    /// </summary>
    /// <param name="personId">The id of the person</param>
    /// <returns>The devices of the person</returns>
    public ValueTask<IReadOnlyCollection<MeasurementDevice>> GetDevicesOfPersonAsync(int personId);

    /// <summary>
    ///     Gets the owner of a device
    /// </summary>
    /// <param name="deviceId">The serial number of the device</param>
    /// <returns>The owner or null if the device does not exist</returns>
    public ValueTask<Person?> GetOwnerOfDeviceAsync(string deviceId);

    /// <summary>
    ///     Gets the users of a device - the owner is not included
    /// </summary>
    /// <param name="deviceId">The serial number of the device</param>
    /// <returns>The users of the device</returns>
    public ValueTask<IReadOnlyCollection<Person>> GetUsersOfDeviceAsync(string deviceId);

    /// <summary>
    ///     Gets a device category
    /// </summary>
    /// <param name="categoryId">The id of the category</param>
    /// <returns>The tracked category or null if it does not exist</returns>
    public ValueTask<DeviceCategory?> GetCategoryByIdAsync(int categoryId);

    /// <summary>
    ///     Gets all device categories, ordered by the number of allowed users
    /// </summary>
    /// <returns>All device categories</returns>
    public ValueTask<IReadOnlyCollection<DeviceCategory>> GetAllCategoriesAsync();

    /// <summary>
    ///     Checks if measurement groups were recorded with a device
    /// </summary>
    /// <param name="deviceId">The serial number of the device</param>
    /// <returns>True if at least one measurement group uses the device, false otherwise</returns>
    public ValueTask<bool> HasMeasurementGroupsAsync(string deviceId);

    /// <summary>
    ///     Adds a new device
    /// </summary>
    /// <param name="device">The device to add</param>
    public void AddDevice(MeasurementDevice device);

    /// <summary>
    ///     Removes a device, its users are removed as well
    /// </summary>
    /// <param name="device">The device to remove</param>
    public void RemoveDevice(MeasurementDevice device);
}

internal sealed class DeviceRepository(
    DbSet<MeasurementDevice> deviceSet,
    DbSet<DeviceCategory> categorySet,
    DbSet<MeasurementGroup> groupSet) : IDeviceRepository
{
    private IQueryable<MeasurementDevice> DevicesWithDetails =>
        deviceSet.Include(d => d.Owner)
                 .Include(d => d.Category)
                 .Include(d => d.Users)
                 .ThenInclude(du => du.User);

    public async ValueTask<bool> DeviceExistsAsync(string deviceId) =>
        await deviceSet.AnyAsync(d => d.Id == deviceId);

    public async ValueTask<MeasurementDevice?> GetDeviceByIdAsync(string deviceId) =>
        await DevicesWithDetails.FirstOrDefaultAsync(d => d.Id == deviceId);

    public async ValueTask<IReadOnlyCollection<MeasurementDevice>> GetDevicesOfPersonAsync(int personId) =>
        await DevicesWithDetails.AsNoTracking()
                                .Where(d => d.OwnerId == personId || d.Users.Any(du => du.UserId == personId))
                                .OrderBy(d => d.Id)
                                .ToListAsync();

    public async ValueTask<Person?> GetOwnerOfDeviceAsync(string deviceId) =>
        await deviceSet.AsNoTracking()
                       .Where(d => d.Id == deviceId)
                       .Select(d => d.Owner)
                       .FirstOrDefaultAsync();

    public async ValueTask<IReadOnlyCollection<Person>> GetUsersOfDeviceAsync(string deviceId) =>
        await deviceSet.AsNoTracking()
                       .Where(d => d.Id == deviceId)
                       .SelectMany(d => d.Users)
                       .OrderBy(du => du.User.LastName)
                       .Select(du => du.User)
                       .ToListAsync();

    public async ValueTask<DeviceCategory?> GetCategoryByIdAsync(int categoryId) =>
        await categorySet.FirstOrDefaultAsync(c => c.Id == categoryId);

    public async ValueTask<IReadOnlyCollection<DeviceCategory>> GetAllCategoriesAsync() =>
        await categorySet.AsNoTracking()
                         .OrderBy(c => c.NumOfAllowedPeople)
                         .ThenBy(c => c.Name)
                         .ToListAsync();

    public async ValueTask<bool> HasMeasurementGroupsAsync(string deviceId) =>
        await groupSet.AnyAsync(mg => mg.DeviceId == deviceId);

    public void AddDevice(MeasurementDevice device)
    {
        deviceSet.Add(device);
    }

    public void RemoveDevice(MeasurementDevice device)
    {
        deviceSet.Remove(device);
    }
}