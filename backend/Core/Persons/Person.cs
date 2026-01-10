namespace EquiPressure.Core.Persons;
/// <summary>
///     A Person to exist in our database
///     string are required because professor said that in last class
/// </summary>
public class Person
{
    public int PersonId { get; set; }

    public required string Firstname { get; set; }
    public required string Lastname { get; set; }
    public required string Email { get; set; }

    public int HeightCm { get; set; }
    public int WeightKg { get; set; }

    public int Plz { get; set; }
    public required string City { get; set; }
    public required string Street { get; set; }
    public int Housenumber { get; set; }

    public string? WebsiteLink { get; set; }
    public string? Description { get; set; }
    
    public List<CustomerRelationship> CustomerRelationships { get; set; } = [];
    public List<Role> Roles { get; set; } = [];
}
/// <summary>
/// Represents a directed relationship between two persons.
/// 
/// This table is required because the relationship between customers
/// is not a simple many-to-many relation. The relationship itself
/// carries additional domain-specific information, such as whether
/// the related person appears as a favorite or as a contact for the
/// owning person.
/// 
/// Each entry describes how <see cref="PersonId"/> perceives
/// <see cref="RelatedPersonId"/>, making the relation asymmetric
/// (Person A can mark Person B as favorite, without the inverse being true).
/// 
/// Without this associative table, these additional attributes could
/// not be modeled without losing information or introducing ambiguity.
///
/// quelle: vertrau mir Bruder
/// </summary>
public class CustomerRelationship
{
    public int PersonId { get; set; }
    public int RelatedPersonId { get; set; }

    // Appears in the favorites list of PersonId
    public bool IsFavorite { get; set; }

    // Appears in the contact list of PersonId
    public bool IsContact { get; set; }
}

/// <summary>
///     Represents a Role a Person can have
///     since a person can have multiple roles
/// </summary>
public class Role
{
    public int RoleId { get; set; }
    public required string Name { get; set; }
}
