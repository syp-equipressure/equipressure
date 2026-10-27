using EquiApi.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace EquiApi.Persistence.Util;

/// <summary>
///     Test data for manual testing during development (around Linz, matching the mock data of the Flutter app).
///     Never used in production. On a fresh database the generated ids follow the order in which the entities are
///     listed here (e.g. person 1 is Sophie Grüneis), see DevelopmentSeedData.md for an overview.
/// </summary>
public static class DevelopmentSeedData
{
    /// <summary>
    ///     Adds the test data if the database does not contain any persons yet, existing data is never touched
    /// </summary>
    /// <param name="context">The database context, the database has to be migrated already</param>
    /// <returns>True if the data was added, false if the database already contained data</returns>
    public static async ValueTask<bool> SeedAsync(DatabaseContext context)
    {
        if (await context.Persons.AnyAsync())
        {
            return false;
        }

        // every stage is saved on its own, so the ids are generated in the listed order
        var roles = await context.AccountRoles.ToDictionaryAsync(r => r.Name);

        var addresses = CreateAddresses();
        await AddAndSaveAsync(context, addresses);

        var persons = CreatePersons(addresses, roles);
        await AddAndSaveAsync(context, persons);
        await AddAndSaveAsync(context, CreateRelationships(persons));

        var breeds = CreateBreeds();
        await AddAndSaveAsync(context, breeds);

        var horses = CreateHorses(addresses, breeds);
        await AddAndSaveAsync(context, horses);
        await AddAndSaveAsync(context, CreatePersonHorses(persons, horses));

        var saddleCategories = CreateSaddleCategories();
        await AddAndSaveAsync(context, saddleCategories);

        var saddles = CreateSaddles(horses, saddleCategories);
        await AddAndSaveAsync(context, saddles);

        var deviceCategories = CreateDeviceCategories();
        await AddAndSaveAsync(context, deviceCategories);

        var devices = CreateDevices(persons, deviceCategories);
        await AddAndSaveAsync(context, devices);

        var groups = CreateMeasurementGroups(persons, horses, saddles, devices);
        await AddAndSaveAsync(context, groups);

        var measurements = CreateMeasurements(groups);
        await AddAndSaveAsync(context, measurements);
        await AddAndSaveAsync(context, CreateMeasurementData(measurements));

        await AddAndSaveAsync(context, CreateReleases(persons, groups));

        return true;
    }

    private static async ValueTask AddAndSaveAsync<T>(DatabaseContext context, IReadOnlyList<T> entities)
        where T : class
    {
        context.Set<T>().AddRange(entities);
        await context.SaveChangesAsync();
    }

    private static Address[] CreateAddresses() =>
    [
        /* 1 */ new() { AddressName = "Lenaustraße 18", PLZ = "4050", CityName = "Traun" },
        /* 2 */ new() { AddressName = "Hauptstraße 42", PLZ = "4020", CityName = "Linz" },
        /* 3 */ new() { AddressName = "Reitweg 7", PLZ = "4060", CityName = "Leonding" },
        /* 4 */ new() { AddressName = "Pferdestraße 15", PLZ = "4070", CityName = "Eferding" },
        /* 5 */ new() { AddressName = "Stallgasse 3", PLZ = "4040", CityName = "Linz-Urfahr" },
        /* 6 */ new() { AddressName = "Reiterhof Sonnenhügel 5", PLZ = "4050", CityName = "Traun" },
        /* 7 */ new() { AddressName = "Pferdehof Grünwald 12", PLZ = "4060", CityName = "Leonding" },
        /* 8 */ new() { AddressName = "Reitsportzentrum Linz 1", PLZ = "4020", CityName = "Linz" },
        /* 9 */ new() { AddressName = null, PLZ = "4020", CityName = "Linz" },
        /* 10 */ new() { AddressName = "Stadtplatz 1", PLZ = "4400", CityName = "Steyr" }
    ];

    private static Person[] CreatePersons(Address[] addresses, Dictionary<RoleName, AccountRole> roles)
    {
        Person Create(string firstName, string lastName, decimal height, decimal weight, LocalDate dateOfBirth,
                      string email, Address address, RoleName[] roleNames, string? websiteLink = null,
                      string? description = null) => new()
                      {
                          FirstName = firstName,
                          LastName = lastName,
                          Height = height,
                          Weight = weight,
                          DateOfBirth = dateOfBirth,
                          Email = email,
                          WebsiteLink = websiteLink,
                          Description = description,
                          Address = address,
                          Roles = roleNames.Select(name => new PersonRoleAssignment { Role = roles[name] }).ToList()
                      };

        return
        [
            /* 1 */ Create("Sophie", "Grüneis", 168M, 60M, new LocalDate(1985, 4, 12), "sophie.grueneis@example.com",
                           addresses[0], [RoleName.Saddler], "https://www.flexibler-sattel.at",
                           "Wir passen alle Sättel an, die gepolstert werden können und deren Kopfeisen sich kalt "
                           + "verstellen lässt. Spezialisiert auf Dressur- und Springsättel."),
            /* 2 */ Create("Ferdinand", "Feldhofer", 182M, 85M, new LocalDate(1968, 9, 3), "info@feldhofer.example.com",
                           addresses[1], [RoleName.Saddler], "https://feldhofer-sattlerei.at",
                           "Traditionelle Sattlerei seit 1985. Reparaturen, Neuanfertigungen und Sattelanpassungen "
                           + "für alle Reitdisziplinen."),
            /* 3 */ Create("Anna", "Schmidt", 165M, 58M, new LocalDate(1990, 1, 27), "anna.schmidt@example.com",
                           addresses[2], [RoleName.Saddler], null,
                           "Mobile Sattlerin für den Großraum Linz. Hausbesuche nach Terminvereinbarung."),
            /* 4 */ Create("Michael", "Weber", 178M, 80M, new LocalDate(1979, 11, 8), "m.weber@example.com",
                           addresses[3], [RoleName.Saddler], "https://weber-reitsport.at",
                           "Spezialist für Westernsättel und Barocksättel. Auch Zaumzeug und Lederreparaturen."),
            // also an equestrian, to test persons with several roles
            /* 5 */ Create("Lisa", "Hofmann", 170M, 62M, new LocalDate(1993, 6, 19), "lisa@hofmann.example.com",
                           addresses[4], [RoleName.Saddler, RoleName.Equestrian], "https://hofmann-sattel.at",
                           "Ergonomische Sattelanpassung mit modernster Druckmessung. Beratung für Pferd und Reiter."),
            /* 6 */ Create("Lena", "Grassauer", 167M, 55M, new LocalDate(2000, 3, 15), "lena.grassauer@example.com",
                           addresses[5], [RoleName.Equestrian]),
            /* 7 */ Create("Max", "Mustermann", 181M, 78M, new LocalDate(1997, 8, 2), "max.mustermann@example.com",
                           addresses[6], [RoleName.Equestrian]),
            // equestrian without street
            /* 8 */ Create("Julia", "Brandstätter", 172M, 61M, new LocalDate(2003, 12, 1), "julia.b@example.com",
                           addresses[8], [RoleName.Equestrian]),
            /* 9 */ Create("Tobias", "Leitner", 185M, 82M, new LocalDate(1995, 5, 23), "tobias.leitner@example.com",
                           addresses[7], [RoleName.Equestrian]),
            /* 10 */ Create("Eva", "Moser", 160M, 54M, new LocalDate(1988, 10, 30), "eva.moser@example.com",
                            addresses[9], [RoleName.Guest])
        ];
    }

    private static PersonRelationship[] CreateRelationships(Person[] persons)
    {
        PersonRelationship Create(int equestrian, int saddler, bool isContact, bool isFavourite) => new()
        {
            Equestrian = persons[equestrian - 1],
            Saddler = persons[saddler - 1],
            IsContact = isContact,
            IsFavourite = isFavourite
        };

        return
        [
            Create(6, 1, isContact: true, isFavourite: true), // Lena - Sophie
            Create(6, 2, isContact: false, isFavourite: true), // Lena - Ferdinand
            Create(6, 3, isContact: true, isFavourite: false), // Lena - Anna
            Create(7, 1, isContact: true, isFavourite: false), // Max - Sophie
            Create(7, 4, isContact: true, isFavourite: true), // Max - Michael
            Create(8, 5, isContact: true, isFavourite: false), // Julia - Lisa
            Create(8, 1, isContact: false, isFavourite: true), // Julia - Sophie
            Create(9, 2, isContact: true, isFavourite: true) // Tobias - Ferdinand
        ];
    }

    private static Breed[] CreateBreeds() =>
    [
        /* 1 */ new() { Name = "Hannoveraner" },
        /* 2 */ new() { Name = "Holsteiner" },
        /* 3 */ new() { Name = "Westfale" },
        /* 4 */ new() { Name = "Trakehner" },
        /* 5 */ new() { Name = "Haflinger" },
        /* 6 */ new() { Name = "Noriker" },
        /* 7 */ new() { Name = "Lipizzaner" },
        /* 8 */ new() { Name = "Quarter Horse" }
    ];

    private static Horse[] CreateHorses(Address[] addresses, Breed[] breeds)
    {
        Horse Create(string name, HorseGender gender, LocalDate dateOfBirth, decimal weight, decimal height,
                     Address address, params int[] breedIds) => new()
                     {
                         Name = name,
                         Gender = gender,
                         DateOfBirth = dateOfBirth,
                         Weight = weight,
                         Height = height,
                         Address = address,
                         HorseBreeds = breedIds.Select(id => new HorseBreed { Breed = breeds[id - 1] }).ToList()
                     };

        return
        [
            /* 1 */ Create("Luna", HorseGender.Female, new LocalDate(2017, 3, 15), 520M, 168M, addresses[5], 1),
            /* 2 */ Create("Apollo", HorseGender.Male, new LocalDate(2013, 7, 22), 580M, 172M, addresses[5], 2),
            // two breeds
            /* 3 */ Create("Bella", HorseGender.Female, new LocalDate(2020, 5, 10), 490M, 165M, addresses[6], 3, 1),
            /* 4 */ Create("Thunder", HorseGender.Male, new LocalDate(2015, 4, 2), 545M, 170M, addresses[7], 4),
            /* 5 */ Create("Sternchen", HorseGender.Female, new LocalDate(2011, 6, 30), 450M, 148M, addresses[9], 5),
            /* 6 */ Create("Nora", HorseGender.Female, new LocalDate(2016, 9, 12), 650M, 160M, addresses[6], 6),
            /* 7 */ Create("Amadeus", HorseGender.Male, new LocalDate(2012, 2, 18), 500M, 158M, addresses[4], 7),
            /* 8 */ Create("Cash", HorseGender.Male, new LocalDate(2018, 8, 8), 480M, 152M, addresses[5], 8)
        ];
    }

    private static PersonHorse[] CreatePersonHorses(Person[] persons, Horse[] horses)
    {
        PersonHorse Create(int person, int horse, bool isOwner, bool isHidden = false) => new()
        {
            Person = persons[person - 1],
            Horse = horses[horse - 1],
            IsOwner = isOwner,
            IsHidden = isHidden
        };

        return
        [
            Create(6, 1, isOwner: true), // Lena owns Luna
            Create(6, 2, isOwner: true), // Lena owns Apollo
            Create(7, 3, isOwner: true), // Max owns Bella
            Create(8, 4, isOwner: true), // Julia owns Thunder
            Create(9, 5, isOwner: true), // Tobias owns Sternchen
            Create(8, 6, isOwner: true), // Julia owns Nora
            Create(5, 7, isOwner: true), // Lisa owns Amadeus
            Create(7, 8, isOwner: true), // Max owns Cash
            Create(7, 1, isOwner: false), // Max rides Luna
            Create(9, 1, isOwner: false, isHidden: true), // Tobias rides Luna, hidden
            Create(6, 3, isOwner: false) // Lena rides Bella
        ];
    }

    private static SaddleCategory[] CreateSaddleCategories() =>
    [
        /* 1 */ new() { Name = "Dressur" },
        /* 2 */ new() { Name = "Springen" },
        /* 3 */ new() { Name = "Vielseitigkeit" },
        /* 4 */ new() { Name = "Freizeit" },
        /* 5 */ new() { Name = "Western" }
    ];

    private static Saddle[] CreateSaddles(Horse[] horses, SaddleCategory[] categories)
    {
        Saddle Create(string name, int horse, int category) => new()
        {
            Name = name,
            Horse = horses[horse - 1],
            Category = categories[category - 1]
        };

        return
        [
            /* 1 */ Create("Dressursattel Classic", 1, 1),
            /* 2 */ Create("Springsattel Pro", 1, 2),
            /* 3 */ Create("Vielseitigkeitssattel Comfort", 2, 3),
            /* 4 */ Create("Dressursattel Premium", 2, 1),
            /* 5 */ Create("Springsattel Light", 3, 2),
            /* 6 */ Create("Freizeitsattel Soft", 3, 4),
            /* 7 */ Create("Dressursattel WINTEC 500", 4, 1),
            /* 8 */ Create("Westernsattel Ranch", 8, 5)
        ];
    }

    // NumOfAllowedPeople is the number of users besides the owner
    private static DeviceCategory[] CreateDeviceCategories() =>
    [
        /* 1 */ new() { Name = "Basic", NumOfAllowedPeople = 1 },
        /* 2 */ new() { Name = "Standard", NumOfAllowedPeople = 3 },
        /* 3 */ new() { Name = "Family", NumOfAllowedPeople = 5 },
        /* 4 */ new() { Name = "Stable", NumOfAllowedPeople = 10 },
        /* 5 */ new() { Name = "Professional", NumOfAllowedPeople = 20, Role = RoleName.Saddler }
    ];

    private static MeasurementDevice[] CreateDevices(Person[] persons, DeviceCategory[] categories)
    {
        MeasurementDevice Create(string id, int owner, int category, params int[] users) => new()
        {
            Id = id,
            Owner = persons[owner - 1],
            Category = categories[category - 1],
            Users = users.Select(user => new DeviceUser { DeviceId = id, User = persons[user - 1] }).ToList()
        };

        return
        [
            Create("EP0701", 6, 2, 7, 9), // Lena, users Max and Tobias
            Create("EP0702", 7, 1), // Max, no users
            Create("EP0703", 8, 1, 9), // Julia, user Tobias - the user limit is reached
            Create("EP0704", 1, 5, 5, 3), // Sophie, users Lisa and Anna
            Create("EP0705", 9, 3, 8), // Tobias, user Julia
            Create("EP0706", 5, 4) // Lisa, no users
        ];
    }

    private static MeasurementGroup[] CreateMeasurementGroups(Person[] persons, Horse[] horses, Saddle[] saddles,
                                                              MeasurementDevice[] devices)
    {
        MeasurementGroup Create(string name, LocalDate date, int person, int horse, int saddle, int device,
                                string? notes) => new()
                                {
                                    Name = name,
                                    Date = date,
                                    Notes = notes,
                                    Person = persons[person - 1],
                                    Horse = horses[horse - 1],
                                    Saddle = saddles[saddle - 1],
                                    DeviceId = devices[device - 1].Id,
                                    Device = devices[device - 1]
                                };

        return
        [
            /* 1 */ Create("Dressurtraining", new LocalDate(2026, 9, 2), 6, 1, 1, 1, "Sehr ausbalanciert"),
            /* 2 */ Create("Springtraining", new LocalDate(2026, 9, 15), 6, 1, 2, 1, "Leichter Druck links hinten"),
            /* 3 */ Create("Erste Messung Bella", new LocalDate(2026, 9, 20), 7, 3, 5, 2, null),
            /* 4 */ Create("Kontrolle nach Anpassung", new LocalDate(2026, 10, 1), 8, 4, 7, 3,
                            "Sattel wurde von Ferdinand angepasst"),
            /* 5 */ Create("Dressur Apollo", new LocalDate(2026, 10, 5), 6, 2, 4, 1, null),
            /* 6 */ Create("Westernreiten Cash", new LocalDate(2026, 10, 10), 7, 8, 8, 2, "Ausritt im Gelände")
        ];
    }

    private static Measurement[] CreateMeasurements(MeasurementGroup[] groups)
    {
        Measurement Create(int group, string pace, string hand, string? description = null) => new()
        {
            Group = groups[group - 1],
            Pace = pace,
            Hand = hand,
            Description = description
        };

        return
        [
            /* 1 */ Create(1, "Schritt", "Links"),
            /* 2 */ Create(1, "Schritt", "Rechts"),
            /* 3 */ Create(1, "Trab", "Links", "Gleichmäßige Druckverteilung"),
            /* 4 */ Create(2, "Galopp", "Links"),
            /* 5 */ Create(2, "Galopp", "Rechts", "Druckspitze am Widerrist"),
            /* 6 */ Create(3, "Schritt", "Links"),
            /* 7 */ Create(4, "Trab", "Rechts"),
            /* 8 */ Create(5, "Schritt", "Rechts"),
            /* 9 */ Create(6, "Schritt", "Links"),
            /* 10 */ Create(6, "Trab", "Links")
        ];
    }

    private static MeasurementData[] CreateMeasurementData(Measurement[] measurements)
    {
        // one recording per measurement: a pressure curve (in mbar) like the mock data of the app
        return measurements.Select((measurement, index) => new MeasurementData
        {
            Measurement = measurement,
            Timestamp = measurement.Group.Date.AtStartOfDayInZone(DateTimeZone.Utc)
                                                      .PlusHours(14)
                                                      .Plus(Duration.FromMinutes(5 * index))
                                                      .ToInstant(),
            Data = CreatePressureCurve(200 + 80 * index)
        })
                           .ToArray();
    }

    private static string CreatePressureCurve(int peak)
    {
        int[] shape = [0, 2, 12, 51, 149, 272, 354, 268, 149, 61, 46, 91, 160, 211, 168, 85, 33, 8, 1, 0];
        const int ShapeMaximum = 354;

        return $"[{string.Join(",", shape.Select(value => value * peak / ShapeMaximum))}]";
    }

    // a release shares measurement groups with a person (e.g. a saddler who should look at them)
    private static Release[] CreateReleases(Person[] persons, MeasurementGroup[] groups)
    {
        Release Create(int person, Instant timestamp, params int[] groupNumbers) => new()
        {
            Person = persons[person - 1],
            ReleaseTimestamp = timestamp,
            MeasurementEligibilities = groupNumbers.Select(group => new MeasurementEligibility
            {
                MeasurementGroup = groups[group - 1]
            })
                                                   .ToList()
        };

        return
        [
            Create(1, Instant.FromUtc(2026, 9, 16, 9, 0), 1, 2), // Lena's Luna measurements for Sophie
            Create(2, Instant.FromUtc(2026, 10, 6, 10, 30), 5), // Lena's Apollo measurement for Ferdinand
            Create(4, Instant.FromUtc(2026, 10, 11, 8, 15), 3, 6), // Max's measurements for Michael
            Create(5, Instant.FromUtc(2026, 10, 2, 17, 45), 4), // Julia's Thunder measurement for Lisa
            Create(3, Instant.FromUtc(2026, 9, 17, 12, 0), 2) // Lena's jumping measurement for Anna
        ];
    }
}