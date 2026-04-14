using NodaTime;

namespace EquiPressure.Core.Model;

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

    public List<HorseBreed> HorseBreeds { get; set; } = [];
    public List<PersonHorse> Persons { get; set; } = [];
    public List<Saddle> Saddles { get; set; } = [];
    public List<MeasurementGroup> MeasurementGroups { get; set; } = [];
}

public class HorseBreed
{
    public int BreedId { get; set; }
    public int HorseId { get; set; }

    public Breed Breed { get; set; } = null!;
    public Horse Horse { get; set; } = null!;
}

public class Breed
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int Counter { get; set; }
    
    public List<HorseBreed> HorseBreeds { get; set; } = [];
}

public enum HorseGender
{
    Female = 10,
    Male = 20
}
