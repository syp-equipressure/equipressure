using NodaTime;

namespace EquiApi.Persistence.Model;

public class Horse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public LocalDate DateOfBirth { get; set; }
    public decimal Weight { get; set; }
    public decimal Height { get; set; }
    public HorseGender Gender { get; set; }

    public int AddressId { get; set; }
    public Address Address { get; set; } = null!;

    public int BreedId { get; set; }
    public Breed Breed { get; set; } = null!;

    public List<PersonHorse> Persons { get; set; } = [];
    public List<Saddle> Saddles { get; set; } = [];
    public List<MeasurementGroup> MeasurementGroups { get; set; } = [];
}

public class Breed
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public List<Horse> Horses { get; set; } = [];
}

public enum HorseGender
{
    Female = 10,
    Male = 20
}
