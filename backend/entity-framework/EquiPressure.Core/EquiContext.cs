using EquiPressure.Core.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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
        ConfigureRelease(modelBuilder);
        ConfigureSaddle(modelBuilder);
    }

    /// <summary>
    /// Configure the Person Objects
    /// An index for the person consisting of the firstname and the lastname exists
    /// the email of a person has to be unique
    /// Configures the association for PersonRelationship, PersonRoleAssignment and DeviceUser
    /// </summary>
    /// <param name="mb">
    /// the modelbuilder to be used
    /// </param>
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

        person.HasMany(p => p.Releases)
              .WithOne(r => r.Person)
              .HasForeignKey(r => r.PersonId)
              .OnDelete(DeleteBehavior.Cascade);

        // A device can always have just one owner
        person.HasMany(p => p.OwnerDevices)
              .WithOne(md => md.Owner)
              .HasForeignKey(md => md.OwnerId)
              .OnDelete(DeleteBehavior.Cascade);

        person.HasMany(p => p.UserDevices)
              .WithOne(du => du.User) // du = Assoziationstabelle für Person und Device als User
              .HasForeignKey(du => du.UserId)
              .OnDelete(DeleteBehavior.Cascade);

        person.HasMany(p => p.MeasurementGroups)
              .WithOne(mg => mg.Person)
              .HasForeignKey(mp => mp.PersonId)
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

        #region userDevice

        var userDevice = mb.Entity<DeviceUser>();
        userDevice.HasKey(du => new { du.DeviceId, du.UserId });

        #endregion
    }

    /// <summary>
    /// Configure the Horse Objects
    /// The Enum of the Horse Gender should be persisted as a string in the database
    /// Configures the association for the HorseBreed
    /// Configures the Breed Entities as well
    /// </summary>
    /// <param name="mb"></param>
    private static void ConfigureHorse(ModelBuilder mb)
    {
        #region horse

        var horse = mb.Entity<Horse>();
        horse.HasKey(h => h.Id);
        horse.Property(h => h.Id).ValueGeneratedOnAdd();
        horse.Property(h => h.Gender)
             .HasConversion(new EnumToStringConverter<HorseGender>());

        horse.HasMany(h => h.Persons)
             .WithOne(ph => ph.Horse)
             .HasForeignKey(ph => ph.HorseId)
             .OnDelete(DeleteBehavior.Cascade);

        horse.HasMany(h => h.HorseBreeds)
             .WithOne(hb => hb.Horse)
             .HasForeignKey(hb => hb.HorseId)
             .OnDelete(DeleteBehavior.Cascade);

        horse.HasMany(h => h.Saddles)
             .WithOne(s => s.Horse)
             .HasForeignKey(s => s.HorseId)
             .OnDelete(DeleteBehavior.Cascade);

        horse.HasMany(h => h.MeasurementGroups)
             .WithOne(mg => mg.Horse)
             .HasForeignKey(mg => mg.HorseId)
             .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region Breed

        var breed = mb.Entity<Breed>();
        breed.HasKey(b => b.Id);
        breed.Property(b => b.Id).ValueGeneratedOnAdd();

        breed.HasMany(b => b.HorseBreeds)
             .WithOne(hb => hb.Breed)
             .HasForeignKey(hb => hb.BreedId)
             .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region HorseBreed

        var horseBreed = mb.Entity<HorseBreed>();
        horseBreed.HasKey(hb => new { hb.HorseId, hb.BreedId });

        #endregion
    }

    /// <summary>
    /// Configure the AccountRole Objects
    /// Configures the association for the personRoleAssignment
    /// </summary>
    /// <param name="mb"></param>
    private static void ConfigureAccountRole(ModelBuilder mb)
    {
        #region accountRole

        var accountRole = mb.Entity<AccountRole>();
        accountRole.HasKey(ar => ar.Id);
        accountRole.Property(ar => ar.Id).ValueGeneratedOnAdd();

        accountRole.HasMany(ar => ar.RoleAssignments)
                   .WithOne(pra => pra.Role)
                   .HasForeignKey(ra => ra.RoleId)
                   .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region personRoleAssignment

        var personRoleAssignment = mb.Entity<PersonRoleAssignment>();
        personRoleAssignment.HasKey(pra => new { pra.PersonId, pra.RoleId });

        #endregion
    }

    /// <summary>
    /// Configure the Address and City Objects
    /// </summary>
    /// <param name="mb"></param>
    private static void ConfigureLocation(ModelBuilder mb)
    {
        #region address

        var address = mb.Entity<Address>();
        address.HasKey(a => a.Id);
        address.Property(a => a.Id).ValueGeneratedOnAdd();

        address.HasMany(a => a.Persons)
               .WithOne(p => p.Address)
               .HasForeignKey(a => a.AddressId)
               .OnDelete(DeleteBehavior.SetNull);

        address.HasMany(a => a.Horses)
               .WithOne(h => h.Address)
               .HasForeignKey(h => h.AddressId)
               .OnDelete(DeleteBehavior.SetNull);

        #endregion

        #region city

        var city = mb.Entity<City>();
        city.HasKey(c => c.Id);
        city.Property(c => c.Id).ValueGeneratedOnAdd();
        city.HasIndex(c => new { c.PLZ, c.Name });

        city.HasMany(c => c.Addresses)
            .WithOne(a => a.City)
            .HasForeignKey(a => a.CityId)
            .OnDelete(DeleteBehavior.SetNull);

        #endregion
    }

    /// <summary>
    /// Configures the MeasurementDevice Objects
    /// Configures the DeviceCategory as well
    /// </summary>
    /// <param name="mb"></param>
    private static void ConfigureDevice(ModelBuilder mb)
    {
        #region device

        var device = mb.Entity<MeasurementDevice>();
        device.HasKey(d => d.Id);
        device.Property(d => d.Id).ValueGeneratedOnAdd();

        device.HasMany(d => d.Users)
              .WithOne(du => du.Device)
              .HasForeignKey(du => du.DeviceId)
              .OnDelete(DeleteBehavior.Cascade);

        device.HasMany(d => d.MeasurementGroups)
              .WithOne(mg => mg.Device)
              .HasForeignKey(mg => mg.DeviceId)
              .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region deviceCategory

        var category = mb.Entity<DeviceCategory>();
        category.HasKey(c => c.Id);
        category.Property(c => c.Id).ValueGeneratedOnAdd();

        category.HasMany(c => c.Devices)
                .WithOne(d => d.Category)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

        #endregion
    }

    /// <summary>
    /// Configure the measurement classes: MeasurementGroup, MeasurementEligibility, Measurement, MeasurementData
    /// </summary>
    /// <param name="mb"></param>
    private static void ConfigureMeasurement(ModelBuilder mb)
    {
        #region measurementGroup

        var group = mb.Entity<MeasurementGroup>();
        group.HasKey(mg => mg.Id);
        group.Property(mg => mg.Id).ValueGeneratedOnAdd();

        group.HasMany(mg => mg.MeasurementEligibilities)
             .WithOne(me => me.MeasurementGroup)
             .HasForeignKey(me => me.MeasurementGroupId)
             .OnDelete(DeleteBehavior.Cascade);

        group.HasMany(mg => mg.Measurements)
             .WithOne(mg => mg.Group)
             .HasForeignKey(mg => mg.GroupId)
             .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region measurementEligibility

        var eligibility = mb.Entity<MeasurementEligibility>();
        eligibility.HasKey(me => new { me.MeasurementGroupId, me.ReleaseId });

        #endregion

        #region measurement

        var measurement = mb.Entity<Measurement>();
        measurement.HasKey(m => m.Id);
        measurement.Property(m => m.Id).ValueGeneratedOnAdd();

        measurement.HasMany(m => m.MeasurementDates)
                   .WithOne(m => m.Measurement)
                   .HasForeignKey(m => m.MeasurementId)
                   .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region measurementData

        var data = mb.Entity<MeasurementData>();
        data.HasKey(d => d.Id);
        data.Property(d => d.Id).ValueGeneratedOnAdd();

        #endregion
    }

    /// <summary>
    /// Configure the realese table
    /// </summary>
    /// <param name="mb"></param>
    private static void ConfigureRelease(ModelBuilder mb)
    {
        var release = mb.Entity<Release>();
        release.HasKey(r => r.Id);
        release.Property(r => r.Id).ValueGeneratedOnAdd();

        release.HasMany(r => r.MeasurementEligibilities)
               .WithOne(me => me.Release)
               .HasForeignKey(me => me.ReleaseId)
               .OnDelete(DeleteBehavior.Cascade);
    }

    /// <summary>
    /// Configure the context for the saddles and their categories
    /// </summary>
    /// <param name="mb"></param>
    private static void ConfigureSaddle(ModelBuilder mb)
    {
        #region saddle

        var saddle = mb.Entity<Saddle>();
        saddle.HasKey(s => s.Id);
        saddle.Property(s => s.Id).ValueGeneratedOnAdd();

        saddle.HasMany(s => s.MeasurementGroups)
              .WithOne(mg => mg.Saddle)
              .HasForeignKey(mg => mg.SaddleId)
              .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region saddleCategory

        var category = mb.Entity<SaddleCategory>();
        category.HasKey(c => c.Id);
        category.Property(c => c.Id).ValueGeneratedOnAdd();

        category.HasMany(c => c.Saddles)
                .WithOne(s => s.Category)
                .HasForeignKey(s => s.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

        #endregion
    }
}
