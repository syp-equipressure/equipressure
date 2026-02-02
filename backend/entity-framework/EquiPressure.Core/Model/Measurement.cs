using NodaTime;

namespace EquiPressure.Core.Model;

public class MeasurementGroup
{
    public int Id { get; set; }
    public LocalDate Date { get; set; }
    public required string Name { get; set; }
    public string? Notes { get; set; }

    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;

    public int DeviceId { get; set; }
    public MeasurementDevice Device { get; set; } = null!;

    public int HorseId { get; set; }
    public Horse Horse { get; set; } = null!;

    public int SaddleId { get; set; }
    public Saddle Saddle { get; set; } = null!;

    public List<MeasurementEligibility> MeasurementEligibilities { get; set; } = [];
    public List<Measurement> Measurements { get; set; } = [];
}

public class Measurement
{
    public int Id { get; set; }
    // the erd says "paces" but i'm not sure there's multiple in there
    // TODO: Ask Flora what that's supposed to be
    public required string Pace { get; set; }
    // just says which side of the horse is faced to the middle of the riding hall
    public required string Hand { get; set; }
    public string? Description { get; set; }
    
    public int GroupId { get; set; }
    public MeasurementGroup Group { get; set; } = null!;
    
    public List<MeasurementData> MeasurementDates { get; set; } = [];
}

public class MeasurementData
{
    public int Id { get; set; }
    public required string Data { get; set; }
    public Instant Timestamp { get; set; }
    
    public int MeasurementId { get; set; }
    public Measurement Measurement { get; set; } = null!;
}
