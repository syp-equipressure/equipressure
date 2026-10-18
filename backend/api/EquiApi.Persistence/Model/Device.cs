namespace EquiApi.Persistence.Model;

public class MeasurementDevice
{
    /// <summary>
    ///     Serial number of the device, provided by the client
    /// </summary>
    public required string Id { get; set; }

    public int OwnerId { get; set; }
    public Person Owner { get; set; } = null!;

    public int CategoryId { get; set; }
    public DeviceCategory Category { get; set; } = null!;

    public List<MeasurementGroup> MeasurementGroups { get; set; } = [];

    /// <summary>
    ///     Additional users of the device - the owner is not part of this list
    /// </summary>
    public List<DeviceUser> Users { get; set; } = [];
}

public class DeviceCategory
{
    public int Id { get; set; }

    // TODO: confirm that this is the maximum number of users (excluding the owner) of a device
    public int NumOfAllowedPeople { get; set; }

    // TODO: enum for name?
    public required string Name { get; set; }

    public List<MeasurementDevice> Devices { get; set; } = [];
}

public class DeviceUser
{
    public int UserId { get; set; }
    public Person User { get; set; } = null!;

    public required string DeviceId { get; set; }
    public MeasurementDevice Device { get; set; } = null!;
}