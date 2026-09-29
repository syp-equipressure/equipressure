namespace EquiApi.Persistence.Model;

public class Address
{
    public int Id { get; set; }
    // Straße ist nicht Pflicht anzugeben, sondern nur PLZ (falls man nicht genau sagen will wo das Pferd steht)
    // aber wir wollen trotzdem das Pferd und den Reiter irgendwie in der Map anzeigen
    public string? AddressName { get; set; }
    public required string PLZ { get; set; }
    public required string CityName { get; set; }


    public List<Person> Persons { get; set; } = [];
    public List<Horse> Horses { get; set; } = [];
}

