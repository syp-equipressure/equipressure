namespace EquiApi.Persistence.Model;

public class Saddle
{
    public int Id { get; set; }
    public required string Name { get; set; }
    
    public int HorseId { get; set; }
    public Horse Horse { get; set; } = null!;
    
    public int CategoryId { get; set; }
    public SaddleCategory Category { get; set; } = null!;
    
    public List<MeasurementGroup> MeasurementGroups { get; set; } = [];
}

public class SaddleCategory
{
    public int Id { get; set; }
    public required string Name { get; set; }
    // TODO: Ask Flora what this is supposed to do; you can always just select the count via sql
    public int Counter { get; set; }
    
    public List<Saddle> Saddles { get; set; } = [];
}