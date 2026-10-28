using System.Globalization;
using System.Text.Json;
using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using OneOf;
using OneOf.Types;

namespace EquiApi.Core.Services;

/// <summary>
///     The values of a new measurement group
/// </summary>
public sealed record NewMeasurementGroupData(
    string Name,
    LocalDate Date,
    string? Notes,
    int PersonId,
    int HorseId,
    int SaddleId,
    string DeviceId);

public interface IMeasurementService
{
    /// <summary>
    ///     Gets all measurement groups of a horse including the person and the saddle
    /// </summary>
    /// <param name="horseId">The id of the horse</param>
    /// <returns>The measurement groups (may be empty) or <see cref="NotFound" /> if the horse does not exist</returns>
    public ValueTask<OneOf<Success<IReadOnlyCollection<MeasurementGroup>>, NotFound>> GetGroupsOfHorseAsync(
        int horseId);

    /// <summary>
    ///     Gets a measurement group including the person, the horse with its breeds and the saddle
    /// </summary>
    /// <param name="groupId">The id of the measurement group</param>
    /// <returns>The measurement group or <see cref="NotFound" /> if it does not exist</returns>
    public ValueTask<OneOf<Success<MeasurementGroup>, NotFound>> GetGroupByIdAsync(int groupId);

    /// <summary>
    ///     Adds a new measurement group
    /// </summary>
    /// <param name="data">The values of the new measurement group</param>
    /// <returns>
    ///     The new measurement group including the person, the horse with its breeds and the saddle
    ///     or <see cref="NotFound" /> if the person, the horse or the device does not exist or the saddle does not
    ///     belong to the horse
    /// </returns>
    public ValueTask<OneOf<Success<MeasurementGroup>, NotFound>> AddGroupAsync(NewMeasurementGroupData data);

    /// <summary>
    ///     Gets all measurements of a measurement group
    /// </summary>
    /// <param name="groupId">The id of the measurement group</param>
    /// <returns>The measurements (may be empty) or <see cref="NotFound" /> if the group does not exist</returns>
    public ValueTask<OneOf<Success<IReadOnlyCollection<Measurement>>, NotFound>> GetMeasurementsOfGroupAsync(
        int groupId);

    /// <summary>
    ///     Gets a measurement of a measurement group
    /// </summary>
    /// <param name="groupId">The id of the measurement group</param>
    /// <param name="measurementId">The id of the measurement</param>
    /// <returns>The measurement or <see cref="NotFound" /> if it does not exist in the group</returns>
    public ValueTask<OneOf<Success<Measurement>, NotFound>> GetMeasurementByIdAsync(int groupId, int measurementId);

    /// <summary>
    ///     Gets the measurement of a measurement group that matches the given pace and hand, pace and hand are unique
    ///     within a group
    /// </summary>
    /// <param name="groupId">The id of the measurement group</param>
    /// <param name="pace">The pace to filter by, null to not filter by pace</param>
    /// <param name="hand">The hand to filter by, null to not filter by hand</param>
    /// <returns>
    ///     The measurement or <see cref="NotFound" /> if the group or a matching measurement does not exist
    /// </returns>
    public ValueTask<OneOf<Success<Measurement>, NotFound>> GetMeasurementByFilterAsync(
        int groupId, string? pace, string? hand);

    /// <summary>
    ///     Gets the average of all data values of the measurements of a group that match the given pace and hand
    /// </summary>
    /// <param name="groupId">The id of the measurement group</param>
    /// <param name="pace">The pace to filter by, null to not filter by pace</param>
    /// <param name="hand">The hand to filter by, null to not filter by hand</param>
    /// <returns>
    ///     The average, <see cref="NotFound" /> if the group does not exist or there are no matching data values
    ///     or <see cref="InvalidMeasurementData" /> if a data entry cannot be parsed as numbers
    /// </returns>
    public ValueTask<OneOf<Success<double>, NotFound, InvalidMeasurementData>> GetGroupAverageAsync(
        int groupId, string? pace, string? hand);

    /// <summary>
    ///     Gets the minimum of all data values of the measurements of a group that match the given pace and hand
    /// </summary>
    /// <param name="groupId">The id of the measurement group</param>
    /// <param name="pace">The pace to filter by, null to not filter by pace</param>
    /// <param name="hand">The hand to filter by, null to not filter by hand</param>
    /// <returns>
    ///     The minimum, <see cref="NotFound" /> if the group does not exist or there are no matching data values
    ///     or <see cref="InvalidMeasurementData" /> if a data entry cannot be parsed as numbers
    /// </returns>
    public ValueTask<OneOf<Success<double>, NotFound, InvalidMeasurementData>> GetGroupMinAsync(
        int groupId, string? pace, string? hand);

    /// <summary>
    ///     Gets the maximum of all data values of the measurements of a group that match the given pace and hand
    /// </summary>
    /// <param name="groupId">The id of the measurement group</param>
    /// <param name="pace">The pace to filter by, null to not filter by pace</param>
    /// <param name="hand">The hand to filter by, null to not filter by hand</param>
    /// <returns>
    ///     The maximum, <see cref="NotFound" /> if the group does not exist or there are no matching data values
    ///     or <see cref="InvalidMeasurementData" /> if a data entry cannot be parsed as numbers
    /// </returns>
    public ValueTask<OneOf<Success<double>, NotFound, InvalidMeasurementData>> GetGroupMaxAsync(
        int groupId, string? pace, string? hand);

    /// <summary>
    ///     Gets the average of all data values of a measurement
    /// </summary>
    /// <param name="groupId">The id of the measurement group</param>
    /// <param name="measurementId">The id of the measurement</param>
    /// <returns>
    ///     The average, <see cref="NotFound" /> if the measurement does not exist in the group or has no data values
    ///     or <see cref="InvalidMeasurementData" /> if a data entry cannot be parsed as numbers
    /// </returns>
    public ValueTask<OneOf<Success<double>, NotFound, InvalidMeasurementData>> GetMeasurementAverageAsync(
        int groupId, int measurementId);

    /// <summary>
    ///     Gets the minimum of all data values of a measurement
    /// </summary>
    /// <param name="groupId">The id of the measurement group</param>
    /// <param name="measurementId">The id of the measurement</param>
    /// <returns>
    ///     The minimum, <see cref="NotFound" /> if the measurement does not exist in the group or has no data values
    ///     or <see cref="InvalidMeasurementData" /> if a data entry cannot be parsed as numbers
    /// </returns>
    public ValueTask<OneOf<Success<double>, NotFound, InvalidMeasurementData>> GetMeasurementMinAsync(
        int groupId, int measurementId);

    /// <summary>
    ///     Gets the maximum of all data values of a measurement
    /// </summary>
    /// <param name="groupId">The id of the measurement group</param>
    /// <param name="measurementId">The id of the measurement</param>
    /// <returns>
    ///     The maximum, <see cref="NotFound" /> if the measurement does not exist in the group or has no data values
    ///     or <see cref="InvalidMeasurementData" /> if a data entry cannot be parsed as numbers
    /// </returns>
    public ValueTask<OneOf<Success<double>, NotFound, InvalidMeasurementData>> GetMeasurementMaxAsync(
        int groupId, int measurementId);

    /// <summary>
    ///     Gets all data entries of a measurement, ordered by their timestamp
    /// </summary>
    /// <param name="groupId">The id of the measurement group</param>
    /// <param name="measurementId">The id of the measurement</param>
    /// <returns>
    ///     The data entries (may be empty) or <see cref="NotFound" /> if the measurement does not exist in the group
    /// </returns>
    public ValueTask<OneOf<Success<IReadOnlyCollection<MeasurementData>>, NotFound>> GetDataOfMeasurementAsync(
        int groupId, int measurementId);

    /// <summary>
    ///     Gets a data entry of a measurement
    /// </summary>
    /// <param name="groupId">The id of the measurement group</param>
    /// <param name="measurementId">The id of the measurement</param>
    /// <param name="dataId">The id of the data entry</param>
    /// <returns>
    ///     The data entry or <see cref="NotFound" /> if the measurement does not exist in the group or the data entry
    ///     does not exist for the measurement
    /// </returns>
    public ValueTask<OneOf<Success<MeasurementData>, NotFound>> GetDataByIdAsync(
        int groupId, int measurementId, int dataId);

    /// <summary>
    ///     Adds a new measurement to a measurement group
    /// </summary>
    /// <param name="groupId">The id of the measurement group</param>
    /// <param name="pace">The pace (Gangart) of the measurement</param>
    /// <param name="hand">The hand (Seite) of the measurement</param>
    /// <param name="description">An optional description of the measurement</param>
    /// <returns>
    ///     The new measurement, <see cref="NotFound" /> if the group does not exist
    ///     or <see cref="MeasurementAlreadyExists" /> if a measurement with the same pace and hand already exists
    /// </returns>
    public ValueTask<OneOf<Success<Measurement>, NotFound, MeasurementAlreadyExists>> AddMeasurementAsync(
        int groupId, string pace, string hand, string? description);

    /// <summary>
    ///     Deletes a measurement including its data entries
    /// </summary>
    /// <param name="groupId">The id of the measurement group</param>
    /// <param name="measurementId">The id of the measurement</param>
    /// <returns><see cref="Success" /> or <see cref="NotFound" /> if the measurement does not exist in the group</returns>
    public ValueTask<OneOf<Success, NotFound>> DeleteMeasurementAsync(int groupId, int measurementId);

    /// <summary>
    ///     Adds a data entry to the measurement that is currently recorded by a device, which is the most recently
    ///     added measurement of the device's measurement groups
    /// </summary>
    /// <param name="deviceId">The id of the measurement device</param>
    /// <param name="data">The data entry, a single number or a JSON array of numbers</param>
    /// <param name="timestamp">The time the data was received</param>
    /// <returns>
    ///     <see cref="Success" />, <see cref="NotFound" /> if the device has no measurement
    ///     or <see cref="InvalidMeasurementData" /> if the data cannot be parsed as numbers
    /// </returns>
    public ValueTask<OneOf<Success, NotFound, InvalidMeasurementData>> AddDataToLatestMeasurementOfDeviceAsync(
        string deviceId, string data, Instant timestamp);

    /// <summary>
    ///     Returned if a data entry cannot be parsed as numbers, a data entry is either a single number or a JSON
    ///     array of numbers
    /// </summary>
    public readonly record struct InvalidMeasurementData;

    /// <summary>
    ///     Returned if a measurement with the same pace and hand already exists in the group
    /// </summary>
    public readonly record struct MeasurementAlreadyExists;
}

internal sealed class MeasurementService(IUnitOfWork uow, ILogger<MeasurementService> logger) : IMeasurementService
{
    public async ValueTask<OneOf<Success<IReadOnlyCollection<MeasurementGroup>>, NotFound>> GetGroupsOfHorseAsync(
        int horseId)
    {
        if (!await uow.HorseRepository.HorseExistsAsync(horseId))
        {
            logger.LogInformation("Horse {HorseId} not found", horseId);

            return new NotFound();
        }

        var groups = await uow.MeasurementRepository.GetGroupsOfHorseAsync(horseId);

        return new Success<IReadOnlyCollection<MeasurementGroup>>(groups);
    }

    public async ValueTask<OneOf<Success<MeasurementGroup>, NotFound>> GetGroupByIdAsync(int groupId)
    {
        var group = await uow.MeasurementRepository.GetGroupByIdAsync(groupId);
        if (group is null)
        {
            LogGroupNotFound(groupId);

            return new NotFound();
        }

        return new Success<MeasurementGroup>(group);
    }

    public async ValueTask<OneOf<Success<MeasurementGroup>, NotFound>> AddGroupAsync(NewMeasurementGroupData data)
    {
        if (!await uow.PersonRepository.PersonExistsAsync(data.PersonId))
        {
            logger.LogInformation("Cannot add measurement group, person {PersonId} not found", data.PersonId);

            return new NotFound();
        }

        if (!await uow.HorseRepository.SaddleOfHorseExistsAsync(data.HorseId, data.SaddleId))
        {
            logger.LogInformation("Cannot add measurement group, saddle {SaddleId} of horse {HorseId} not found",
                                  data.SaddleId, data.HorseId);

            return new NotFound();
        }

        if (!await uow.DeviceRepository.DeviceExistsAsync(data.DeviceId))
        {
            logger.LogInformation("Cannot add measurement group, device {DeviceId} not found", data.DeviceId);

            return new NotFound();
        }

        var group = new MeasurementGroup
        {
            Name = data.Name,
            Date = data.Date,
            Notes = data.Notes,
            PersonId = data.PersonId,
            HorseId = data.HorseId,
            SaddleId = data.SaddleId,
            DeviceId = data.DeviceId
        };

        uow.MeasurementRepository.AddGroup(group);
        await uow.SaveChangesAsync();
        logger.LogInformation("Measurement group {GroupId} added for horse {HorseId}", group.Id, data.HorseId);

        // loads the person, the horse and the saddle, which are only referenced by their ids
        var addedGroup = await uow.MeasurementRepository.GetGroupByIdAsync(group.Id);

        return new Success<MeasurementGroup>(addedGroup ?? group);
    }

    public async ValueTask<OneOf<Success<IReadOnlyCollection<Measurement>>, NotFound>> GetMeasurementsOfGroupAsync(
        int groupId)
    {
        if (!await uow.MeasurementRepository.GroupExistsAsync(groupId))
        {
            LogGroupNotFound(groupId);

            return new NotFound();
        }

        var measurements = await uow.MeasurementRepository.GetMeasurementsOfGroupAsync(groupId);

        return new Success<IReadOnlyCollection<Measurement>>(measurements);
    }

    public async ValueTask<OneOf<Success<Measurement>, NotFound>> GetMeasurementByIdAsync(int groupId,
                                                                                        int measurementId)
    {
        var measurement = await uow.MeasurementRepository.GetMeasurementByIdAsync(groupId, measurementId);
        if (measurement is null)
        {
            LogMeasurementNotFound(groupId, measurementId);

            return new NotFound();
        }

        return new Success<Measurement>(measurement);
    }

    public async ValueTask<OneOf<Success<Measurement>, NotFound>> GetMeasurementByFilterAsync(
        int groupId, string? pace, string? hand)
    {
        if (!await uow.MeasurementRepository.GroupExistsAsync(groupId))
        {
            LogGroupNotFound(groupId);

            return new NotFound();
        }

        var measurement = await uow.MeasurementRepository.GetMeasurementByFilterAsync(groupId, pace, hand);
        if (measurement is null)
        {
            logger.LogInformation("No measurement with pace {Pace} and hand {Hand} found in group {GroupId}",
                                  pace, hand, groupId);

            return new NotFound();
        }

        return new Success<Measurement>(measurement);
    }

    public ValueTask<OneOf<Success<double>, NotFound, IMeasurementService.InvalidMeasurementData>>
        GetGroupAverageAsync(int groupId, string? pace, string? hand) =>
        GetAggregateOfGroupAsync(groupId, pace, hand, values => values.Average());

    public ValueTask<OneOf<Success<double>, NotFound, IMeasurementService.InvalidMeasurementData>> GetGroupMinAsync(
        int groupId, string? pace, string? hand) =>
        GetAggregateOfGroupAsync(groupId, pace, hand, values => values.Min());

    public ValueTask<OneOf<Success<double>, NotFound, IMeasurementService.InvalidMeasurementData>> GetGroupMaxAsync(
        int groupId, string? pace, string? hand) =>
        GetAggregateOfGroupAsync(groupId, pace, hand, values => values.Max());

    public ValueTask<OneOf<Success<double>, NotFound, IMeasurementService.InvalidMeasurementData>>
        GetMeasurementAverageAsync(int groupId, int measurementId) =>
        GetAggregateOfMeasurementAsync(groupId, measurementId, values => values.Average());

    public ValueTask<OneOf<Success<double>, NotFound, IMeasurementService.InvalidMeasurementData>>
        GetMeasurementMinAsync(int groupId, int measurementId) =>
        GetAggregateOfMeasurementAsync(groupId, measurementId, values => values.Min());

    public ValueTask<OneOf<Success<double>, NotFound, IMeasurementService.InvalidMeasurementData>>
        GetMeasurementMaxAsync(int groupId, int measurementId) =>
        GetAggregateOfMeasurementAsync(groupId, measurementId, values => values.Max());

    public async ValueTask<OneOf<Success<IReadOnlyCollection<MeasurementData>>, NotFound>> GetDataOfMeasurementAsync(
        int groupId, int measurementId)
    {
        if (!await uow.MeasurementRepository.MeasurementExistsAsync(groupId, measurementId))
        {
            LogMeasurementNotFound(groupId, measurementId);

            return new NotFound();
        }

        var data = await uow.MeasurementRepository.GetDataOfMeasurementAsync(measurementId);

        return new Success<IReadOnlyCollection<MeasurementData>>(data);
    }

    public async ValueTask<OneOf<Success<MeasurementData>, NotFound>> GetDataByIdAsync(
        int groupId, int measurementId, int dataId)
    {
        if (!await uow.MeasurementRepository.MeasurementExistsAsync(groupId, measurementId))
        {
            LogMeasurementNotFound(groupId, measurementId);

            return new NotFound();
        }

        var data = await uow.MeasurementRepository.GetDataByIdAsync(measurementId, dataId);
        if (data is null)
        {
            logger.LogInformation("Data entry {DataId} of measurement {MeasurementId} not found",
                                  dataId, measurementId);

            return new NotFound();
        }

        return new Success<MeasurementData>(data);
    }

    public async ValueTask<OneOf<Success<Measurement>, NotFound, IMeasurementService.MeasurementAlreadyExists>>
        AddMeasurementAsync(int groupId, string pace, string hand, string? description)
    {
        if (!await uow.MeasurementRepository.GroupExistsAsync(groupId))
        {
            LogGroupNotFound(groupId);

            return new NotFound();
        }

        var existingMeasurement = await uow.MeasurementRepository.GetMeasurementByFilterAsync(groupId, pace, hand);
        if (existingMeasurement is not null)
        {
            logger.LogInformation("Cannot add measurement, pace {Pace} and hand {Hand} already exist in group {GroupId}",
                                  pace, hand, groupId);

            return new IMeasurementService.MeasurementAlreadyExists();
        }

        var measurement = new Measurement
        {
            GroupId = groupId,
            Pace = pace,
            Hand = hand,
            Description = description
        };

        uow.MeasurementRepository.AddMeasurement(measurement);
        await uow.SaveChangesAsync();
        logger.LogInformation("Measurement {MeasurementId} added to group {GroupId}", measurement.Id, groupId);

        return new Success<Measurement>(measurement);
    }

    public async ValueTask<OneOf<Success, NotFound>> DeleteMeasurementAsync(int groupId, int measurementId)
    {
        var measurement = await uow.MeasurementRepository.GetMeasurementByIdAsync(groupId, measurementId);
        if (measurement is null)
        {
            LogMeasurementNotFound(groupId, measurementId);

            return new NotFound();
        }

        uow.MeasurementRepository.RemoveMeasurement(measurement);
        await uow.SaveChangesAsync();
        logger.LogInformation("Measurement {MeasurementId} deleted from group {GroupId}", measurementId, groupId);

        return new Success();
    }

    public async ValueTask<OneOf<Success, NotFound, IMeasurementService.InvalidMeasurementData>>
        AddDataToLatestMeasurementOfDeviceAsync(string deviceId, string data, Instant timestamp)
    {
        var trimmedData = data.Trim();
        if (!TryParseValues(trimmedData, []))
        {
            logger.LogWarning("Data from device {DeviceId} cannot be parsed as numbers: {Data}", deviceId, trimmedData);

            return new IMeasurementService.InvalidMeasurementData();
        }

        var measurement = await uow.MeasurementRepository.GetLatestMeasurementOfDeviceAsync(deviceId);
        if (measurement is null)
        {
            logger.LogWarning("Dropping data, device {DeviceId} has no measurement to record into", deviceId);

            return new NotFound();
        }

        uow.MeasurementRepository.AddData(new MeasurementData
        {
            Data = trimmedData,
            Timestamp = timestamp,
            MeasurementId = measurement.Id
        });
        await uow.SaveChangesAsync();
        logger.LogDebug("Data stored in measurement {MeasurementId}", measurement.Id);

        return new Success();
    }

    private async ValueTask<OneOf<Success<double>, NotFound, IMeasurementService.InvalidMeasurementData>>
        GetAggregateOfGroupAsync(int groupId, string? pace, string? hand, Func<List<double>, double> aggregate)
    {
        if (!await uow.MeasurementRepository.GroupExistsAsync(groupId))
        {
            LogGroupNotFound(groupId);

            return new NotFound();
        }

        var data = await uow.MeasurementRepository.GetDataOfGroupAsync(groupId, pace, hand);

        return Aggregate(data, aggregate);
    }

    private async ValueTask<OneOf<Success<double>, NotFound, IMeasurementService.InvalidMeasurementData>>
        GetAggregateOfMeasurementAsync(int groupId, int measurementId, Func<List<double>, double> aggregate)
    {
        if (!await uow.MeasurementRepository.MeasurementExistsAsync(groupId, measurementId))
        {
            LogMeasurementNotFound(groupId, measurementId);

            return new NotFound();
        }

        var data = await uow.MeasurementRepository.GetDataOfMeasurementAsync(measurementId);

        return Aggregate(data, aggregate);
    }

    private OneOf<Success<double>, NotFound, IMeasurementService.InvalidMeasurementData> Aggregate(
        IReadOnlyCollection<MeasurementData> data, Func<List<double>, double> aggregate)
    {
        List<double> values = [];
        foreach (var entry in data)
        {
            if (!TryParseValues(entry.Data, values))
            {
                logger.LogInformation("Data entry {DataId} cannot be parsed as numbers", entry.Id);

                return new IMeasurementService.InvalidMeasurementData();
            }
        }

        if (values.Count == 0)
        {
            logger.LogInformation("No data values found to aggregate");

            return new NotFound();
        }

        return new Success<double>(aggregate(values));
    }

    /// <summary>
    ///     Parses a data entry, which is either a single number or a JSON array of numbers (e.g. a pressure curve).
    ///     Numbers are always parsed with the invariant culture, so a decimal point is expected.
    /// </summary>
    /// <param name="data">The raw data of the entry</param>
    /// <param name="values">The list the parsed values are added to</param>
    /// <returns>True if the whole entry could be parsed into finite numbers, false otherwise</returns>
    private static bool TryParseValues(string data, List<double> values)
    {
        var trimmedData = data.Trim();
        if (!trimmedData.StartsWith('['))
        {
            if (!double.TryParse(trimmedData, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)
                || !double.IsFinite(value))
            {
                return false;
            }

            values.Add(value);

            return true;
        }

        double[]? arrayValues;
        try
        {
            arrayValues = JsonSerializer.Deserialize<double[]>(trimmedData);
        }
        catch (JsonException)
        {
            return false;
        }

        if (arrayValues is null || !arrayValues.All(double.IsFinite))
        {
            return false;
        }

        values.AddRange(arrayValues);

        return true;
    }

    private void LogGroupNotFound(int groupId)
    {
        logger.LogInformation("Measurement group {GroupId} not found", groupId);
    }

    private void LogMeasurementNotFound(int groupId, int measurementId)
    {
        logger.LogInformation("Measurement {MeasurementId} not found in group {GroupId}", measurementId, groupId);
    }
}