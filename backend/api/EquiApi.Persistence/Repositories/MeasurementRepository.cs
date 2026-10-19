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
    ///     Removes a measurement, its data entries are removed as well
    /// </summary>
    /// <param name="measurement">The measurement to remove</param>
    public void RemoveMeasurement(Measurement measurement);
}

internal sealed class MeasurementRepository(
    DbSet<MeasurementGroup> groupSet,
    DbSet<Measurement> measurementSet,
    DbSet<MeasurementData> dataSet) : IMeasurementRepository
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

    public void AddGroup(MeasurementGroup group)
    {
        groupSet.Add(group);
    }

    public void AddMeasurement(Measurement measurement)
    {
        measurementSet.Add(measurement);
    }

    public void RemoveMeasurement(Measurement measurement)
    {
        measurementSet.Remove(measurement);
    }
}