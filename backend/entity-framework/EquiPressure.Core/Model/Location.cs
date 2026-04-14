namespace EquiPressure.Core.Model;

public class Address
{
    public int Id { get; set; }
    // Straße ist nicht Pflicht anzugeben, sondern nur PLZ (falls man nicht genau sagen will wo das Pferd steht)
    // aber wir wollen trotzdem das Pferd und den Reiter irgendwie in der Map anzeigen
    public string? Street { get; set; }
    public int? HouseNumber { get; set; }

    public int CityId { get; set; }
    public City City { get; set; } = null!;

    public List<Person> Persons { get; set; } = [];
    public List<Horse> Horses { get; set; } = [];
}

public class City
{
    public int Id { get; set; }
    public required string PLZ { get; set; }
    public required string Name { get; set; }
    
    public List<Address> Addresses { get; set; } = [];
}
