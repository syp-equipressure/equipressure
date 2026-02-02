using EquiPressure.Core.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

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
        
        ConfigurePerson(modelBuilder.Entity<Person>());
        ConfigurePersonRelationship(modelBuilder.Entity<PersonRelationship>());
        ConfigurePersonRole(modelBuilder.Entity<PersonRole>());
        ConfigurePersonRoleAssignment(modelBuilder.Entity<PersonRoleAssignment>());
        
    }

    private static void ConfigurePerson(EntityTypeBuilder<Person> person)
    {
        
    }
    private static void ConfigurePersonRelationship(EntityTypeBuilder<PersonRelationship> personRelation)
    {
        
    }
    private static void ConfigurePersonRole(EntityTypeBuilder<PersonRole> personRole)
    {
        
    }
    private static void ConfigurePersonRoleAssignment(EntityTypeBuilder<PersonRoleAssignment> personRoleAssignment)
    {
        
    }
}
