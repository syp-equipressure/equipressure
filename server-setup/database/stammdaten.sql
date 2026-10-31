-- Stammdaten für den Betrieb, übernommen aus DevelopmentSeedData.cs (ohne Testpersonen).
-- Mehrfaches Ausführen ist unbedenklich, vorhandene Einträge bleiben unverändert.
BEGIN;

INSERT INTO "EquiPressure"."Breed" ("Name")
SELECT v.name
FROM (VALUES
    ('Hannoveraner'), ('Holsteiner'), ('Westfale'), ('Trakehner'), ('Haflinger'), ('Noriker'),
    ('Lipizzaner'), ('Quarter Horse'), ('Oldenburger'), ('Österreichisches Warmblut'),
    ('Bayerisches Warmblut'), ('Württemberger'), ('Rheinländer'), ('Holländisches Warmblut (KWPN)'),
    ('Selle Français'), ('Irish Sport Horse'), ('Englisches Vollblut'), ('Arabisches Vollblut'),
    ('Shagya-Araber'), ('Traber'), ('Andalusier (PRE)'), ('Lusitano'), ('Friese'), ('Kladruber'),
    ('Knabstrupper'), ('Freiberger'), ('Fjordpferd'), ('Islandpferd'), ('Tinker'), ('Appaloosa'),
    ('Paint Horse'), ('Criollo'), ('Mustang'), ('Deutsches Reitpony'), ('Welsh Pony'),
    ('Connemara Pony'), ('Shetlandpony'), ('Süddeutsches Kaltblut'), ('Shire Horse'),
    ('Percheron'), ('Mischling')
) AS v(name)
WHERE NOT EXISTS (SELECT 1 FROM "EquiPressure"."Breed" b WHERE b."Name" = v.name);

INSERT INTO "EquiPressure"."SaddleCategory" ("Name")
SELECT v.name
FROM (VALUES ('Dressur'), ('Springen'), ('Vielseitigkeit'), ('Freizeit'), ('Western')) AS v(name)
WHERE NOT EXISTS (SELECT 1 FROM "EquiPressure"."SaddleCategory" c WHERE c."Name" = v.name);

INSERT INTO "EquiPressure"."DeviceCategory" ("Name", "NumOfAllowedPeople", "Role")
SELECT v.name, v.people, v.role
FROM (VALUES
    ('Basic', 1, 'Equestrian'),
    ('Standard', 3, 'Equestrian'),
    ('Family', 5, 'Equestrian'),
    ('Stable', 10, 'Equestrian'),
    ('Professional', 20, 'Saddler')
) AS v(name, people, role)
WHERE NOT EXISTS (SELECT 1 FROM "EquiPressure"."DeviceCategory" c WHERE c."Name" = v.name);

COMMIT;
