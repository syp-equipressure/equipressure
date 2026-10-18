using EquiApi.Persistence.Model;
using FluentValidation;

namespace EquiApi.Dtos;

public sealed record MeasurementGroupSummaryDto(
    int Id,
    LocalDate Date,
    string PersonFirstName,
    string PersonLastName,
    string SaddleName)
{
    public static MeasurementGroupSummaryDto FromGroup(MeasurementGroup mg) =>
        new(mg.Id, mg.Date, mg.Person.FirstName, mg.Person.LastName, mg.Saddle.Name);
}

public sealed record MeasurementGroupListResponse(IEnumerable<MeasurementGroupSummaryDto> Groups)
{
    public static MeasurementGroupListResponse FromGroups(IEnumerable<MeasurementGroup> groups) =>
        new(groups.Select(MeasurementGroupSummaryDto.FromGroup));
}

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
    public static MeasurementGroupDetailDto FromGroup(MeasurementGroup mg) =>
        new(mg.Id,
            mg.Date,
            mg.Name,
            mg.Notes,
            mg.Horse.Name,
            mg.Horse.HorseBreeds.FirstOrDefault()?.Breed.Name ?? string.Empty,
            mg.Horse.DateOfBirth,
            mg.Horse.Weight,
            mg.Person.FirstName,
            mg.Person.LastName,
            mg.Person.Height,
            mg.Person.Weight,
            mg.Saddle.Name);
}

public sealed record MeasurementDto(int Id, string Pace, string Hand, string? Description)
{
    public static MeasurementDto FromMeasurement(Measurement m) =>
        new(m.Id, m.Pace, m.Hand, m.Description);
}

public sealed record MeasurementListResponse(IEnumerable<MeasurementDto> Measurements)
{
    public static MeasurementListResponse FromMeasurements(IEnumerable<Measurement> measurements) =>
        new(measurements.Select(MeasurementDto.FromMeasurement));
}

public sealed record MeasurementDataDto(int Id, string Data, Instant Timestamp)
{
    public static MeasurementDataDto FromData(MeasurementData d) =>
        new(d.Id, d.Data, d.Timestamp);
}

public sealed record MeasurementDataListResponse(IEnumerable<MeasurementDataDto> Data)
{
    public static MeasurementDataListResponse FromData(IEnumerable<MeasurementData> data) =>
        new(data.Select(MeasurementDataDto.FromData));
}

public sealed record AggregateResponse(double Value);

public sealed record AddMeasurementRequest(string Pace, string Hand, string? Description)
{
    public sealed class Validator : AbstractValidator<AddMeasurementRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Pace)
                .NotEmpty();

            RuleFor(x => x.Hand)
                .NotEmpty();

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .When(x => x.Description is not null);
        }
    }
}
