using EquiApi.Core.Services;
using EquiApi.Persistence.Model;
using EquiApi.Shared;
using FluentValidation;

namespace EquiApi.Dtos;

/// <summary>
///     A measurement group with minimal data
/// </summary>
/// <param name="Id">The id of the measurement group</param>
/// <param name="Date">The date of the measurement group</param>
/// <param name="PersonFirstName">The first name of the rider</param>
/// <param name="PersonLastName">The last name of the rider</param>
/// <param name="SaddleName">The name of the saddle</param>
public sealed record MeasurementGroupSummaryDto(
    int Id,
    LocalDate Date,
    string PersonFirstName,
    string PersonLastName,
    string SaddleName)
{
    public static MeasurementGroupSummaryDto FromGroup(MeasurementGroup group) =>
        new(group.Id, group.Date, group.Person.FirstName, group.Person.LastName, group.Saddle.Name);
}

/// <summary>
///     A list of measurement groups with minimal data
/// </summary>
/// <param name="Groups">The measurement groups</param>
public sealed record MeasurementGroupListResponse(IReadOnlyList<MeasurementGroupSummaryDto> Groups)
{
    public static MeasurementGroupListResponse FromGroups(IEnumerable<MeasurementGroup> groups) =>
        new(groups.Select(MeasurementGroupSummaryDto.FromGroup).ToList());
}

/// <summary>
///     A measurement group with the data of the horse, the rider and the saddle
/// </summary>
/// <param name="Id">The id of the measurement group</param>
/// <param name="Date">The date of the measurement group</param>
/// <param name="Name">The name of the measurement group</param>
/// <param name="Notes">Optional notes on the measurement group</param>
/// <param name="HorseName">The name of the horse</param>
/// <param name="HorseBreed">The name of the first breed of the horse, empty if it has none</param>
/// <param name="DateOfBirth">The birthdate of the horse</param>
/// <param name="HorseWeight">The weight of the horse in kg</param>
/// <param name="RiderFirstName">The first name of the rider</param>
/// <param name="RiderLastName">The last name of the rider</param>
/// <param name="RiderHeight">The height of the rider in cm</param>
/// <param name="RiderWeight">The weight of the rider in kg</param>
/// <param name="SaddleName">The name of the saddle</param>
public sealed record MeasurementGroupDetailDto(
    int Id,
    LocalDate Date,
    string Name,
    string? Notes,
    string HorseName,
    string HorseBreed,
    LocalDate DateOfBirth,
    decimal HorseWeight,
    string RiderFirstName,
    string RiderLastName,
    decimal RiderHeight,
    decimal RiderWeight,
    string SaddleName)
{
    public static MeasurementGroupDetailDto FromGroup(MeasurementGroup group) =>
        new(group.Id, group.Date, group.Name, group.Notes, group.Horse.Name,
            group.Horse.HorseBreeds.FirstOrDefault()?.Breed.Name ?? string.Empty, group.Horse.DateOfBirth,
            group.Horse.Weight, group.Person.FirstName, group.Person.LastName, group.Person.Height,
            group.Person.Weight, group.Saddle.Name);
}

/// <summary>
///     A measurement
/// </summary>
/// <param name="Id">The id of the measurement</param>
/// <param name="Pace">The pace (Gangart) of the measurement</param>
/// <param name="Hand">The hand (Seite) of the measurement</param>
/// <param name="Description">An optional description of the measurement</param>
public sealed record MeasurementDto(int Id, string Pace, string Hand, string? Description)
{
    public static MeasurementDto FromMeasurement(Measurement measurement) =>
        new(measurement.Id, measurement.Pace, measurement.Hand, measurement.Description);
}

/// <summary>
///     A list of measurements
/// </summary>
/// <param name="Measurements">The measurements</param>
public sealed record MeasurementListResponse(IReadOnlyList<MeasurementDto> Measurements)
{
    public static MeasurementListResponse FromMeasurements(IEnumerable<Measurement> measurements) =>
        new(measurements.Select(MeasurementDto.FromMeasurement).ToList());
}

/// <summary>
///     A data entry of a measurement
/// </summary>
/// <param name="Id">The id of the data entry</param>
/// <param name="Data">The measured value</param>
/// <param name="Timestamp">The point in time the value was measured</param>
public sealed record MeasurementDataDto(int Id, string Data, Instant Timestamp)
{
    public static MeasurementDataDto FromData(MeasurementData data) =>
        new(data.Id, data.Data, data.Timestamp);
}

/// <summary>
///     A list of data entries of a measurement
/// </summary>
/// <param name="Data">The data entries</param>
public sealed record MeasurementDataListResponse(IReadOnlyList<MeasurementDataDto> Data)
{
    public static MeasurementDataListResponse FromData(IEnumerable<MeasurementData> data) =>
        new(data.Select(MeasurementDataDto.FromData).ToList());
}

/// <summary>
///     An aggregated value (average, minimum or maximum) of the data of a measurement
/// </summary>
/// <param name="Value">The aggregated value</param>
public sealed record AggregateResponse(double Value);

/// <summary>
///     Request to add a measurement group
/// </summary>
/// <param name="Name">The name of the measurement group</param>
/// <param name="Date">The date of the measurement group, must not be in the future</param>
/// <param name="Notes">Optional notes on the measurement group</param>
/// <param name="PersonId">The id of the rider</param>
/// <param name="HorseId">The id of the horse</param>
/// <param name="SaddleId">The id of the saddle, it has to belong to the horse</param>
/// <param name="DeviceId">The serial number of the measurement device</param>
public sealed record AddMeasurementGroupRequest(
    string Name,
    LocalDate Date,
    string? Notes,
    int PersonId,
    int HorseId,
    int SaddleId,
    string DeviceId)
{
    public NewMeasurementGroupData ToData() => new(Name, Date, Notes, PersonId, HorseId, SaddleId, DeviceId);

    public sealed class Validator : AbstractValidator<AddMeasurementGroupRequest>
    {
        public Validator(IClock clock)
        {
            RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
            RuleFor(x => x.Date).LessThanOrEqualTo(clock.LocalDate);
            RuleFor(x => x.Notes).MaximumLength(500).When(x => x.Notes is not null);
            RuleFor(x => x.PersonId).GreaterThan(0);
            RuleFor(x => x.HorseId).GreaterThan(0);
            RuleFor(x => x.SaddleId).GreaterThan(0);
            RuleFor(x => x.DeviceId).NotEmpty();
        }
    }
}

/// <summary>
///     Request to add a measurement to a measurement group
/// </summary>
/// <param name="Pace">The pace (Gangart) of the measurement</param>
/// <param name="Hand">The hand (Seite) of the measurement</param>
/// <param name="Description">An optional description of the measurement</param>
/// <param name="Recordings">
///     The optional recordings of the measurement, every recording is stored as one data entry
/// </param>
public sealed record AddMeasurementRequest(
    string Pace,
    string Hand,
    string? Description,
    IReadOnlyList<RecordingRequest>? Recordings = null)
{
    public IReadOnlyList<NewRecordingData> ToRecordingData() =>
        (Recordings ?? []).Select(recording => recording.ToData()).ToList();

    public sealed class Validator : AbstractValidator<AddMeasurementRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Pace).NotEmpty();
            RuleFor(x => x.Hand).NotEmpty();
            RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
            RuleForEach(x => x.Recordings).NotNull().SetValidator(new RecordingRequest.Validator());
            RuleFor(x => x.Recordings)
                .Must(recordings => recordings!.Select(r => r?.Timestamp).Distinct().Count() == recordings!.Count)
                .When(x => x.Recordings is not null)
                .WithMessage("Every recording of a measurement needs its own timestamp");
        }
    }
}

/// <summary>
///     A recording of the sensor mat at one point in time
/// </summary>
/// <param name="Timestamp">When the values were recorded</param>
/// <param name="Values">The pressure values of all sensors in mbar, row by row (e.g. 400 values for a 20×20 mat)</param>
public sealed record RecordingRequest(Instant Timestamp, IReadOnlyList<double> Values)
{
    public NewRecordingData ToData() => new(Timestamp, Values);

    public sealed class Validator : AbstractValidator<RecordingRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Values).NotEmpty().WithMessage("A recording needs at least one value");
            RuleForEach(x => x.Values)
                .Must(value => double.IsFinite(value) && value >= 0)
                .WithMessage("Pressure values have to be positive numbers");
        }
    }
}