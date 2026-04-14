namespace EquiPressure.Core.Model;

public class MeasurementDevice
{
    public int Id { get; set; }

    public int OwnerId { get; set; }
    public Person Owner { get; set; } = null!;

    public int CategoryId { get; set; }
    public DeviceCategory Category { get; set; } = null!;

    public List<MeasurementGroup> MeasurementGroups { get; set; } = [];
    public List<DeviceUser> Users { get; set; } = [];
}

public class DeviceCategory
{
    public int Id { get; set; }
    // TODO: Ask if that is the real meaning behind thsi
    public int NumOfAllowedPeople { get; set; }
    public required string Name { get; set; }

    public List<MeasurementDevice> Devices = [];
}

public class DeviceUser
{
    public int UserId { get; set; }
    public Person User { get; set; } = null!;

    public int DeviceId { get; set; }
    public MeasurementDevice Device { get; set; } = null!;
}