namespace EquiPressure.Core.Model;

public class Address
{
    public int Id { get; set; }
    public required string Street { get; set; }
    public int HouseNumber { get; set; }
    
    public int CityId { get; set; }
    public City City { get; set; } = null!;
}

public class City
{
    public int Id { get; set; }
    public required string PLZ { get; set; }
    public required string Name { get; set; }
}