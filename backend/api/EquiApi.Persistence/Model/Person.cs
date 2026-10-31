namespace EquiApi.Persistence.Model;

public class Person
{
    public int Id { get; set; }

    /// <summary>
    ///     Id of the user in Keycloak (sub claim of the access token), null for persons without an account
    ///     (e.g. customers created by a saddler)
    /// </summary>
    public string? ExternalId { get; set; }

    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public decimal Height { get; set; } // in cm
    public decimal Weight { get; set; } // in kg
    public LocalDate DateOfBirth { get; set; }
    public string? Email { get; set; }
    public string? WebsiteLink { get; set; }
    public string? Description { get; set; }

    /// <summary>
    ///     When the person was added, set by the database ("member since" in the apps)
    /// </summary>
    public Instant CreatedAt { get; set; }

    // only with an image, the content is stored separately so that lists of persons stay small
    public string? ImageContentType { get; set; }
    public Instant? ImageUpdatedAt { get; set; }
    public PersonImage? Image { get; set; }

    public int AddressId { get; set; }
    public Address Address { get; set; } = null!;

    public List<PersonRelationship> Relationships { get; set; } = [];
    public List<PersonRoleAssignment> Roles { get; set; } = [];
    public List<PersonHorse> Horses { get; set; } = [];
    public List<MeasurementGroup> MeasurementGroups { get; set; } = [];
    public List<DeviceUser> UserDevices { get; set; } = [];
    public List<MeasurementDevice> OwnerDevices { get; set; } = [];
    public List<Release> Releases { get; set; } = [];
}

/// <summary>
///     The content of the image of a person
/// </summary>
public class PersonImage
{
    public int PersonId { get; set; }
    public Person Person { get; set; } = null!;

    public required byte[] Data { get; set; }
}

// m:n relationship between an equestrian and a saddler
public class PersonRelationship
{
    public int EquestrianId { get; set; }
    public int SaddlerId { get; set; }

    public bool IsContact { get; set; }
    public bool IsFavourite { get; set; }

    public Person Equestrian { get; set; } = null!;
    public Person Saddler { get; set; } = null!;
}

public class AccountRole
{
    public int Id { get; set; }
    public required RoleName Name { get; set; }

    public List<PersonRoleAssignment> RoleAssignments { get; set; } = [];
}

public class PersonRoleAssignment
{
    public int PersonId { get; set; }
    public int RoleId { get; set; }

    public Person Person { get; set; } = null!;
    public AccountRole Role { get; set; } = null!;
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

public enum RoleName
{
    Equestrian = 10,
    Saddler = 20,
    Guest = 30
}