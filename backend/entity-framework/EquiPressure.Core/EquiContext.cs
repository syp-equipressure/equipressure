using EquiPressure.Core.Model;
using Microsoft.EntityFrameworkCore;

namespace EquiPressure.Core;

public class EquiContext(DbContextOptions<EquiContext> options) : DbContext(options)
{
    // Person db sets
    public DbSet<Person> Person { get; set; }
    public DbSet<PersonRelationship> PersonRelationships { get; set; }
    public DbSet<PersonRole> PersonRoles { get; set; }
    public DbSet<PersonRoleAssignment> PersonRoleAssignments { get; set; }
    public DbSet<PersonHorse> PersonHorses { get; set; }
    //Horse db sets
    public DbSet<Horse> Horses { get; set; }
    public DbSet<HorseBreed> HorseBreeds { get; set; }
    public DbSet<Breed> Breeds { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }
}
