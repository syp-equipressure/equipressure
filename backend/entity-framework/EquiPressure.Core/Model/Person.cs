using NodaTime;

namespace EquiPressure.Core.Model;

public class Person
{
    public int Id { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public decimal Height { get; set; } // in cm
    public decimal Weight { get; set; } // in kg
    public LocalDate DateOfBirth { get; set; }
    public string? Email { get; set; }
    public string? WebsiteLink { get; set; }
    public string? Description { get; set; }

    public List<PersonRelationship> Relationships { get; set; } = [];
    public List<PersonRole> Roles { get; set; } = [];
    public List<PersonHorse> Horses { get; set; } = [];
    public List<MeasurementGroup> MeasurementGroups { get; set; } = [];
    public List<DeviceUser> Devices { get; set; } = [];
    public List<MeasurementDevice> AdminDevices { get; set; } = [];
    public List<Release> Releases { get; set; } = [];   
}

// m-m bzh zwischen Person und Person
public class PersonRelationship
{
    public int Person1Id { get; set; }
    public int Person2Id { get; set; }

    public bool IsContact { get; set; }
    public bool IsFavourite { get; set; }

    public Person Person1 { get; set; } = null!;
    public Person Person2 { get; set; } = null!;
}

public class PersonRole
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public List<PersonRoleAssignment> RoleAssignments { get; set; } = [];
}

public class PersonRoleAssignment
{
    public int PersonId { get; set; }
    public int RoleId { get; set; }

    public Person Person { get; set; } = null!;
    public PersonRole Role { get; set; } = null!;
}

public class PersonHorse
{
    public int PersonId { get; set; }
    public int HorseId { get; set; }
    
    public bool IsHidden { get; set; }
    public bool IsOwner { get; set; }

    public Person Person { get; set; } = null!;
    public Horse Horse { get; set; } = null!;
}