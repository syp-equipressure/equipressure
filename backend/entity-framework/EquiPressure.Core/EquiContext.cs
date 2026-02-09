using EquiPressure.Core.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EquiPressure.Core;

public class EquiContext(DbContextOptions<EquiContext> options) : DbContext(options)
{
    // Person DbSets
    public DbSet<Person> Person { get; set; }
    public DbSet<PersonRelationship> PersonRelationships { get; set; }
    public DbSet<AccountRole> PersonRoles { get; set; }
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

    // Location DbSets
    public DbSet<Address> Addresses { get; set; }
    public DbSet<City> Cities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigurePerson(modelBuilder);
        ConfigureHorse(modelBuilder);
        ConfigureAccountRole(modelBuilder);
        ConfigureLocation(modelBuilder);
        ConfigureDevice(modelBuilder);
        ConfigureMeasurement(modelBuilder);
        ConfigureSaddle(modelBuilder);
        ConfigureRelease(modelBuilder);
    }

    private static void ConfigureRelease(ModelBuilder mb)
    {
        throw new NotImplementedException();
    }

    private static void ConfigureSaddle(ModelBuilder mb)
    {
        throw new NotImplementedException();
    }

    private static void ConfigureMeasurement(ModelBuilder mb)
    {
        throw new NotImplementedException();
    }

    private static void ConfigureDevice(ModelBuilder mb)
    {
        throw new NotImplementedException();
    }

    private static void ConfigureLocation(ModelBuilder mb)
    {
        throw new NotImplementedException();
    }

    private static void ConfigurePerson(ModelBuilder mb)
    {
        #region person

        var person = mb.Entity<Person>();

        person.HasKey(p => p.Id);
        person.Property(p => p.Id).ValueGeneratedOnAdd();
        person.HasIndex(p => new { p.FirstName, p.LastName });
        person.HasIndex(p => p.Email).IsUnique();

        person.HasMany(p => p.Relationships)
              .WithOne(ps => ps.Person1)
              .HasForeignKey(ps => ps.Person1Id)
              .OnDelete(DeleteBehavior.Cascade);

        person.HasMany(p => p.Relationships)
              .WithOne(pr => pr.Person2)
              .HasForeignKey(pr => pr.Person2Id)
              .OnDelete(DeleteBehavior.Cascade);

        person.HasMany(p => p.Roles)
              .WithOne(pra => pra.Person)
              .HasForeignKey(pra => pra.PersonId)
              .OnDelete(DeleteBehavior.Cascade);

        person.HasMany(p => p.Horses)
              .WithOne(ph => ph.Person)
              .HasForeignKey(ph => ph.PersonId)
              .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region personRelation

        var personRelation = mb.Entity<PersonRelationship>();
        personRelation.HasKey(p => new { p.Person1Id, p.Person2Id });

        #endregion

        #region personHorse

        var personHorse = mb.Entity<PersonHorse>();
        personHorse.HasKey(ph => new { ph.PersonId, ph.HorseId });

        #endregion
    }

    private static void ConfigureHorse(ModelBuilder mb)
    {
        #region horse

        var horse = mb.Entity<Horse>();
        horse.HasMany(h => h.Persons)
             .WithOne(ph => ph.Horse)
             .HasForeignKey(ph => ph.HorseId)
             .OnDelete(DeleteBehavior.Cascade);

        #endregion
    }

    private static void ConfigureAccountRole(ModelBuilder mb)
    {
        #region accountRole

        var accountRole = mb.Entity<AccountRole>();
        accountRole.HasKey(p => p.Id);
        accountRole.Property(p => p.Id).ValueGeneratedOnAdd();

        accountRole.HasMany(ar => ar.RoleAssignments)
                   .WithOne(ra => ra.Role)
                   .HasForeignKey(ra => ra.RoleId)
                   .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region personRoleAssignment

        var personRoleAssignment = mb.Entity<PersonRoleAssignment>();
        personRoleAssignment.HasKey(p => new { p.PersonId, p.RoleId });

        #endregion
    }
}
