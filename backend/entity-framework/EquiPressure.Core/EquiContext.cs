using EquiPressure.Core.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquiPressure.Core;

public class EquiContext(DbContextOptions<EquiContext> options) : DbContext(options)
{
    // Person DbSets
    public DbSet<Person> Person { get; set; }
    public DbSet<PersonRelationship> PersonRelationships { get; set; }
    public DbSet<PersonRole> PersonRoles { get; set; }
    public DbSet<PersonRoleAssignment> PersonRoleAssignments { get; set; }
    public DbSet<PersonHorse> PersonHorses { get; set; }
    // Horse DbSets
    public DbSet<Horse> Horses { get; set; }
    public DbSet<HorseBreed> HorseBreeds { get; set; }
    public DbSet<Breed> Breeds { get; set; }
    // Device DbSets
    public DbSet<MeasurementDevice> Devices { get; set; }
    public DbSet<DeviceCategory> DeviceCategories { get; set; }
    public DbSet<DeviceUser> DeviceUsers { get; set; }
    // Measurement DbSets
    public DbSet<MeasurementGroup> MeasurementGroups { get; set; }
    public DbSet<Measurement> Measurements { get; set; }
    public DbSet<MeasurementData> MeasurementDates { get; set; }
    // Release DbSets
    public DbSet<Release> Releases { get; set; }
    public DbSet<MeasurementEligibility> MeasurementEligibilities { get; set; }
    // Saddle DbSets
    public DbSet<Saddle> Saddles { get; set; }
    public DbSet<SaddleCategory> SaddleCategories { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigurePerson(modelBuilder.Entity<Person>());
        ConfigurePersonRelationship(modelBuilder.Entity<PersonRelationship>());
        ConfigurePersonRole(modelBuilder.Entity<PersonRole>());
        ConfigurePersonRoleAssignment(modelBuilder.Entity<PersonRoleAssignment>());
        ConfigurePersonHorse(modelBuilder.Entity<PersonHorse>());
    }

    private static void ConfigurePerson(EntityTypeBuilder<Person> person)
    {
        person.HasKey(p => p.Id);
        person.Property(p => p.Id).ValueGeneratedOnAdd();
        person.HasIndex(p => new { p.FirstName, p.LastName });
        person.HasIndex(p => p.Email).IsUnique();
    }

    private static void ConfigurePersonRelationship(EntityTypeBuilder<PersonRelationship> personRelation)
    {
        personRelation.HasKey(p => new { p.Person1Id, p.Person2Id });

        // TODO think about the correct delete behaviour
        // TODO: Do we really want the HasOne Relationship or the HasMany Relationship; Ask Professor Haslinger
        personRelation.HasOne(pr => pr.Person1)
                      .WithMany(p => p.Relationships)
                      .HasForeignKey(pr => pr.Person1Id)
                      .OnDelete(DeleteBehavior.Cascade);

        personRelation.HasOne(pr => pr.Person2)
                      .WithMany(p => p.Relationships)
                      .HasForeignKey(pr => pr.Person2Id)
                      .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigurePersonRole(EntityTypeBuilder<PersonRole> personRole)
    {
        personRole.HasKey(p => p.Id);
        personRole.Property(p => p.Id).ValueGeneratedOnAdd();
    }

    private static void ConfigurePersonRoleAssignment(EntityTypeBuilder<PersonRoleAssignment> personRoleAssignment)
    {
        personRoleAssignment.HasKey(p => new { p.PersonId, p.RoleId });

        personRoleAssignment.HasOne(pr => pr.Person)
                            .WithMany(p => p.Roles)
                            .HasForeignKey(pr => pr.PersonId)
                            .OnDelete(DeleteBehavior.Cascade);

        personRoleAssignment.HasOne(pr => pr.Role)
                            .WithMany(r => r.RoleAssignments)
                            .HasForeignKey(pr => pr.RoleId)
                            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigurePersonHorse(EntityTypeBuilder<PersonHorse> personHorse)
    {
        personHorse.HasKey(ph => new { ph.PersonId, ph.HorseId });

        personHorse.HasOne(ph => ph.Person)
                   .WithMany(p => p.Horses)
                   .HasForeignKey(ph => ph.PersonId)
                   .OnDelete(DeleteBehavior.Cascade);
        personHorse.HasOne(ph => ph.Horse)
                   .WithMany(h => h.Persons)
                   .HasForeignKey(ph => ph.HorseId)
                   .OnDelete(DeleteBehavior.Cascade);
    }
}
