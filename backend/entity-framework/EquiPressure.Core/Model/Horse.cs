using NodaTime;

namespace EquiPressure.Core.Model;

public class Horse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public LocalDate DateOfBirth { get; set; }
    public decimal Weight { get; set; }
    public decimal Height { get; set; }
    public required string Gender { get; set; }
    
    public int HorseBreedId { get; set; }
    public int AddressId { get; set; }
    public List<HorseBreed> HorseBreeds { get; set; } = null!;
    public List<Address> HorseAddress { get; set; } = null!;
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
}

