# Development test data

When the API starts in **Development**, it migrates the database and fills it with test data, but only if
the database has no persons yet. Existing data is never changed. The data is defined in
`EquiApi.Persistence/Util/DevelopmentSeedData.cs`.

- Turn it off: set `General:SeedDevelopmentData` to `false` in `appsettings.Development.json`.
- Get a fresh copy: drop the database (or the `EquiPressure` schema) and start the API again.
- The ids below are correct for a **fresh** database. If you add data before the seeder runs, they can differ.

## Persons

| Id | Name | Role | Address |
|---|---|---|---|
| 1 | Sophie Grüneis | Saddler | Lenaustraße 18, 4050 Traun |
| 2 | Ferdinand Feldhofer | Saddler | Hauptstraße 42, 4020 Linz |
| 3 | Anna Schmidt | Saddler (no website) | Reitweg 7, 4060 Leonding |
| 4 | Michael Weber | Saddler | Pferdestraße 15, 4070 Eferding |
| 5 | Lisa Hofmann | Saddler **and** Equestrian | Stallgasse 3, 4040 Linz-Urfahr |
| 6 | Lena Grassauer | Equestrian | Reiterhof Sonnenhügel 5, 4050 Traun |
| 7 | Max Mustermann | Equestrian | Pferdehof Grünwald 12, 4060 Leonding |
| 8 | Julia Brandstätter | Equestrian | 4020 Linz (no street) |
| 9 | Tobias Leitner | Equestrian | Reitsportzentrum Linz 1, 4020 Linz |
| 10 | Eva Moser | Guest | Stadtplatz 1, 4400 Steyr |

## Contacts and favourites

| Equestrian | Saddler | Contact | Favourite |
|---|---|---|---|
| 6 Lena | 1 Sophie | ✓ | ✓ |
| 6 Lena | 2 Ferdinand | | ✓ |
| 6 Lena | 3 Anna | ✓ | |
| 7 Max | 1 Sophie | ✓ | |
| 7 Max | 4 Michael | ✓ | ✓ |
| 8 Julia | 5 Lisa | ✓ | |
| 8 Julia | 1 Sophie | | ✓ |
| 9 Tobias | 2 Ferdinand | ✓ | ✓ |

## Horses

| Id | Name | Breed(s) | Owner | Other riders | Saddles (ids) |
|---|---|---|---|---|---|
| 1 | Luna | Hannoveraner | 6 Lena | 7 Max, 9 Tobias (hidden) | 1, 2 |
| 2 | Apollo | Holsteiner | 6 Lena | | 3, 4 |
| 3 | Bella | Westfale, Hannoveraner | 7 Max | 6 Lena | 5, 6 |
| 4 | Thunder | Trakehner | 8 Julia | | 7 |
| 5 | Sternchen | Haflinger | 9 Tobias | | |
| 6 | Nora | Noriker | 8 Julia | | |
| 7 | Amadeus | Lipizzaner | 5 Lisa | | |
| 8 | Cash | Quarter Horse | 7 Max | | 8 |

Breed ids 1–8: Hannoveraner, Holsteiner, Westfale, Trakehner, Haflinger, Noriker, Lipizzaner, Quarter Horse.
Saddle category ids 1–5: Dressur, Springen, Vielseitigkeit, Freizeit, Western.

## Devices

`NumOfAllowedPeople` is the number of users besides the owner. Category ids 1–5: Basic (1), Standard (3),
Family (5), Stable (10), Professional (20).

| Serial | Owner | Category | Users |
|---|---|---|---|
| EP0701 | 6 Lena | Standard | 7 Max, 9 Tobias |
| EP0702 | 7 Max | Basic | |
| EP0703 | 8 Julia | Basic | 9 Tobias (limit reached) |
| EP0704 | 1 Sophie | Professional | 5 Lisa, 3 Anna |
| EP0705 | 9 Tobias | Family | 8 Julia |
| EP0706 | 5 Lisa | Stable | |

## Measurements

6 measurement groups (Luna ×2, Bella, Thunder, Apollo, Cash) with 10 measurements (gait `Schritt`/`Trab`/`Galopp`,
hand `Links`/`Rechts`), each with one recording of pressure values. 5 releases share groups with the saddlers
1–5 (`Release.Person` is the person the measurements are shared with).

## Example requests

```http
GET http://localhost:5200/api/persons/1                  # saddler with full address
GET http://localhost:5200/api/persons/saddlers
GET http://localhost:5200/api/persons/6/favourites       # Sophie and Ferdinand
GET http://localhost:5200/api/persons/1/contacts         # Lena and Max (saddler side)
GET http://localhost:5200/api/persons/6/horses           # Apollo and Luna
GET http://localhost:5200/api/horses/1/riders            # Lena first, Max - Tobias is hidden
GET http://localhost:5200/api/persons/6/devices          # EP0701
POST http://localhost:5200/api/devices/EP0703/users/7    # 409, the limit of a Basic device is reached
PUT http://localhost:5200/api/persons/8/favourites/2     # Julia marks Ferdinand as favourite
PUT http://localhost:5200/api/persons/10/favourites/1    # 409, Eva is a guest
```
