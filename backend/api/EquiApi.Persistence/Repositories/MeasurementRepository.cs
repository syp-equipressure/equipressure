using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace EquiApi.Persistence.Repositories;

public interface IMeasurementRepository
{
    /// <summary>
    /// Returns all measurement groups for a given horse with minimal data.
    /// </summary>
    /// <param name="horseId">The id of the horse.</param>
    /// <returns>A read-only collection of measurement groups.</returns>
    public ValueTask<IReadOnlyCollection<MeasurementGroup>> GetAllGroupsByHorseAsync(int horseId);

    /// <summary>
    /// Returns the full detail view of a single measurement group.
    /// </summary>
    /// <param name="mgId">The id of the measurement group.</param>
    /// <returns>The measurement group if found, otherwise null.</returns>
    public ValueTask<MeasurementGroup?> GetGroupByIdAsync(int mgId);

    /// <summary>
    /// Returns all measurements belonging to a measurement group.
    /// </summary>
    /// <param name="mgId">The id of the measurement group.</param>
    /// <returns>A read-only collection of measurements.</returns>
    public ValueTask<IReadOnlyCollection<Measurement>> GetMeasurementsByGroupAsync(int mgId);

    /// <summary>
    /// Returns a single measurement from a group filtered by pace and hand.
    /// </summary>
    /// <param name="mgId">The id of the measurement group.</param>
    /// <param name="pace">Optional pace filter.</param>
    /// <param name="hand">Optional hand filter.</param>
    /// <returns>The matching measurement if found, otherwise null.</returns>
    public ValueTask<Measurement?> GetMeasurementByFilterAsync(int mgId, string? pace, string? hand);

    /// <summary>
    /// Checks whether a measurement group with the given id exists.
    /// </summary>
    /// <param name="mgId">The id of the measurement group.</param>
    /// <returns>True if exists, false otherwise.</returns>
    public ValueTask<bool> GroupExistsAsync(int mgId);

    /// <summary>
    /// Checks whether a horse with the given id exists.
    /// </summary>
    /// <param name="horseId">The id of the horse.</param>
    /// <returns>True if exists, false otherwise.</returns>
    public ValueTask<bool> HorseExistsAsync(int horseId);

    /// <summary>
    /// Checks whether a measurement with the given id exists inside a group.
    /// </summary>
    /// <param name="mgId">The id of the measurement group.</param>
    /// <param name="mId">The id of the measurement.</param>
    /// <returns>True if exists, false otherwise.</returns>
    public ValueTask<bool> MeasurementExistsAsync(int mgId, int mId);

    /// <summary>
    /// Returns all measurement data entries for a given measurement.
    /// </summary>
    /// <param name="mId">The id of the measurement.</param>
    /// <returns>A read-only collection of measurement data.</returns>
    public ValueTask<IReadOnlyCollection<MeasurementData>> GetAllDataByMeasurementAsync(int mId);

    /// <summary>
    /// Returns a single measurement data entry by id.
    /// </summary>
    /// <param name="mId">The id of the measurement.</param>
    /// <param name="dId">The id of the measurement data entry.</param>
    /// <returns>The measurement data if found, otherwise null.</returns>
    public ValueTask<MeasurementData?> GetDataByIdAsync(int mId, int dId);
    
    /// <summary>
    /// Adds a new measurement to a measurement group.
    /// </summary>
    /// <param name="measurement">The measurement to add.</param>
    public void AddMeasurement(Measurement measurement);

    /// <summary>
    /// Removes a measurement from a measurement group.
    /// </summary>
    /// <param name="measurement">The measurement to remove.</param>
    public void RemoveMeasurement(Measurement measurement);

    /// <summary>
    /// Returns a measurement by id within a group.
    /// </summary>
    /// <param name="mgId">The id of the measurement group.</param>
    /// <param name="mId">The id of the measurement.</param>
    /// <returns>The measurement if found, otherwise null.</returns>
    public ValueTask<Measurement?> GetMeasurementByIdAsync(int mgId, int mId);
}

internal sealed class MeasurementRepository(
    DbSet<MeasurementGroup> groupSet,
    DbSet<Measurement> measurementSet,
    DbSet<MeasurementData> dataSet,
    DbSet<Horse> horseSet) : IMeasurementRepository
{
    public async ValueTask<IReadOnlyCollection<MeasurementGroup>> GetAllGroupsByHorseAsync(int horseId)
    {
        return await groupSet
                     .Include(mg => mg.Person)
                     .Include(mg => mg.Saddle)
                     .Where(mg => mg.HorseId == horseId)
                     .AsNoTracking()
                     .ToListAsync();
    }

    public async ValueTask<MeasurementGroup?> GetGroupByIdAsync(int mgId)
    {
        return await groupSet
                     .Include(mg => mg.Person)
                     .Include(mg => mg.Horse)
                     .ThenInclude(h => h.HorseBreeds)
                     .Include(mg => mg.Saddle)
                     .Where(mg => mg.Id == mgId)
                     .FirstOrDefaultAsync();
    }

    public async ValueTask<IReadOnlyCollection<Measurement>> GetMeasurementsByGroupAsync(int mgId)
    {
        return await measurementSet
                     .Where(m => m.GroupId == mgId)
                     .AsNoTracking()
                     .ToListAsync();
    }

    public async ValueTask<Measurement?> GetMeasurementByFilterAsync(int mgId, string? pace, string? hand)
    {
        return await measurementSet
                     .Where(m => m.GroupId == mgId)
                     .Where(m => pace == null || m.Pace == pace)
                     .Where(m => hand == null || m.Hand == hand)
                     .FirstOrDefaultAsync();
    }

    public async ValueTask<bool> GroupExistsAsync(int mgId)
    {
        return await groupSet.AnyAsync(mg => mg.Id == mgId);
    }

    public async ValueTask<bool> HorseExistsAsync(int horseId)
    {
        return await horseSet.AnyAsync(h => h.Id == horseId);
    }

    public async ValueTask<bool> MeasurementExistsAsync(int mgId, int mId)
    {
        return await measurementSet.AnyAsync(m => m.GroupId == mgId && m.Id == mId);
    }

    public async ValueTask<IReadOnlyCollection<MeasurementData>> GetAllDataByMeasurementAsync(int mId)
    {
        return await dataSet
                     .Where(d => d.MeasurementId == mId)
                     .OrderBy(d => d.Timestamp)
                     .AsNoTracking()
                     .ToListAsync();
    }

    public async ValueTask<MeasurementData?> GetDataByIdAsync(int mId, int dId)
    {
        return await dataSet
                     .Where(d => d.MeasurementId == mId && d.Id == dId)
                     .FirstOrDefaultAsync();
    }

}
