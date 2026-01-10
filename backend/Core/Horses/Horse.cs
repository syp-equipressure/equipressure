namespace EquiPressure.Core.Horses;

public class Horse
{
    public int HorseId { get; set; }

    public int PersonId { get; set; }   // Owner

    public required string Name { get; set; }
    public LocalDate Dob { get; set; }

    public int WeightKg { get; set; }
    public int HeightCm { get; set; }

    public required string Breed { get; set; }
    public required string Sex { get; set; }
}

/// <summary>
///     every Horse has one or more saddles
///     a saddle only has one horse, so we can set the ID here
/// </summary>
public class Saddle
{
    public int SaddleId { get; set; }
    public int HorseId { get; set; }

    // e.g. to show the Saddle in a dropdown menu
    public required string Name { get; set; } 
}
