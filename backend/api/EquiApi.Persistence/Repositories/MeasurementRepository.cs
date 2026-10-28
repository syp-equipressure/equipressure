using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace EquiApi.Persistence.Repositories;

public interface IMeasurementRepository
{
    /// <summary>
    ///     Gets all measurement groups of a horse including the person and the saddle
    /// </summary>
    /// <param name="horseId">The id of the horse</param>
    /// <returns>The measurement groups of the horse</returns>
    public ValueTask<IReadOnlyCollection<MeasurementGroup>> GetGroupsOfHorseAsync(int horseId);

    /// <summary>
    ///     Gets a measurement group including the person, the horse with its breeds and the saddle
    /// </summary>
    /// <param name="groupId">The id of the measurement group</param>
    /// <returns>The measurement group or null if it does not exist</returns>
    public ValueTask<MeasurementGroup?> GetGroupByIdAsync(int groupId);

    /// <summary>
    ///     Checks if a measurement group exists
    /// </summary>
    /// <param name="groupId">The id of the measurement group</param>
    /// <returns>True if the measurement group exists, false otherwise</returns>
    public ValueTask<bool> GroupExistsAsync(int groupId);

    /// <summary>
    ///     Gets all measurements of a measurement group
    /// </summary>
    /// <param name="groupId">The id of the measurement group</param>
    /// <returns>The measurements of the group</returns>
    public ValueTask<IReadOnlyCollection<Measurement>> GetMeasurementsOfGroupAsync(int groupId);

    /// <summary>
    ///     Gets the first measurement of a measurement group that matches the given pace and hand
    /// </summary>
    /// <param name="groupId">The id of the measurement group</param>
    /// <param name="pace">The pace to filter by, null to not filter by pace</param>
    /// <param name="hand">The hand to filter by, null to not filter by hand</param>
    /// <returns>The matching measurement or null if there is none</returns>
    public ValueTask<Measurement?> GetMeasurementByFilterAsync(int groupId, string? pace, string? hand);

    /// <summary>
    ///     Gets a measurement of a measurement group
    /// </summary>
    /// <param name="groupId">The id of the measurement group</param>
    /// <param name="measurementId">The id of the measurement</param>
    /// <returns>The measurement or null if it does not exist in the group</returns>
    public ValueTask<Measurement?> GetMeasurementByIdAsync(int groupId, int measurementId);

    /// <summary>
    ///     Checks if a measurement exists in a measurement group
    /// </summary>
    /// <param name="groupId">The id of the measurement group</param>
    /// <param name="measurementId">The id of the measurement</param>
    /// <returns>True if the measurement exists in the group, false otherwise</returns>
    public ValueTask<bool> MeasurementExistsAsync(int groupId, int measurementId);

    /// <summary>
    ///     Gets the most recently added measurement of all measurement groups of a device
    /// </summary>
    /// <param name="deviceId">The id of the measurement device</param>
    /// <returns>The measurement or null if the device has no measurements</returns>
    public ValueTask<Measurement?> GetLatestMeasurementOfDeviceAsync(string deviceId);

    /// <summary>
    ///     Gets all data entries of a measurement, ordered by their timestamp
    /// </summary>
    /// <param name="measurementId">The id of the measurement</param>
    /// <returns>The data entries of the measurement</returns>
    public ValueTask<IReadOnlyCollection<MeasurementData>> GetDataOfMeasurementAsync(int measurementId);

    /// <summary>
    ///     Gets all data entries of the measurements of a measurement group that match the given pace and hand
    /// </summary>
    /// <param name="groupId">The id of the measurement group</param>
    /// <param name="pace">The pace to filter by, null to not filter by pace</param>
    /// <param name="hand">The hand to filter by, null to not filter by hand</param>
    /// <returns>The data entries of the matching measurements</returns>
    public ValueTask<IReadOnlyCollection<MeasurementData>> GetDataOfGroupAsync(int groupId, string? pace,
                                                                                string? hand);

    /// <summary>
    ///     Gets a data entry of a measurement
    /// </summary>
    /// <param name="measurementId">The id of the measurement</param>
    /// <param name="dataId">The id of the data entry</param>
    /// <returns>The data entry or null if it does not exist for the measurement</returns>
    public ValueTask<MeasurementData?> GetDataByIdAsync(int measurementId, int dataId);

    /// <summary>
    ///     Checks if a person may share a measurement group: the persons of the horse, the rider, the owner of the
    ///     measurement device (e.g. the saddler who measured) and the persons the group was released to
    /// </summary>
    /// <param name="personId">The id of the person</param>
    /// <param name="groupId">The id of the measurement group</param>
    /// <returns>True if the person may share the group, false otherwise</returns>
    public ValueTask<bool> CanShareGroupAsync(int personId, int groupId);

    /// <summary>
    ///     Gets the ids of all measurement groups of a horse
    /// </summary>
    /// <param name="horseId">The id of the horse</param>
    /// <returns>The ids of the measurement groups</returns>
    public ValueTask<IReadOnlyCollection<int>> GetGroupIdsOfHorseAsync(int horseId);

    /// <summary>
    ///     Adds a new release of measurement groups
    /// </summary>
    /// <param name="release">The release to add</param>
    public void AddRelease(Release release);

    /// <summary>
    ///     Adds a new measurement group
    /// </summary>
    /// <param name="group">The measurement group to add</param>
    public void AddGroup(MeasurementGroup group);

    /// <summary>
    ///     Adds a new measurement
    /// </summary>
    /// <param name="measurement">The measurement to add</param>
    public void AddMeasurement(Measurement measurement);

    /// <summary>
    ///     Adds a new data entry
    /// </summary>
    /// <param name="data">The data entry to add</param>
    public void AddData(MeasurementData data);

    /// <summary>
    ///     Removes a measurement, its data entries are removed as well
    /// </summary>
    /// <param name="measurement">The measurement to remove</param>
    public void RemoveMeasurement(Measurement measurement);

    /// <summary>
    ///     Removes a measurement group, its measurements, their data entries and its release entries are removed as well
    /// </summary>
    /// <param name="group">The measurement group to remove</param>
    public void RemoveGroup(MeasurementGroup group);
}

internal sealed class MeasurementRepository(
    DbSet<MeasurementGroup> groupSet,
    DbSet<Measurement> measurementSet,
    DbSet<MeasurementData> dataSet,
    DbSet<Release> releaseSet) : IMeasurementRepository
{
    public async ValueTask<IReadOnlyCollection<MeasurementGroup>> GetGroupsOfHorseAsync(int horseId) =>
        await groupSet.AsNoTracking()
                      .Include(mg => mg.Person)
                      .Include(mg => mg.Saddle)
                      .Where(mg => mg.HorseId == horseId)
                      .ToListAsync();

    public async ValueTask<MeasurementGroup?> GetGroupByIdAsync(int groupId) =>
        await groupSet.Include(mg => mg.Person)
                      .Include(mg => mg.Horse)
                      .ThenInclude(h => h.HorseBreeds)
                      .ThenInclude(hb => hb.Breed)
                      .Include(mg => mg.Saddle)
                      .FirstOrDefaultAsync(mg => mg.Id == groupId);

    public async ValueTask<bool> GroupExistsAsync(int groupId) =>
        await groupSet.AnyAsync(mg => mg.Id == groupId);

    public async ValueTask<IReadOnlyCollection<Measurement>> GetMeasurementsOfGroupAsync(int groupId) =>
        await measurementSet.AsNoTracking()
                            .Where(m => m.GroupId == groupId)
                            .ToListAsync();

    public async ValueTask<Measurement?> GetMeasurementByFilterAsync(int groupId, string? pace, string? hand) =>
        await measurementSet.Where(m => m.GroupId == groupId)
                            .Where(m => pace == null || m.Pace == pace)
                            .Where(m => hand == null || m.Hand == hand)
                            .FirstOrDefaultAsync();

    public async ValueTask<Measurement?> GetMeasurementByIdAsync(int groupId, int measurementId) =>
        await measurementSet.FirstOrDefaultAsync(m => m.GroupId == groupId && m.Id == measurementId);

    public async ValueTask<bool> MeasurementExistsAsync(int groupId, int measurementId) =>
        await measurementSet.AnyAsync(m => m.GroupId == groupId && m.Id == measurementId);

    public async ValueTask<Measurement?> GetLatestMeasurementOfDeviceAsync(string deviceId) =>
        await measurementSet.Where(m => m.Group.DeviceId == deviceId)
                            .OrderByDescending(m => m.Id)
                            .FirstOrDefaultAsync();

    public async ValueTask<IReadOnlyCollection<MeasurementData>> GetDataOfMeasurementAsync(int measurementId) =>
        await dataSet.AsNoTracking()
                     .Where(d => d.MeasurementId == measurementId)
                     .OrderBy(d => d.Timestamp)
                     .ToListAsync();

    public async ValueTask<IReadOnlyCollection<MeasurementData>> GetDataOfGroupAsync(int groupId, string? pace,
                                                                                       string? hand) =>
        await dataSet.AsNoTracking()
                     .Where(d => d.Measurement.GroupId == groupId)
                     .Where(d => pace == null || d.Measurement.Pace == pace)
                     .Where(d => hand == null || d.Measurement.Hand == hand)
                     .ToListAsync();

    public async ValueTask<MeasurementData?> GetDataByIdAsync(int measurementId, int dataId) =>
        await dataSet.FirstOrDefaultAsync(d => d.MeasurementId == measurementId && d.Id == dataId);

    public async ValueTask<bool> CanShareGroupAsync(int personId, int groupId) =>
        await groupSet.AnyAsync(mg => mg.Id == groupId
                                      && (mg.PersonId == personId
                                          || mg.Device.OwnerId == personId
                                          || mg.Horse.Persons.Any(ph => ph.PersonId == personId)
                                          || mg.MeasurementEligibilities.Any(me => me.Release.PersonId
                                                                                   == personId)));

    public async ValueTask<IReadOnlyCollection<int>> GetGroupIdsOfHorseAsync(int horseId) =>
        await groupSet.Where(mg => mg.HorseId == horseId)
                      .Select(mg => mg.Id)
                      .ToListAsync();

    public void AddGroup(MeasurementGroup group)
    {
        groupSet.Add(group);
    }

    public void AddMeasurement(Measurement measurement)
    {
        measurementSet.Add(measurement);
    }

    public void AddData(MeasurementData data)
    {
        dataSet.Add(data);
    }

    public void RemoveMeasurement(Measurement measurement)
    {
        measurementSet.Remove(measurement);
    }

    public void AddRelease(Release release)
    {
        releaseSet.Add(release);
    }

    public void RemoveGroup(MeasurementGroup group)
    {
        groupSet.Remove(group);
    }
}