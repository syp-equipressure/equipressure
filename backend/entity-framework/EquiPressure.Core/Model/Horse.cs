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
    public List<HorseBreed> HorseBreeds { get; set; } = null!;
    public int AddressId { get; set; }
    public List<Address> HorseAddress { get; set; } = null!;
}

public class HorseBreed
{
    
}

public class Breed
{
    
}

