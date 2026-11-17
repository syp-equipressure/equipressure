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
    // Keycloak user ids of the test accounts sophie.grueneis and lena.grassauer (backend/keycloak/realm-equipressure.json)
    private const string SophieExternalId = "00000000-0000-4000-8000-000000000001";
    private const string LenaExternalId = "00000000-0000-4000-8000-000000000006";

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
        await AddAndSaveAsync(context, measurements.Select(m => m.Measurement).ToList());
        await AddAndSaveAsync(context, CreateMeasurementData(groups, measurements));

        await AddAndSaveAsync(context, CreateReleases(persons, groups));
        await AddAndSaveAsync(context, CreateChatMessages(persons, horses, groups));

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
                      string? description = null, string? externalId = null) => new()
                      {
                          ExternalId = externalId,
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
                           + "verstellen lässt. Spezialisiert auf Dressur- und Springsättel.",
                           SophieExternalId),
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
                           addresses[5], [RoleName.Equestrian], externalId: LenaExternalId),
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
        /* 8 */ new() { Name = "Quarter Horse" },
        // further breeds for the suggestions in the apps, not used by the horses below
        /* 9 */ new() { Name = "Oldenburger" },
        /* 10 */ new() { Name = "Österreichisches Warmblut" },
        /* 11 */ new() { Name = "Bayerisches Warmblut" },
        /* 12 */ new() { Name = "Württemberger" },
        /* 13 */ new() { Name = "Rheinländer" },
        /* 14 */ new() { Name = "Holländisches Warmblut (KWPN)" },
        /* 15 */ new() { Name = "Selle Français" },
        /* 16 */ new() { Name = "Irish Sport Horse" },
        /* 17 */ new() { Name = "Englisches Vollblut" },
        /* 18 */ new() { Name = "Arabisches Vollblut" },
        /* 19 */ new() { Name = "Shagya-Araber" },
        /* 20 */ new() { Name = "Traber" },
        /* 21 */ new() { Name = "Andalusier (PRE)" },
        /* 22 */ new() { Name = "Lusitano" },
        /* 23 */ new() { Name = "Friese" },
        /* 24 */ new() { Name = "Kladruber" },
        /* 25 */ new() { Name = "Knabstrupper" },
        /* 26 */ new() { Name = "Freiberger" },
        /* 27 */ new() { Name = "Fjordpferd" },
        /* 28 */ new() { Name = "Islandpferd" },
        /* 29 */ new() { Name = "Tinker" },
        /* 30 */ new() { Name = "Appaloosa" },
        /* 31 */ new() { Name = "Paint Horse" },
        /* 32 */ new() { Name = "Criollo" },
        /* 33 */ new() { Name = "Mustang" },
        /* 34 */ new() { Name = "Deutsches Reitpony" },
        /* 35 */ new() { Name = "Welsh Pony" },
        /* 36 */ new() { Name = "Connemara Pony" },
        /* 37 */ new() { Name = "Shetlandpony" },
        /* 38 */ new() { Name = "Süddeutsches Kaltblut" },
        /* 39 */ new() { Name = "Shire Horse" },
        /* 40 */ new() { Name = "Percheron" },
        /* 41 */ new() { Name = "Mischling" }
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
            /* 8 */ Create("Westernsattel Ranch", 8, 5),
            /* 9 */ Create("Freizeitsattel Haflinger Spezial", 5, 4),
            /* 10 */ Create("Vielseitigkeitssattel Breit", 6, 3),
            /* 11 */ Create("Barock-Dressursattel", 7, 1)
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
            /* 6 */ Create("Westernreiten Cash", new LocalDate(2026, 10, 10), 7, 8, 8, 2, "Ausritt im Gelände"),
            /* 7 */ Create("Springgymnastik Apollo", new LocalDate(2026, 10, 14), 6, 2, 3, 1, null),
            /* 8 */ Create("Trainingskontrolle Thunder", new LocalDate(2026, 10, 18), 8, 4, 7, 3,
                            "Druckbild nach drei Wochen stabil"),
            /* 9 */ Create("Ausritt Sternchen", new LocalDate(2026, 10, 8), 9, 5, 9, 5,
                            "Sattel drückt vorne am Widerrist"),
            /* 10 */ Create("Erste Messung Nora", new LocalDate(2026, 10, 12), 8, 6, 10, 3, null),
            /* 11 */ Create("Dressur Amadeus", new LocalDate(2026, 9, 25), 5, 7, 11, 6, "Ruhige Einheit"),
            /* 12 */ Create("Versammlung Amadeus", new LocalDate(2026, 10, 16), 5, 7, 11, 6,
                             "Mehr Druck hinten bei versammelten Lektionen")
        ];
    }

    // a measurement in the seed data together with how long it took (the duration results from the timestamps of
    // its recordings, the database has no extra column for it)
    private sealed record SeedMeasurement(Measurement Measurement, int DurationSeconds);

    private static SeedMeasurement[] CreateMeasurements(MeasurementGroup[] groups)
    {
        SeedMeasurement Create(int group, string pace, string hand, int durationSeconds,
                               string? description = null) =>
            new(new Measurement
            {
                Group = groups[group - 1],
                Pace = pace,
                Hand = hand,
                Description = description
            },
                durationSeconds);

        return
        [
            /* 1 */ Create(1, "Schritt", "Links", 120),
            /* 2 */ Create(1, "Schritt", "Rechts", 90),
            /* 3 */ Create(1, "Trab", "Links", 180, "Gleichmäßige Druckverteilung"),
            /* 4 */ Create(2, "Galopp", "Links", 60),
            /* 5 */ Create(2, "Galopp", "Rechts", 75, "Druckspitze am Widerrist"),
            /* 6 */ Create(3, "Schritt", "Links", 150),
            /* 7 */ Create(4, "Trab", "Rechts", 120),
            /* 8 */ Create(5, "Schritt", "Rechts", 105),
            /* 9 */ Create(6, "Schritt", "Links", 240),
            /* 10 */ Create(6, "Trab", "Links", 150),
            // added later, so the ids above stay the same
            /* 11 */ Create(1, "Trab", "Rechts", 165),
            /* 12 */ Create(2, "Trab", "Links", 90),
            /* 13 */ Create(3, "Trab", "Links", 105),
            /* 14 */ Create(3, "Trab", "Rechts", 96),
            /* 15 */ Create(4, "Schritt", "Links", 81),
            /* 16 */ Create(4, "Galopp", "Rechts", 54, "Sattel rutscht leicht nach rechts"),
            /* 17 */ Create(5, "Trab", "Rechts", 141),
            /* 18 */ Create(5, "Galopp", "Links", 69),
            /* 19 */ Create(7, "Trab", "Links", 111),
            /* 20 */ Create(7, "Galopp", "Links", 51),
            /* 21 */ Create(7, "Galopp", "Rechts", 45),
            /* 22 */ Create(8, "Schritt", "Rechts", 129),
            /* 23 */ Create(8, "Trab", "Rechts", 162),
            /* 24 */ Create(9, "Schritt", "Links", 210, "Gelände, leichter Anstieg"),
            /* 25 */ Create(9, "Trab", "Links", 120),
            /* 26 */ Create(10, "Schritt", "Links", 90),
            /* 27 */ Create(10, "Schritt", "Rechts", 87),
            /* 28 */ Create(11, "Schritt", "Rechts", 102),
            /* 29 */ Create(11, "Trab", "Links", 138),
            /* 30 */ Create(11, "Galopp", "Rechts", 63),
            /* 31 */ Create(12, "Trab", "Rechts", 120, "Versammelter Trab"),
            /* 32 */ Create(12, "Galopp", "Links", 60)
        ];
    }

    private const int PressureGridSize = 20;
    private const int SecondsBetweenFrames = 3;

    // how the saddle of a measurement group fits: pressure factors for the front, middle and back of the saddle and
    // the balance between the panels (positive = more pressure on the left panel)
    private static readonly (double Front, double Middle, double Back, double Balance)[] groupFits =
    [
        /* 1 */ (1.0, 1.0, 1.0, 0.0),
        /* 2 */ (1.0, 1.0, 1.25, 0.12), // slight pressure at the back left
        /* 3 */ (1.25, 0.7, 1.2, 0.0), // the saddle bridges, little pressure in the middle
        /* 4 */ (1.0, 1.0, 1.0, -0.04),
        /* 5 */ (1.15, 1.0, 0.9, -0.05),
        /* 6 */ (0.9, 1.05, 1.0, 0.0),
        /* 7 */ (1.05, 1.0, 1.1, 0.0),
        /* 8 */ (1.0, 1.0, 1.05, -0.03),
        /* 9 */ (1.4, 1.0, 0.8, 0.0), // the saddle is too narrow at the withers
        /* 10 */ (1.0, 0.95, 1.0, 0.08),
        /* 11 */ (1.0, 1.0, 1.0, 0.0),
        /* 12 */ (0.9, 1.0, 1.25, 0.0) // collected work shifts the weight to the back
    ];

    private static MeasurementData[] CreateMeasurementData(MeasurementGroup[] groups, SeedMeasurement[] measurements)
    {
        // the measurements of a group are recorded one after another with a short break in between
        var nextStartOfGroup = new Dictionary<MeasurementGroup, Instant>();
        List<MeasurementData> data = [];
        for (var index = 0; index < measurements.Length; index++)
        {
            var (measurement, durationSeconds) = measurements[index];
            var group = measurement.Group;
            var start = nextStartOfGroup.GetValueOrDefault(
                group, group.Date.AtStartOfDayInZone(DateTimeZone.Utc).PlusHours(14).ToInstant());
            nextStartOfGroup[group] = start.Plus(Duration.FromSeconds(durationSeconds + 60));

            var fit = groupFits[Array.IndexOf(groups, group)];
            var frameCount = durationSeconds / SecondsBetweenFrames + 1;
            for (var frame = 0; frame < frameCount; frame++)
            {
                data.Add(new MeasurementData
                {
                    Measurement = measurement,
                    Timestamp = start.Plus(Duration.FromSeconds(frame * SecondsBetweenFrames)),
                    Data = CreatePressureFrame(measurement, fit, index, frame, (double) frame / (frameCount - 1))
                });
            }
        }

        return data.ToArray();
    }

    /// <summary>
    ///     Creates a frame of the sensor mat as a JSON array with 20x20 values (in mbar) in row-major order, row 0 is the
    ///     front of the saddle. Like the live measurement of the app it has two panels left and right of the spine with
    ///     three pressure zones each. The pressure depends on the pace, the side of the hand carries a bit more weight and
    ///     the movement of the pace shifts the weight over time (left/right in trot, front/back in canter).
    /// </summary>
    /// <param name="measurement">The measurement the frame belongs to</param>
    /// <param name="fit">How the saddle of the measurement group fits</param>
    /// <param name="measurementIndex">The index of the measurement, makes the measurements differ a bit</param>
    /// <param name="frame">The number of the frame within the measurement</param>
    /// <param name="progress">How far the measurement has progressed (0 to 1)</param>
    private static string CreatePressureFrame(Measurement measurement,
                                              (double Front, double Middle, double Back, double Balance) fit,
                                              int measurementIndex, int frame, double progress)
    {
        var (peak, swayLeftRight, rockFrontBack, phaseStep) = measurement.Pace switch
        {
            "Galopp" => (340.0, 0.04, 0.16, 1.9),
            "Trab" => (260.0, 0.1, 0.05, 2.4),
            _ => (180.0, 0.04, 0.03, 1.3)
        };
        var phase = frame * phaseStep + measurementIndex;
        peak *= 1 + 0.03 * (measurementIndex % 4) + 0.03 * Math.Sin(phase * 0.37);

        var handBalance = measurement.Hand == "Links" ? 0.08 : -0.08;
        var balance = fit.Balance + handBalance + swayLeftRight * Math.Sin(phase);
        var leftFactor = 1 + balance;
        var rightFactor = 1 - balance;

        // rocking in canter, and towards the end of a measurement the rider sits a bit further back
        var rock = rockFrontBack * Math.Sin(phase);
        var frontFactor = fit.Front * (1 + rock);
        var backFactor = fit.Back * (1 - rock) * (1 + 0.08 * progress);

        const double LeftX = 6.0;
        const double RightX = 13.0;
        const double SpreadX = 2.2;
        (double Y, double SpreadY, double Strength)[] zones =
        [
            (4.0 + 0.3 * Math.Sin(phase), 2.5, 0.7 * frontFactor),
            (10.0, 3.5, 1.0 * fit.Middle),
            (16.0 - 0.3 * Math.Cos(phase), 2.5, 0.6 * backFactor)
        ];

        var values = new int[PressureGridSize * PressureGridSize];
        for (var row = 0; row < PressureGridSize; row++)
        {
            for (var col = 0; col < PressureGridSize; col++)
            {
                double left = 0;
                double right = 0;
                foreach (var zone in zones)
                {
                    var dy = (row - zone.Y) / zone.SpreadY;
                    var dxLeft = (col - LeftX) / SpreadX;
                    var dxRight = (col - RightX) / SpreadX;
                    left += zone.Strength * Math.Exp(-(dxLeft * dxLeft + dy * dy));
                    right += zone.Strength * Math.Exp(-(dxRight * dxRight + dy * dy));
                }

                values[row * PressureGridSize + col] =
                    (int) Math.Round(peak * (left * leftFactor + right * rightFactor));
            }
        }

        return $"[{string.Join(",", values)}]";
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

    private static ChatMessage[] CreateChatMessages(Person[] persons, Horse[] horses, MeasurementGroup[] groups)
    {
        ChatMessage Create(int sender, int recipient, Instant sentAt, string? text, bool read,
                           int? horse = null, int? group = null, Instant? acceptedAt = null) => new()
                           {
                               Type = horse is not null ? ChatMessageType.Horse
                : group is not null ? ChatMessageType.Measurement
                : ChatMessageType.Text,
                               Text = text,
                               SentAt = sentAt,
                               ReadAt = read ? sentAt.Plus(Duration.FromMinutes(5)) : null,
                               AcceptedAt = acceptedAt,
                               Sender = persons[sender - 1],
                               Recipient = persons[recipient - 1],
                               Horse = horse is null ? null : horses[horse.Value - 1],
                               MeasurementGroup = group is null ? null : groups[group.Value - 1]
                           };

        return
        [
            // Lena - Sophie: a shared measurement that Sophie already added, the last answer is unread
            /* 1 */ Create(6, 1, Instant.FromUtc(2026, 10, 20, 8, 30),
                           "Hallo Sophie, Luna drückt seit dem Springtraining links hinten etwas. Hättest du nächste "
                           + "Woche Zeit?", read: true),
            /* 2 */ Create(1, 6, Instant.FromUtc(2026, 10, 20, 9, 12),
                           "Hallo Lena! Gerne, schick mir die Messung, dann schaue ich sie mir vorher an.", read: true),
            /* 3 */ Create(6, 1, Instant.FromUtc(2026, 10, 20, 9, 20), "Hier ist die Messung vom Springtraining.",
                           read: true, group: 2, acceptedAt: Instant.FromUtc(2026, 10, 20, 10, 5)),
            /* 4 */ Create(1, 6, Instant.FromUtc(2026, 10, 20, 10, 7),
                           "Danke! Der Druck links hinten ist gut sichtbar. Donnerstag um 10 Uhr bei dir im Stall?",
                           read: false),
            // Lena - Anna: a shared horse that Anna has not added yet
            /* 5 */ Create(6, 3, Instant.FromUtc(2026, 10, 23, 17, 40), "Könntest du dir Apollo einmal anschauen?",
                           read: false, horse: 2),
            // Max - Sophie: an unread question
            /* 6 */ Create(7, 1, Instant.FromUtc(2026, 10, 22, 14, 0), "Hallo, wann hätten Sie wieder einen Termin frei?",
                           read: false)
        ];
    }
}