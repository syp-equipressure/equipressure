using NodaTime;

namespace EquiPressure.Core.Model;

public class Release
{
    public int Id { get; set; }
    public Instant ReleaseTimestamp { get; set; }

    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;

    public List<MeasurementEligibility> MeasurementEligibilities { get; set; } = [];
}

public class MeasurementEligibility
{
    public int ReleaseId { get; set; }
    public Release Release { get; set; } = null!;

    public int MeasurementGroupId { get; set; }
    public MeasurementGroup MeasurementGroup { get; set; } = null!;
}
