namespace EquiApi.Persistence.Model;

public class Address
{
    public int Id { get; set; }

    // The street is optional, only the PLZ is required (in case someone does not want to share where exactly
    // their horse is kept), but we still want to be able to show horses and riders on the map
    public string? AddressName { get; set; }
    public required string PLZ { get; set; }
    public required string CityName { get; set; }

    public List<Person> Persons { get; set; } = [];
    public List<Horse> Horses { get; set; } = [];
}