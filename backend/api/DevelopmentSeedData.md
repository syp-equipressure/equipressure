# Development test data

When the API starts in **Development**, it migrates the database and fills it with test data, but only if
the database has no persons yet. Existing data is never changed. The data is defined in
`EquiApi.Persistence/Util/DevelopmentSeedData.cs`.

- Turn it off: set `General:SeedDevelopmentData` to `false` in `appsettings.Development.json`.
- Get a fresh copy: drop the database (or the `EquiPressure` schema) and start the API again.
- The ids below are correct for a **fresh** database. If you add data before the seeder runs, they can differ.

## Test accounts

Sophie and Lena are linked to Keycloak users from `backend/keycloak/realm-equipressure.json`, so you can log in
as them (with `KC_LOGIN_THEME=equipressure-dev`) and e.g. chat with each other:

| Person | Username | Password | App |
|---|---|---|---|
| 1 Sophie Grüneis | `sophie.grueneis` | `sophie` | Sattler frontend |
| 6 Lena Grassauer | `lena.grassauer` | `lena` | Reiter app |

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
| 5 | Sternchen | Haflinger | 9 Tobias | | 9 |
| 6 | Nora | Noriker | 8 Julia | | 10 |
| 7 | Amadeus | Lipizzaner | 5 Lisa | | 11 |
| 8 | Cash | Quarter Horse | 7 Max | | 8 |

Breed ids 1–8: Hannoveraner, Holsteiner, Westfale, Trakehner, Haflinger, Noriker, Lipizzaner, Quarter Horse.
Ids 9–41 are further common breeds (Oldenburger … Mischling) for the suggestions in the apps, no horse uses them.
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

Every horse has 1–2 measurement groups, 32 measurements in total (gait `Schritt`/`Trab`/`Galopp`, hand
`Links`/`Rechts`) that last between 45 seconds and 4 minutes.

| Group | Horse | Rider | Saddle | Measurements (gait, hand, duration) |
|---|---|---|---|---|
| 1 Dressurtraining | 1 Luna | 6 Lena | 1 | Schritt L 2:00, Schritt R 1:30, Trab L 3:00, Trab R 2:45 |
| 2 Springtraining | 1 Luna | 6 Lena | 2 | Galopp L 1:00, Galopp R 1:15, Trab L 1:30 |
| 3 Erste Messung Bella | 3 Bella | 7 Max | 5 | Schritt L 2:30, Trab L 1:45, Trab R 1:36 |
| 4 Kontrolle nach Anpassung | 4 Thunder | 8 Julia | 7 | Trab R 2:00, Schritt L 1:21, Galopp R 0:54 |
| 5 Dressur Apollo | 2 Apollo | 6 Lena | 4 | Schritt R 1:45, Trab R 2:21, Galopp L 1:09 |
| 6 Westernreiten Cash | 8 Cash | 7 Max | 8 | Schritt L 4:00, Trab L 2:30 |
| 7 Springgymnastik Apollo | 2 Apollo | 6 Lena | 3 | Trab L 1:51, Galopp L 0:51, Galopp R 0:45 |
| 8 Trainingskontrolle Thunder | 4 Thunder | 8 Julia | 7 | Schritt R 2:09, Trab R 2:42 |
| 9 Ausritt Sternchen | 5 Sternchen | 9 Tobias | 9 | Schritt L 3:30, Trab L 2:00 |
| 10 Erste Messung Nora | 6 Nora | 8 Julia | 10 | Schritt L 1:30, Schritt R 1:27 |
| 11 Dressur Amadeus | 7 Amadeus | 5 Lisa | 11 | Schritt R 1:42, Trab L 2:18, Galopp R 1:03 |
| 12 Versammlung Amadeus | 7 Amadeus | 5 Lisa | 11 | Trab R 2:00, Galopp L 1:00 |

Measurement ids 1–10 are the first measurement(s) of the groups 1–6, the others follow in the order of the table
(see `DevelopmentSeedData.cs`). A measurement has a recording every 3 seconds, so its duration is the time between
the first and the last recording. Every recording is a full frame of the 20×20 sensor mat: a JSON array with 400
pressure values in mbar, row by row (row 0 is the front of the saddle). The pressure is higher in `Trab` and
`Galopp`, the side of the hand carries a bit more weight, the weight sways left/right in trot and rocks front/back in
canter, and every group has its own saddle fit (e.g. group 9: too narrow at the withers, group 3: the saddle bridges).
5 releases share groups with the saddlers 1–5 (`Release.Person` is the person the measurements are shared with).

6 chat messages: Lena (6) and Sophie (1) talk about the jumping measurement (message 3 shares group 2, Sophie already
added it, message 4 is unread), Lena shares her horse Apollo with Anna (3, not added yet) and Max (7) asks Sophie for an
appointment (unread).

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
GET http://localhost:5200/api/persons/6/chats            # conversations with Sophie and Anna
GET http://localhost:5200/api/persons/6/chats/1          # messages between Lena and Sophie
PUT http://localhost:5200/api/persons/3/chats/messages/5/accept   # Anna adds Apollo
ws://localhost:5200/api/persons/6/chats/ws                # live chat events of Lena
```
