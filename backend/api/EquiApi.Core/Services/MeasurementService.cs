using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using EquiPressure.Core.Service;
using OneOf.Types;

namespace EquiApi.Core.Services;

using GetGroupsByHorseResult =
    OneOf.OneOf<OneOf.Types.Success<IReadOnlyCollection<MeasurementGroup>>, OneOf.Types.None, OneOf.Types.NotFound>;
using GetGroupByIdResult =
    OneOf.OneOf<OneOf.Types.Success<MeasurementGroup>, OneOf.Types.NotFound>;
using GetMeasurementsByGroupResult =
    OneOf.OneOf<OneOf.Types.Success<IReadOnlyCollection<Measurement>>, OneOf.Types.None, OneOf.Types.NotFound>;
using GetMeasurementByFilterResult =
    OneOf.OneOf<OneOf.Types.Success<Measurement>, OneOf.Types.NotFound>;
using GetMeasurementAggregateResult =
    OneOf.OneOf<OneOf.Types.Success<double>, OneOf.Types.NotFound, IBaseService.InvalidData>;
using GetAllDataByMeasurementResult =
    OneOf.OneOf<OneOf.Types.Success<IReadOnlyCollection<MeasurementData>>, OneOf.Types.None, OneOf.Types.NotFound>;
using GetDataByIdResult =
    OneOf.OneOf<OneOf.Types.Success<MeasurementData>, OneOf.Types.NotFound>;

public interface IMeasurementService
{
    /// <summary>
    /// Returns all measurement groups for a given horse with minimal data (date, person, saddle).
    /// </summary>
    /// <param name="horseId">The id of the horse.</param>
    /// <returns>
    /// A <see cref="Success{T}"/> containing the groups,
    /// <see cref="None"/> if no groups exist,
    /// or <see cref="NotFound"/> if the horse does not exist.
    /// </returns>
    public ValueTask<GetGroupsByHorseResult> GetGroupsByHorseAsync(int horseId);

    /// <summary>
    /// Returns the full detail view of a measurement group including horse, rider, and saddle data.
    /// </summary>
    /// <param name="mgId">The id of the measurement group.</param>
    /// <returns>
    /// A <see cref="Success{T}"/> containing the group,
    /// or <see cref="NotFound"/> if the group does not exist.
    /// </returns>
    public ValueTask<GetGroupByIdResult> GetGroupByIdAsync(int mgId);

    /// <summary>
    /// Returns all measurements belonging to a measurement group.
    /// </summary>
    /// <param name="mgId">The id of the measurement group.</param>
    /// <returns>
    /// A <see cref="Success{T}"/> containing the measurements,
    /// <see cref="None"/> if no measurements exist,
    /// or <see cref="NotFound"/> if the group does not exist.
    /// </returns>
    public ValueTask<GetMeasurementsByGroupResult> GetMeasurementsByGroupAsync(int mgId);

    /// <summary>
    /// Returns a single measurement from a group filtered by pace and/or hand.
    /// Since pace+hand must be unique within a group, this returns at most one result.
    /// </summary>
    /// <param name="mgId">The id of the measurement group.</param>
    /// <param name="pace">Optional pace filter.</param>
    /// <param name="hand">Optional hand filter.</param>
    /// <returns>
    /// A <see cref="Success{T}"/> containing the measurement,
    /// or <see cref="NotFound"/> if no matching measurement exists.
    /// </returns>
    public ValueTask<GetMeasurementByFilterResult> GetMeasurementByFilterAsync(int mgId, string? pace, string? hand);

    /// <summary>
    /// Returns the average of all measurement data values in a group, optionally filtered by pace and hand.
    /// </summary>
    /// <param name="mgId">The id of the measurement group.</param>
    /// <param name="pace">Optional pace filter.</param>
    /// <param name="hand">Optional hand filter.</param>
    /// <returns>
    /// A <see cref="Success{Double}"/> containing the average,
    /// <see cref="NotFound"/> if the group or matching measurement does not exist,
    /// or <see cref="IBaseService.InvalidData"/> if the data cannot be parsed as numbers.
    /// </returns>
    public ValueTask<GetMeasurementAggregateResult> GetAverageAsync(int mgId, string? pace, string? hand);

    /// <summary>
    /// Returns the minimum of all measurement data values in a group, optionally filtered by pace and hand.
    /// </summary>
    /// <param name="mgId">The id of the measurement group.</param>
    /// <param name="pace">Optional pace filter.</param>
    /// <param name="hand">Optional hand filter.</param>
    /// <returns>
    /// A <see cref="Success{Double}"/> containing the minimum,
    /// <see cref="NotFound"/> if the group or matching measurement does not exist,
    /// or <see cref="IBaseService.InvalidData"/> if the data cannot be parsed as numbers.
    /// </returns>
    public ValueTask<GetMeasurementAggregateResult> GetMinAsync(int mgId, string? pace, string? hand);

    /// <summary>
    /// Returns the maximum of all measurement data values in a group, optionally filtered by pace and hand.
    /// </summary>
    /// <param name="mgId">The id of the measurement group.</param>
    /// <param name="pace">Optional pace filter.</param>
    /// <param name="hand">Optional hand filter.</param>
    /// <returns>
    /// A <see cref="Success{Double}"/> containing the maximum,
    /// <see cref="NotFound"/> if the group or matching measurement does not exist,
    /// or <see cref="IBaseService.InvalidData"/> if the data cannot be parsed as numbers.
    /// </returns>
    public ValueTask<GetMeasurementAggregateResult> GetMaxAsync(int mgId, string? pace, string? hand);

    /// <summary>
    /// Returns all measurement data entries for a given measurement.
    /// </summary>
    /// <param name="mgId">The id of the measurement group.</param>
    /// <param name="mId">The id of the measurement.</param>
    /// <returns>
    /// A <see cref="Success{T}"/> containing the data entries,
    /// <see cref="None"/> if no data exists,
    /// or <see cref="NotFound"/> if the group or measurement does not exist.
    /// </returns>
    public ValueTask<GetAllDataByMeasurementResult> GetAllDataByMeasurementAsync(int mgId, int mId);

    /// <summary>
    /// Returns a single measurement data entry by id.
    /// </summary>
    /// <param name="mgId">The id of the measurement group.</param>
    /// <param name="mId">The id of the measurement.</param>
    /// <param name="dId">The id of the data entry.</param>
    /// <returns>
    /// A <see cref="Success{T}"/> containing the data entry,
    /// or <see cref="NotFound"/> if the group, measurement, or data entry does not exist.
    /// </returns>
    public ValueTask<GetDataByIdResult> GetDataByIdAsync(int mgId, int mId, int dId);
}

public class MeasurementService(IUnitOfWork uow, ILogger<MeasurementService> logger) : IMeasurementService
{
    public async ValueTask<GetGroupsByHorseResult> GetGroupsByHorseAsync(int horseId)
    {
        if (!await uow.MeasurementRepository.HorseExistsAsync(horseId))
        {
            logger.LogWarning("Horse with id {HorseId} not found", horseId);
            return new NotFound();
        }

        var groups = await uow.MeasurementRepository.GetAllGroupsByHorseAsync(horseId);

        if (groups.Count <= 0)
        {
            logger.LogWarning("No measurement groups found for horse {HorseId}", horseId);
            return new None();
        }

        logger.LogInformation("Successfully retrieved {Count} measurement groups for horse {HorseId}",
                              groups.Count, horseId);
        return new Success<IReadOnlyCollection<MeasurementGroup>>(groups);
    }
    
    public async ValueTask<GetGroupByIdResult> GetGroupByIdAsync(int mgId)
    {
        var group = await uow.MeasurementRepository.GetGroupByIdAsync(mgId);

        if (group is null)
        {
            logger.LogWarning("Measurement group with id {MgId} not found", mgId);
            return new NotFound();
        }

        logger.LogInformation("Successfully retrieved measurement group {MgId}", mgId);
        return new Success<MeasurementGroup>(group);
    }
    
    public async ValueTask<GetMeasurementsByGroupResult> GetMeasurementsByGroupAsync(int mgId)
    {
        if (!await uow.MeasurementRepository.GroupExistsAsync(mgId))
        {
            logger.LogWarning("Measurement group {MgId} not found", mgId);
            return new NotFound();
        }

        var measurements = await uow.MeasurementRepository.GetMeasurementsByGroupAsync(mgId);

        if (measurements.Count <= 0)
        {
            logger.LogWarning("No measurements found in group {MgId}", mgId);
            return new None();
        }

        logger.LogInformation("Successfully retrieved {Count} measurements for group {MgId}",
                              measurements.Count, mgId);
        return new Success<IReadOnlyCollection<Measurement>>(measurements);
    }
    
    public async ValueTask<GetMeasurementByFilterResult> GetMeasurementByFilterAsync(int mgId, string? pace,
                                                                                     string? hand)
    {
        if (!await uow.MeasurementRepository.GroupExistsAsync(mgId))
        {
            logger.LogWarning("Measurement group {MgId} not found", mgId);
            return new NotFound();
        }

        var measurement = await uow.MeasurementRepository.GetMeasurementByFilterAsync(mgId, pace, hand);

        if (measurement is null)
        {
            logger.LogWarning("No measurement found in group {MgId} with pace={Pace} hand={Hand}", mgId, pace, hand);
            return new NotFound();
        }

        logger.LogInformation("Successfully retrieved measurement from group {MgId}", mgId);
        return new Success<Measurement>(measurement);
    }
    
    public async ValueTask<GetMeasurementAggregateResult> GetAverageAsync(int mgId, string? pace, string? hand)
    {
        var measurementResult = await uow.MeasurementRepository.GetMeasurementByFilterAsync(mgId, pace, hand);
        if (measurementResult is null)
        {
            logger.LogWarning("No matching measurement found in group {MgId}", mgId);
            return new NotFound();
        }

        var data = await uow.MeasurementRepository.GetAllDataByMeasurementAsync(measurementResult.Id);

        if (!TryParseAll(data, out var values))
        {
            logger.LogWarning("Could not parse measurement data as numbers for measurement {MId}",
                              measurementResult.Id);
            return new IBaseService.InvalidData();
        }

        return new Success<double>(values.Average());
    }
    
    public async ValueTask<GetMeasurementAggregateResult> GetMinAsync(int mgId, string? pace, string? hand)
    {
        var measurementResult = await uow.MeasurementRepository.GetMeasurementByFilterAsync(mgId, pace, hand);
        if (measurementResult is null)
        {
            logger.LogWarning("No matching measurement found in group {MgId}", mgId);
            return new NotFound();
        }

        var data = await uow.MeasurementRepository.GetAllDataByMeasurementAsync(measurementResult.Id);

        if (!TryParseAll(data, out var values))
        {
            logger.LogWarning("Could not parse measurement data as numbers for measurement {MId}",
                              measurementResult.Id);
            return new IBaseService.InvalidData();
        }

        return new Success<double>(values.Min());
    }
        
    public async ValueTask<GetMeasurementAggregateResult> GetMaxAsync(int mgId, string? pace, string? hand)
    {
        var measurementResult = await uow.MeasurementRepository.GetMeasurementByFilterAsync(mgId, pace, hand);
        if (measurementResult is null)
        {
            logger.LogWarning("No matching measurement found in group {MgId}", mgId);
            return new NotFound();
        }

        var data = await uow.MeasurementRepository.GetAllDataByMeasurementAsync(measurementResult.Id);

        if (!TryParseAll(data, out var values))
        {
            logger.LogWarning("Could not parse measurement data as numbers for measurement {MId}",
                              measurementResult.Id);
            return new IBaseService.InvalidData();
        }

        return new Success<double>(values.Max());
    }
    
    public async ValueTask<GetAllDataByMeasurementResult> GetAllDataByMeasurementAsync(int mgId, int mId)
    {
        if (!await uow.MeasurementRepository.MeasurementExistsAsync(mgId, mId))
        {
            logger.LogWarning("Measurement {MId} in group {MgId} not found", mId, mgId);
            return new NotFound();
        }

        var data = await uow.MeasurementRepository.GetAllDataByMeasurementAsync(mId);

        if (data.Count <= 0)
        {
            logger.LogWarning("No data found for measurement {MId}", mId);
            return new None();
        }

        logger.LogInformation("Successfully retrieved {Count} data entries for measurement {MId}",
                              data.Count, mId);
        return new Success<IReadOnlyCollection<MeasurementData>>(data);
    }

    private static bool TryParseAll(IReadOnlyCollection<MeasurementData> data, out List<double> values)
    {
        values = new List<double>(data.Count);

        foreach (var entry in data)
        {
            if (!double.TryParse(entry.Data, out var parsed))
            {
                return false;
            }

            values.Add(parsed);
        }

        return values.Count > 0;
    }
}
