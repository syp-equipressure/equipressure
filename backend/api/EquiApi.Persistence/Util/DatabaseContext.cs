using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EquiApi.Persistence.Util;

public sealed class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public const string SchemaName = "EquiPressure";

    // Person DbSets
    public DbSet<Person> Persons { get; set; }
    public DbSet<PersonRelationship> PersonRelationships { get; set; }
    public DbSet<AccountRole> AccountRoles { get; set; }
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(SchemaName);

        ConfigurePerson(modelBuilder);
        ConfigureHorse(modelBuilder);
        ConfigureAccountRole(modelBuilder);
        ConfigureLocation(modelBuilder);
        ConfigureDevice(modelBuilder);
        ConfigureMeasurement(modelBuilder);
        ConfigureRelease(modelBuilder);
        ConfigureSaddle(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Conventions.Remove<TableNameFromDbSetConvention>();
    }

    /// <summary>
    ///     Configures the person and its associations (PersonRelationship, PersonHorse and DeviceUser).
    ///     An index for the person consisting of the first name and the last name exists
    ///     and the email of a person has to be unique.
    /// </summary>
    /// <param name="mb">The model builder to use</param>
    private static void ConfigurePerson(ModelBuilder mb)
    {
        #region person

        var person = mb.Entity<Person>();

        person.HasKey(p => p.Id);
        person.Property(p => p.Id).ValueGeneratedOnAdd();
        person.HasIndex(p => new { p.FirstName, p.LastName });
        person.HasIndex(p => p.Email).IsUnique();
        person.HasIndex(p => p.ExternalId).IsUnique();
        person.HasIndex(p => p.DateOfBirth);

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

        // A device always has exactly one owner
        person.HasMany(p => p.OwnerDevices)
              .WithOne(md => md.Owner)
              .HasForeignKey(md => md.OwnerId)
              .OnDelete(DeleteBehavior.Cascade);

        person.HasMany(p => p.UserDevices)
              .WithOne(du => du.User)
              .HasForeignKey(du => du.UserId)
              .OnDelete(DeleteBehavior.Cascade);

        person.HasMany(p => p.MeasurementGroups)
              .WithOne(mg => mg.Person)
              .HasForeignKey(mg => mg.PersonId)
              .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region personRelation

        var personRelation = mb.Entity<PersonRelationship>();
        personRelation.HasKey(pr => new { pr.EquestrianId, pr.SaddlerId });

        // Self-referencing relationship: Person.Relationships contains the relationships where the person is the saddler
        personRelation.HasOne(pr => pr.Equestrian)
                      .WithMany()
                      .HasForeignKey(pr => pr.EquestrianId)
                      .OnDelete(DeleteBehavior.Cascade);

        personRelation.HasOne(pr => pr.Saddler)
                      .WithMany(p => p.Relationships)
                      .HasForeignKey(pr => pr.SaddlerId)
                      .OnDelete(DeleteBehavior.Restrict); // Restrict to avoid cascade cycles

        #endregion

        #region personHorse

        var personHorse = mb.Entity<PersonHorse>();
        personHorse.HasKey(ph => new { ph.PersonId, ph.HorseId });

        #endregion

        #region deviceUser

        var deviceUser = mb.Entity<DeviceUser>();
        deviceUser.HasKey(du => new { du.DeviceId, du.UserId });

        #endregion
    }

    /// <summary>
    ///     Configures the horse, its breeds and the HorseBreed association.
    ///     The gender of a horse is persisted as a string.
    /// </summary>
    /// <param name="mb">The model builder to use</param>
    private static void ConfigureHorse(ModelBuilder mb)
    {
        #region horse

        var horse = mb.Entity<Horse>();
        horse.HasKey(h => h.Id);
        horse.Property(h => h.Id).ValueGeneratedOnAdd();
        horse.Property(h => h.Gender)
             .HasConversion(new EnumToStringConverter<HorseGender>());
        horse.HasIndex(h => h.Name);

        horse.HasMany(h => h.Persons)
             .WithOne(ph => ph.Horse)
             .HasForeignKey(ph => ph.HorseId)
             .OnDelete(DeleteBehavior.Cascade);

        horse.HasMany(h => h.Saddles)
             .WithOne(s => s.Horse)
             .HasForeignKey(s => s.HorseId)
             .OnDelete(DeleteBehavior.Cascade);

        horse.HasMany(h => h.MeasurementGroups)
             .WithOne(mg => mg.Horse)
             .HasForeignKey(mg => mg.HorseId)
             .OnDelete(DeleteBehavior.Cascade);

        horse.HasMany(h => h.HorseBreeds)
             .WithOne(hb => hb.Horse)
             .HasForeignKey(hb => hb.HorseId)
             .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region horseBreed

        var horseBreed = mb.Entity<HorseBreed>();
        horseBreed.HasKey(hb => new { hb.HorseId, hb.BreedId });

        #endregion

        #region breed

        var breed = mb.Entity<Breed>();
        breed.HasKey(b => b.Id);
        breed.Property(b => b.Id).ValueGeneratedOnAdd();

        breed.HasMany(b => b.HorseBreeds)
             .WithOne(hb => hb.Breed)
             .HasForeignKey(hb => hb.BreedId)
             .OnDelete(DeleteBehavior.Cascade);

        #endregion
    }

    /// <summary>
    ///     Configures the account roles and the PersonRoleAssignment association.
    ///     The roles are fixed and seeded, the name of a role is persisted as a string and has to be unique.
    /// </summary>
    /// <param name="mb">The model builder to use</param>
    private static void ConfigureAccountRole(ModelBuilder mb)
    {
        #region accountRole

        var accountRole = mb.Entity<AccountRole>();
        accountRole.HasKey(ar => ar.Id);
        accountRole.Property(ar => ar.Id).ValueGeneratedOnAdd();
        accountRole.Property(ar => ar.Name)
                   .HasConversion(new EnumToStringConverter<RoleName>());
        accountRole.HasIndex(ar => ar.Name).IsUnique();

        accountRole.HasMany(ar => ar.RoleAssignments)
                   .WithOne(pra => pra.Role)
                   .HasForeignKey(pra => pra.RoleId)
                   .OnDelete(DeleteBehavior.Cascade);
        
        accountRole.HasData(new { Id = 1, Name = RoleName.Equestrian },
                            new { Id = 2, Name = RoleName.Saddler },
                            new { Id = 3, Name = RoleName.Guest });

        #endregion

        #region personRoleAssignment

        var personRoleAssignment = mb.Entity<PersonRoleAssignment>();
        personRoleAssignment.HasKey(pra => new { pra.PersonId, pra.RoleId });

        #endregion
    }

    /// <summary>
    ///     Configures the addresses.
    ///     An address cannot be deleted while a person or a horse still uses it.
    /// </summary>
    /// <param name="mb">The model builder to use</param>
    private static void ConfigureLocation(ModelBuilder mb)
    {
        #region address

        var address = mb.Entity<Address>();
        address.HasKey(a => a.Id);
        address.Property(a => a.Id).ValueGeneratedOnAdd();

        address.HasMany(a => a.Persons)
               .WithOne(p => p.Address)
               .HasForeignKey(p => p.AddressId)
               .OnDelete(DeleteBehavior.Restrict);

        address.HasMany(a => a.Horses)
               .WithOne(h => h.Address)
               .HasForeignKey(h => h.AddressId)
               .OnDelete(DeleteBehavior.Restrict);

        #endregion
    }

    /// <summary>
    ///     Configures the measurement devices and their categories.
    ///     The id of a device is its serial number, which is provided by the client.
    /// </summary>
    /// <param name="mb">The model builder to use</param>
    private static void ConfigureDevice(ModelBuilder mb)
    {
        #region device

        var device = mb.Entity<MeasurementDevice>();
        device.HasKey(d => d.Id);

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
        category.Property(c => c.Role)
                .HasConversion(new EnumToStringConverter<RoleName>())
                .HasDefaultValue(RoleName.Equestrian);

        category.HasMany(c => c.Devices)
                .WithOne(d => d.Category)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

        #endregion
    }

    /// <summary>
    ///     Configures the measurement classes: MeasurementGroup, MeasurementEligibility, Measurement, MeasurementData
    /// </summary>
    /// <param name="mb">The model builder to use</param>
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
             .WithOne(m => m.Group)
             .HasForeignKey(m => m.GroupId)
             .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region measurementEligibility

        var eligibility = mb.Entity<MeasurementEligibility>();
        eligibility.HasKey(me => new { me.MeasurementGroupId, me.ReleaseId });

        #endregion

        #region measurement

        var measurement = mb.Entity<Measurement>();
        measurement.HasKey(m => m.Id);
        measurement.HasIndex(m => m.GroupId);
        measurement.HasIndex(m => new { m.GroupId, m.Pace, m.Hand }).IsUnique();

        measurement.HasMany(m => m.MeasurementDates)
                   .WithOne(md => md.Measurement)
                   .HasForeignKey(md => md.MeasurementId)
                   .OnDelete(DeleteBehavior.Cascade);

        #endregion

        #region measurementData

        var data = mb.Entity<MeasurementData>();
        data.HasKey(d => d.Id);
        data.Property(d => d.Id).ValueGeneratedOnAdd();

        #endregion
    }

    /// <summary>
    ///     Configures the releases
    /// </summary>
    /// <param name="mb">The model builder to use</param>
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
    ///     Configures the saddles and their categories
    /// </summary>
    /// <param name="mb">The model builder to use</param>
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
