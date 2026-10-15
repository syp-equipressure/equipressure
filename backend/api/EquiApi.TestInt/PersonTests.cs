using System.Net;
using EquiApi.Dtos;
using EquiApi.Persistence.Model;
using EquiApi.TestInt.Util;

namespace EquiApi.TestInt;

public sealed class PersonTests(WebApiTestFixture webApiFixture) : WebApiTestBase(webApiFixture)
{
    [Fact]
    public async ValueTask GetPersonById_ExistingPerson_ReturnsPersonWithAddressAndRoles()
    {
        var address = TestData.CreateAddress("Hauptstraße 1");
        var person = TestData.CreatePerson(email: "max@reiter.at", address: address);
        await SeedAsync(person);

        var response = await ApiClient.GetAsync($"api/persons/{person.Id}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<PersonDetailDto>(response);
        content.Id.Should().Be(person.Id);
        content.FirstName.Should().Be(person.FirstName);
        content.LastName.Should().Be(person.LastName);
        content.DateOfBirth.Should().Be(person.DateOfBirth);
        content.Height.Should().Be(person.Height);
        content.Weight.Should().Be(person.Weight);
        content.Email.Should().Be(person.Email);
        content.Address.Should().Be(new AddressDto(address.Id, "Hauptstraße 1", "Steyr", "4400"));
        content.Roles.Should().Equal(RoleName.Equestrian);
    }

    [Fact]
    public async ValueTask GetPersonById_NonExistingPerson_ReturnsNotFound()
    {
        var response = await ApiClient.GetAsync($"api/persons/{TestData.NonExistingId}", TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async ValueTask GetPersonById_InvalidId_ReturnsBadRequest(int invalidId)
    {
        var response = await ApiClient.GetAsync($"api/persons/{invalidId}", TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask GetProfileData_ExistingPerson_ReturnsName()
    {
        var person = TestData.CreatePerson("Anna", "Huber");
        await SeedAsync(person);

        var response = await ApiClient.GetAsync($"api/persons/{person.Id}/profile-data", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<PersonNameDto>(response);
        content.Should().Be(new PersonNameDto("Anna", "Huber"));
    }

    [Fact]
    public async ValueTask GetProfileData_NonExistingPerson_ReturnsNotFound()
    {
        var response = await ApiClient.GetAsync($"api/persons/{TestData.NonExistingId}/profile-data",
                                                TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetAddress_ExistingPerson_ReturnsAddress()
    {
        var address = TestData.CreateAddress("Hauptstraße 1", "4020", "Linz");
        var person = TestData.CreatePerson(address: address);
        await SeedAsync(person);

        var response = await ApiClient.GetAsync($"api/persons/{person.Id}/location", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<AddressDto>(response);
        content.Should().Be(new AddressDto(address.Id, "Hauptstraße 1", "Linz", "4020"));
    }

    [Fact]
    public async ValueTask GetAddress_NonExistingPerson_ReturnsNotFound()
    {
        var response = await ApiClient.GetAsync($"api/persons/{TestData.NonExistingId}/location",
                                                TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask UpdateAddress_NewAddress_ChangesAddressOfPerson()
    {
        var person = TestData.CreatePerson(address: TestData.CreateAddress("Stadtplatz 1", "4400", "Steyr"));
        await SeedAsync(person);
        var request = TestData.CreateAddressRequest("Lenaustraße 18", "Traun", "4050");

        var response = await ApiClient.PutAsJsonAsync($"api/persons/{person.Id}/location", request, JsonOptions,
                                                      TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<AddressDto>(response);
        content.Should().Be(new AddressDto(content.Id, "Lenaustraße 18", "Traun", "4050"));
        var getResponse = await ApiClient.GetAsync($"api/persons/{person.Id}/location", TestCancellationToken);
        var storedAddress = await ReadContentAsync<AddressDto>(getResponse);
        storedAddress.Should().Be(content);
    }

    [Fact]
    public async ValueTask UpdateAddress_AddressExists_ReusesAddress()
    {
        var existingAddress = TestData.CreateAddress("Lenaustraße 18", "4050", "Traun");
        var person = TestData.CreatePerson();
        await SeedAsync(person, existingAddress);
        var request = TestData.CreateAddressRequest("lenaustraße 18", "TRAUN", "4050");

        var response = await ApiClient.PutAsJsonAsync($"api/persons/{person.Id}/location", request, JsonOptions,
                                                      TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<AddressDto>(response);
        content.Id.Should().Be(existingAddress.Id);
    }

    [Fact]
    public async ValueTask UpdateAddress_NonExistingPerson_ReturnsNotFound()
    {
        var request = TestData.CreateAddressRequest("Lenaustraße 18", "Traun", "4050");

        var response = await ApiClient.PutAsJsonAsync($"api/persons/{TestData.NonExistingId}/location", request,
                                                      JsonOptions, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask UpdateAddress_InvalidAddress_ReturnsBadRequest()
    {
        var person = TestData.CreatePerson();
        await SeedAsync(person);
        var request = TestData.CreateAddressRequest(null, "", "4050");

        var response = await ApiClient.PutAsJsonAsync($"api/persons/{person.Id}/location", request, JsonOptions,
                                                      TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask GetContacts_EquestrianWithRelationships_ReturnsOnlyOwnContacts()
    {
        var equestrian = TestData.CreatePerson("Max");
        var otherEquestrian = TestData.CreatePerson("Moritz");
        var contact = TestData.CreatePerson("Sabine", "Sattler", role: RoleName.Saddler);
        var favouriteOnly = TestData.CreatePerson("Susi", "Sattel", role: RoleName.Saddler);
        var contactOfOther = TestData.CreatePerson("Sepp", "Sattlerei", role: RoleName.Saddler);
        await SeedAsync(TestData.CreateRelationship(equestrian, contact, isContact: true),
                        TestData.CreateRelationship(equestrian, favouriteOnly, isFavourite: true),
                        TestData.CreateRelationship(otherEquestrian, contactOfOther, isContact: true));

        var response = await ApiClient.GetAsync($"api/persons/{equestrian.Id}/contacts", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<ContactListResponse>(response);
        var saddler = content.Contacts.Should().ContainSingle().Subject;
        saddler.Id.Should().Be(contact.Id);
        saddler.WebsiteLink.Should().Be(contact.WebsiteLink);
    }

    [Fact]
    public async ValueTask GetContacts_SaddlerWithRelationships_ReturnsOnlyOwnEquestrianContacts()
    {
        var saddler = TestData.CreatePerson("Sabine", "Sattler", role: RoleName.Saddler);
        var otherSaddler = TestData.CreatePerson("Sepp", "Sattlerei", role: RoleName.Saddler);
        var firstContact = TestData.CreatePerson("Max", "Mustermann");
        var secondContact = TestData.CreatePerson("Anna", "Auer");
        var favouriteOnly = TestData.CreatePerson("Moritz", "Maier");
        var contactOfOther = TestData.CreatePerson("Berta", "Berger");
        await SeedAsync(TestData.CreateRelationship(firstContact, saddler, isContact: true),
                        TestData.CreateRelationship(secondContact, saddler, isContact: true, isFavourite: true),
                        TestData.CreateRelationship(favouriteOnly, saddler, isFavourite: true),
                        TestData.CreateRelationship(contactOfOther, otherSaddler, isContact: true));

        var response = await ApiClient.GetAsync($"api/persons/{saddler.Id}/contacts", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<ContactListResponse>(response);
        content.Contacts.Select(c => c.Id).Should().Equal(secondContact.Id, firstContact.Id);
        content.Contacts.Should().AllSatisfy(c => c.WebsiteLink.Should().BeNull());
    }

    [Fact]
    public async ValueTask GetContacts_NoContacts_ReturnsEmptyList()
    {
        var person = TestData.CreatePerson();
        await SeedAsync(person);

        var response = await ApiClient.GetAsync($"api/persons/{person.Id}/contacts", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<ContactListResponse>(response);
        content.Contacts.Should().BeEmpty();
    }

    [Fact]
    public async ValueTask GetContacts_NonExistingPerson_ReturnsNotFound()
    {
        var response = await ApiClient.GetAsync($"api/persons/{TestData.NonExistingId}/contacts",
                                                TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetFavourites_EquestrianWithRelationships_ReturnsOnlyOwnFavourites()
    {
        var equestrian = TestData.CreatePerson("Max");
        var otherEquestrian = TestData.CreatePerson("Moritz");
        var favourite = TestData.CreatePerson("Sabine", "Sattler", role: RoleName.Saddler);
        var contactOnly = TestData.CreatePerson("Susi", "Sattel", role: RoleName.Saddler);
        var favouriteOfOther = TestData.CreatePerson("Sepp", "Sattlerei", role: RoleName.Saddler);
        await SeedAsync(TestData.CreateRelationship(equestrian, favourite, isFavourite: true, isContact: true),
                        TestData.CreateRelationship(equestrian, contactOnly, isContact: true),
                        TestData.CreateRelationship(otherEquestrian, favouriteOfOther, isFavourite: true));

        var response = await ApiClient.GetAsync($"api/persons/{equestrian.Id}/favourites", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<SaddlerListResponse>(response);
        var saddler = content.Saddlers.Should().ContainSingle().Subject;
        saddler.Id.Should().Be(favourite.Id);
        saddler.WebsiteLink.Should().Be(favourite.WebsiteLink);
        saddler.CityName.Should().Be(favourite.Address.CityName);
    }

    [Fact]
    public async ValueTask GetFavourites_NonExistingPerson_ReturnsNotFound()
    {
        var response = await ApiClient.GetAsync($"api/persons/{TestData.NonExistingId}/favourites",
                                                TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetHorses_PersonWithHorses_ReturnsOnlyOwnedHorsesWithAddress()
    {
        var person = TestData.CreatePerson();
        var stable = TestData.CreateAddress("Stallweg 3", "4050", "Traun");
        var ownedHorse = TestData.CreateHorse("Blitz", stable, TestData.CreateBreed());
        var riddenHorse = TestData.CreateHorse("Donner");
        await SeedAsync(TestData.CreatePersonHorse(person, ownedHorse, isOwner: true),
                        TestData.CreatePersonHorse(person, riddenHorse));

        var response = await ApiClient.GetAsync($"api/persons/{person.Id}/horses", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<HorseListResponse>(response);
        var horse = content.Horses.Should().ContainSingle().Subject;
        horse.Id.Should().Be(ownedHorse.Id);
        horse.BreedNames.Should().Equal("Haflinger");
        horse.Address.Should().Be(new AddressDto(stable.Id, "Stallweg 3", "Traun", "4050"));
    }

    [Fact]
    public async ValueTask GetHorses_NoHorses_ReturnsEmptyList()
    {
        var person = TestData.CreatePerson();
        await SeedAsync(person);

        var response = await ApiClient.GetAsync($"api/persons/{person.Id}/horses", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<HorseListResponse>(response);
        content.Horses.Should().BeEmpty();
    }

    [Fact]
    public async ValueTask GetHorses_NonExistingPerson_ReturnsNotFound()
    {
        var response = await ApiClient.GetAsync($"api/persons/{TestData.NonExistingId}/horses",
                                                TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetDevices_PersonWithDevices_ReturnsOwnedAndUsedDevices()
    {
        var person = TestData.CreatePerson("Max");
        var otherPerson = TestData.CreatePerson("Moritz");
        var category = TestData.CreateDeviceCategory();
        await SeedAsync(TestData.CreateDevice("owned-device", person, category),
                        TestData.CreateDevice("used-device", otherPerson, category, person),
                        TestData.CreateDevice("other-device", otherPerson, category));

        var response = await ApiClient.GetAsync($"api/persons/{person.Id}/devices", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<DeviceListResponse>(response);
        content.Devices.Select(d => d.Id).Should().BeEquivalentTo(["owned-device", "used-device"]);
        var usedDevice = content.Devices.Single(d => d.Id == "used-device");
        usedDevice.Owner.Id.Should().Be(otherPerson.Id);
        usedDevice.Users.Should().ContainSingle().Which.Id.Should().Be(person.Id);
    }

    [Fact]
    public async ValueTask GetDevices_NoDevices_ReturnsEmptyList()
    {
        var person = TestData.CreatePerson();
        await SeedAsync(person);

        var response = await ApiClient.GetAsync($"api/persons/{person.Id}/devices", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<DeviceListResponse>(response);
        content.Devices.Should().BeEmpty();
    }

    [Fact]
    public async ValueTask GetDevices_NonExistingPerson_ReturnsNotFound()
    {
        var response = await ApiClient.GetAsync($"api/persons/{TestData.NonExistingId}/devices",
                                                TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask GetSaddlers_SaddlersAndEquestrians_ReturnsOnlySaddlersOrderedByLastName()
    {
        await SeedAsync(TestData.CreatePerson("Max"),
                        TestData.CreatePerson("Sepp", "Zimmermann", role: RoleName.Saddler),
                        TestData.CreatePerson("Sabine", "Auer", role: RoleName.Saddler));

        var response = await ApiClient.GetAsync("api/persons/saddlers", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await ReadContentAsync<SaddlerListResponse>(response);
        content.Saddlers.Select(s => s.LastName).Should().Equal("Auer", "Zimmermann");
    }

    [Fact]
    public async ValueTask CreatePerson_ValidEquestrian_ReturnsCreatedPerson()
    {
        var request = TestData.CreateAddPersonRequest(email: "anna@reiter.at");

        var response = await ApiClient.PostAsJsonAsync("api/persons", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await ReadContentAsync<PersonDetailDto>(response);
        content.FirstName.Should().Be(request.FirstName);
        content.Email.Should().Be(request.Email);
        content.Address.CityName.Should().Be(request.Address.CityName);
        content.Roles.Should().Equal(RoleName.Equestrian);

        response.Headers.Location.Should().NotBeNull();
        var getResponse = await ApiClient.GetAsync(response.Headers.Location, TestCancellationToken);
        var storedPerson = await ReadContentAsync<PersonDetailDto>(getResponse);
        storedPerson.Should().BeEquivalentTo(content);
    }

    [Fact]
    public async ValueTask CreatePerson_ValidSaddlerWithWebsite_ReturnsCreatedPerson()
    {
        var request = TestData.CreateAddPersonRequest(role: RoleName.Saddler,
                                                      websiteLink: "https://sattler.example.com");

        var response = await ApiClient.PostAsJsonAsync("api/persons", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await ReadContentAsync<PersonDetailDto>(response);
        content.WebsiteLink.Should().Be(request.WebsiteLink);
        content.Roles.Should().Equal(RoleName.Saddler);
    }

    [Fact]
    public async ValueTask CreatePerson_AddressExists_ReusesAddress()
    {
        var address = TestData.CreateAddress("Hauptstraße 1");
        await SeedAsync(address);
        // city name and address name are compared case-insensitive
        var request = TestData.CreateAddPersonRequest(address: TestData.CreateAddressRequest("hauptstraße 1", "steyr"));

        var response = await ApiClient.PostAsJsonAsync("api/persons", request, JsonOptions, TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await ReadContentAsync<PersonDetailDto>(response);
        content.Address.Id.Should().Be(address.Id);
    }

    [Fact]
    public async ValueTask CreatePerson_InvalidData_ReturnsBadRequest()
    {
        var request = TestData.CreateAddPersonRequest(firstName: "");

        var response = await ApiClient.PostAsJsonAsync("api/persons", request, JsonOptions, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask CreatePerson_EquestrianWithWebsite_ReturnsBadRequest()
    {
        var request = TestData.CreateAddPersonRequest(websiteLink: "https://example.com");

        var response = await ApiClient.PostAsJsonAsync("api/persons", request, JsonOptions, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask CreatePerson_DuplicateEmail_ReturnsConflict()
    {
        const string DuplicateEmail = "duplicate@reiter.at";
        await SeedAsync(TestData.CreatePerson(email: DuplicateEmail));
        var request = TestData.CreateAddPersonRequest(email: DuplicateEmail);

        var response = await ApiClient.PostAsJsonAsync("api/persons", request, JsonOptions, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask UpdatePerson_OnlySomeValues_ChangesOnlyTheseValues()
    {
        var person = TestData.CreatePerson("Max", "Mustermann", "max@reiter.at");
        await SeedAsync(person);
        var request = new UpdatePersonRequest(null, "Musterfrau", 185M, null, null, null, null, null, null, null);

        var response = await ApiClient.PatchAsJsonAsync($"api/persons/{person.Id}", request, JsonOptions,
                                                        TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var getResponse = await ApiClient.GetAsync($"api/persons/{person.Id}", TestCancellationToken);
        var content = await ReadContentAsync<PersonDetailDto>(getResponse);
        content.FirstName.Should().Be("Max");
        content.LastName.Should().Be("Musterfrau");
        content.Height.Should().Be(185M);
        content.Weight.Should().Be(person.Weight);
        content.Email.Should().Be("max@reiter.at");
    }

    [Fact]
    public async ValueTask UpdatePerson_NewAddressAndRoles_ReplacesAddressAndRoles()
    {
        var person = TestData.CreatePerson();
        await SeedAsync(person);
        var request = new UpdatePersonRequest(null, null, null, null, null, null, null, null,
                                              TestData.CreateAddressRequest("Hauptplatz 1", "Linz", "4020"),
                                              [RoleName.Equestrian, RoleName.Saddler]);

        var response = await ApiClient.PatchAsJsonAsync($"api/persons/{person.Id}", request, JsonOptions,
                                                        TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var getResponse = await ApiClient.GetAsync($"api/persons/{person.Id}", TestCancellationToken);
        var content = await ReadContentAsync<PersonDetailDto>(getResponse);
        content.Address.CityName.Should().Be("Linz");
        content.Address.AddressName.Should().Be("Hauptplatz 1");
        content.Roles.Should().BeEquivalentTo([RoleName.Equestrian, RoleName.Saddler]);
    }

    [Fact]
    public async ValueTask UpdatePerson_NonExistingPerson_ReturnsNotFound()
    {
        var request = new UpdatePersonRequest("Moritz", null, null, null, null, null, null, null, null, null);

        var response = await ApiClient.PatchAsJsonAsync($"api/persons/{TestData.NonExistingId}", request,
                                                        JsonOptions, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask UpdatePerson_InvalidId_ReturnsBadRequest()
    {
        var request = new UpdatePersonRequest("Moritz", null, null, null, null, null, null, null, null, null);

        var response = await ApiClient.PatchAsJsonAsync("api/persons/-1", request, JsonOptions,
                                                        TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask UpdatePerson_InvalidHeight_ReturnsBadRequest()
    {
        var person = TestData.CreatePerson();
        await SeedAsync(person);
        var request = new UpdatePersonRequest(null, null, 0M, null, null, null, null, null, null, null);

        var response = await ApiClient.PatchAsJsonAsync($"api/persons/{person.Id}", request, JsonOptions,
                                                        TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask UpdatePerson_EmailOfOtherPerson_ReturnsConflict()
    {
        const string TakenEmail = "taken@reiter.at";
        var person = TestData.CreatePerson("Max");
        await SeedAsync(person, TestData.CreatePerson("Moritz", email: TakenEmail));
        var request = new UpdatePersonRequest(null, null, null, null, null, TakenEmail, null, null, null, null);

        var response = await ApiClient.PatchAsJsonAsync($"api/persons/{person.Id}", request, JsonOptions,
                                                        TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask DeletePerson_ExistingPerson_RemovesPerson()
    {
        var person = TestData.CreatePerson();
        await SeedAsync(person);

        var response = await ApiClient.DeleteAsync($"api/persons/{person.Id}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var getResponse = await ApiClient.GetAsync($"api/persons/{person.Id}", TestCancellationToken);
        ShouldBeProblem(getResponse, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask DeletePerson_NonExistingPerson_ReturnsNotFound()
    {
        var response = await ApiClient.DeleteAsync($"api/persons/{TestData.NonExistingId}", TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask DeletePerson_InvalidId_ReturnsBadRequest()
    {
        var response = await ApiClient.DeleteAsync("api/persons/-1", TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask DeletePerson_SaddlerWithContactsAndFavourites_RemovesSaddlerAndRelationships()
    {
        var equestrian = TestData.CreatePerson("Max");
        var saddler = TestData.CreatePerson("Sabine", "Sattler", role: RoleName.Saddler);
        await SeedAsync(TestData.CreateRelationship(equestrian, saddler, isContact: true, isFavourite: true));

        var response = await ApiClient.DeleteAsync($"api/persons/{saddler.Id}", TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var contactsResponse = await ApiClient.GetAsync($"api/persons/{equestrian.Id}/contacts",
                                                        TestCancellationToken);
        var contacts = await ReadContentAsync<ContactListResponse>(contactsResponse);
        contacts.Contacts.Should().BeEmpty();
    }

    [Fact]
    public async ValueTask CreatePerson_SaddlerWithoutStreet_ReturnsBadRequest()
    {
        var request = TestData.CreateAddPersonRequest(role: RoleName.Saddler,
                                                      address: TestData.CreateAddressRequest(addressName: null));

        var response = await ApiClient.PostAsJsonAsync("api/persons", request, JsonOptions, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask UpdateAddress_SaddlerWithoutStreet_ReturnsBadRequest()
    {
        var saddler = TestData.CreatePerson(role: RoleName.Saddler);
        await SeedAsync(saddler);
        var request = TestData.CreateAddressRequest(addressName: null, cityName: "Traun", plz: "4050");

        var response = await ApiClient.PutAsJsonAsync($"api/persons/{saddler.Id}/location", request, JsonOptions,
                                                      TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask UpdatePerson_EquestrianWithoutStreetBecomesSaddler_ReturnsBadRequest()
    {
        var equestrian = TestData.CreatePerson();
        await SeedAsync(equestrian);
        var request = new UpdatePersonRequest(null, null, null, null, null, null, null, null, null,
                                              [RoleName.Saddler]);

        var response = await ApiClient.PatchAsJsonAsync($"api/persons/{equestrian.Id}", request, JsonOptions,
                                                        TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async ValueTask AddFavourite_EquestrianAndSaddler_SaddlerAppearsInFavourites()
    {
        var equestrian = TestData.CreatePerson("Max");
        var saddler = TestData.CreatePerson("Sabine", "Sattler", role: RoleName.Saddler);
        await SeedAsync(equestrian, saddler);

        var response = await ApiClient.PutAsync($"api/persons/{equestrian.Id}/favourites/{saddler.Id}", null,
                                                TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var favourites = await ReadContentAsync<SaddlerListResponse>(
            await ApiClient.GetAsync($"api/persons/{equestrian.Id}/favourites", TestCancellationToken));
        favourites.Saddlers.Should().ContainSingle().Which.Id.Should().Be(saddler.Id);
        var contacts = await ReadContentAsync<ContactListResponse>(
            await ApiClient.GetAsync($"api/persons/{equestrian.Id}/contacts", TestCancellationToken));
        contacts.Contacts.Should().BeEmpty();
    }

    [Fact]
    public async ValueTask AddFavourite_CalledTwice_StaysSingleFavourite()
    {
        var equestrian = TestData.CreatePerson("Max");
        var saddler = TestData.CreatePerson("Sabine", "Sattler", role: RoleName.Saddler);
        await SeedAsync(equestrian, saddler);

        await ApiClient.PutAsync($"api/persons/{equestrian.Id}/favourites/{saddler.Id}", null, TestCancellationToken);
        var response = await ApiClient.PutAsync($"api/persons/{equestrian.Id}/favourites/{saddler.Id}", null,
                                                TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var favourites = await ReadContentAsync<SaddlerListResponse>(
            await ApiClient.GetAsync($"api/persons/{equestrian.Id}/favourites", TestCancellationToken));
        favourites.Saddlers.Should().ContainSingle();
    }

    [Fact]
    public async ValueTask AddFavourite_SaddlerMarksEquestrian_ReturnsConflict()
    {
        var equestrian = TestData.CreatePerson("Max");
        var saddler = TestData.CreatePerson("Sabine", "Sattler", role: RoleName.Saddler);
        await SeedAsync(equestrian, saddler);

        var response = await ApiClient.PutAsync($"api/persons/{saddler.Id}/favourites/{equestrian.Id}", null,
                                                TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask AddFavourite_NonExistingSaddler_ReturnsNotFound()
    {
        var equestrian = TestData.CreatePerson();
        await SeedAsync(equestrian);

        var response = await ApiClient.PutAsync($"api/persons/{equestrian.Id}/favourites/{TestData.NonExistingId}",
                                                null, TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask RemoveFavourite_FavouriteAndContact_RemovesFavouriteButKeepsContact()
    {
        var equestrian = TestData.CreatePerson("Max");
        var saddler = TestData.CreatePerson("Sabine", "Sattler", role: RoleName.Saddler);
        await SeedAsync(TestData.CreateRelationship(equestrian, saddler, isContact: true, isFavourite: true));

        var response = await ApiClient.DeleteAsync($"api/persons/{equestrian.Id}/favourites/{saddler.Id}",
                                                   TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var favourites = await ReadContentAsync<SaddlerListResponse>(
            await ApiClient.GetAsync($"api/persons/{equestrian.Id}/favourites", TestCancellationToken));
        favourites.Saddlers.Should().BeEmpty();
        var contacts = await ReadContentAsync<ContactListResponse>(
            await ApiClient.GetAsync($"api/persons/{equestrian.Id}/contacts", TestCancellationToken));
        contacts.Contacts.Should().ContainSingle().Which.Id.Should().Be(saddler.Id);
    }

    [Fact]
    public async ValueTask RemoveFavourite_NoFavourite_ReturnsNotFound()
    {
        var equestrian = TestData.CreatePerson("Max");
        var saddler = TestData.CreatePerson("Sabine", "Sattler", role: RoleName.Saddler);
        await SeedAsync(equestrian, saddler);

        var response = await ApiClient.DeleteAsync($"api/persons/{equestrian.Id}/favourites/{saddler.Id}",
                                                   TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask AddContact_SaddlerAddsEquestrian_BothSeeEachOtherAsContact()
    {
        var equestrian = TestData.CreatePerson("Max");
        var saddler = TestData.CreatePerson("Sabine", "Sattler", role: RoleName.Saddler);
        await SeedAsync(equestrian, saddler);

        var response = await ApiClient.PutAsync($"api/persons/{saddler.Id}/contacts/{equestrian.Id}", null,
                                                TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var saddlerContacts = await ReadContentAsync<ContactListResponse>(
            await ApiClient.GetAsync($"api/persons/{saddler.Id}/contacts", TestCancellationToken));
        saddlerContacts.Contacts.Should().ContainSingle().Which.Id.Should().Be(equestrian.Id);
        var equestrianContacts = await ReadContentAsync<ContactListResponse>(
            await ApiClient.GetAsync($"api/persons/{equestrian.Id}/contacts", TestCancellationToken));
        equestrianContacts.Contacts.Should().ContainSingle().Which.Id.Should().Be(saddler.Id);
    }

    [Fact]
    public async ValueTask AddContact_TwoSaddlers_ReturnsConflict()
    {
        var saddler = TestData.CreatePerson("Sabine", "Sattler", role: RoleName.Saddler);
        var otherSaddler = TestData.CreatePerson("Sepp", "Sattlerei", role: RoleName.Saddler);
        await SeedAsync(saddler, otherSaddler);

        var response = await ApiClient.PutAsync($"api/persons/{saddler.Id}/contacts/{otherSaddler.Id}", null,
                                                TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async ValueTask AddContact_NonExistingPerson_ReturnsNotFound()
    {
        var saddler = TestData.CreatePerson(role: RoleName.Saddler);
        await SeedAsync(saddler);

        var response = await ApiClient.PutAsync($"api/persons/{saddler.Id}/contacts/{TestData.NonExistingId}", null,
                                                TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async ValueTask RemoveContact_FromEquestrianSide_RemovesContactForBoth()
    {
        var equestrian = TestData.CreatePerson("Max");
        var saddler = TestData.CreatePerson("Sabine", "Sattler", role: RoleName.Saddler);
        await SeedAsync(TestData.CreateRelationship(equestrian, saddler, isContact: true));

        var response = await ApiClient.DeleteAsync($"api/persons/{equestrian.Id}/contacts/{saddler.Id}",
                                                   TestCancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var saddlerContacts = await ReadContentAsync<ContactListResponse>(
            await ApiClient.GetAsync($"api/persons/{saddler.Id}/contacts", TestCancellationToken));
        saddlerContacts.Contacts.Should().BeEmpty();
    }

    [Fact]
    public async ValueTask RemoveContact_NoContact_ReturnsNotFound()
    {
        var equestrian = TestData.CreatePerson("Max");
        var saddler = TestData.CreatePerson("Sabine", "Sattler", role: RoleName.Saddler);
        await SeedAsync(equestrian, saddler);

        var response = await ApiClient.DeleteAsync($"api/persons/{saddler.Id}/contacts/{equestrian.Id}",
                                                   TestCancellationToken);

        ShouldBeProblem(response, HttpStatusCode.NotFound);
    }
}