namespace EquiPressure.Core.Persons;

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

// Assoziationstabelle 
public class CustomerRelationship
{
    public int PersonId { get; set; }
    public int RelatedPersonId { get; set; }

    // erscheint in der Favouriten-Liste unserer Person
    public bool IsFavorite { get; set; }

    // erscheint in der Kontakt-Liste unserer Person
    public bool IsContact { get; set; }
}

public class Role
{
    public int RoleId { get; set; }
    public required string Name { get; set; }
}
