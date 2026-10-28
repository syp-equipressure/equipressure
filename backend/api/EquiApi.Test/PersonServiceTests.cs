using EquiApi.Core.Services;
using EquiApi.Persistence.Model;
using EquiApi.Persistence.Util;
using EquiApi.Test.Util;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OneOf.Types;

namespace EquiApi.Test;

public sealed class PersonServiceTests
{
    private const int PersonId = 1;

    private readonly IUnitOfWork _uowMock = Substitute.For<IUnitOfWork>();
    private readonly PersonService _sut;

    public PersonServiceTests()
    {
        _sut = new PersonService(_uowMock, NullLogger<PersonService>.Instance);

        _uowMock.PersonRepository.GetRolesByNameAsync(Arg.Any<IReadOnlyCollection<RoleName>>())
                .Returns(call => call.Arg<IReadOnlyCollection<RoleName>>().Select(TestData.CreateRole).ToList());
        _uowMock.PersonRepository.GetRelationshipsOfPersonAsync(Arg.Any<int>())
                .Returns(new List<PersonRelationship>());
    }

    [Fact]
    public async Task GetPersonByIdAsync_PersonNotFound_ReturnsNotFound()
    {
        _uowMock.PersonRepository.GetPersonByIdAsync(PersonId).Returns((Person?) null);

        var result = await _sut.GetPersonByIdAsync(PersonId);

        result.Value.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task GetPersonByIdAsync_PersonExists_ReturnsPerson()
    {
        var person = TestData.CreatePerson(PersonId);
        _uowMock.PersonRepository.GetPersonByIdAsync(PersonId).Returns(person);

        var result = await _sut.GetPersonByIdAsync(PersonId);

        result.Value.Should().BeOfType<Success<Person>>().Which.Value.Should().Be(person);
    }

    [Fact]
    public async Task GetPersonAddressAsync_PersonNotFound_ReturnsNotFound()
    {
        _uowMock.PersonRepository.GetPersonAddressAsync(PersonId).Returns((Address?) null);

        var result = await _sut.GetPersonAddressAsync(PersonId);

        result.Value.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task GetPersonAddressAsync_PersonExists_ReturnsAddress()
    {
        var address = TestData.CreateAddress();
        _uowMock.PersonRepository.GetPersonAddressAsync(PersonId).Returns(address);

        var result = await _sut.GetPersonAddressAsync(PersonId);

        result.Value.Should().BeOfType<Success<Address>>().Which.Value.Should().Be(address);
    }

    [Fact]
    public async Task UpdatePersonAddressAsync_PersonNotFound_ReturnsNotFound()
    {
        _uowMock.PersonRepository.GetPersonByIdAsync(PersonId).Returns((Person?) null);

        var result = await _sut.UpdatePersonAddressAsync(PersonId, TestData.CreateAddressData());

        result.Value.Should().BeOfType<NotFound>();
        _uowMock.LocationRepository.DidNotReceive().AddAddress(Arg.Any<Address>());
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task UpdatePersonAddressAsync_AddressExists_SetsExistingAddressAndSaves()
    {
        var person = TestData.CreatePerson(PersonId);
        var existingAddress = TestData.CreateAddress(5, "Hauptplatz 1", "4020", "Linz");
        _uowMock.PersonRepository.GetPersonByIdAsync(PersonId).Returns(person);
        _uowMock.LocationRepository.FindAddressAsync("Hauptplatz 1", "4020", "Linz").Returns(existingAddress);

        var result = await _sut.UpdatePersonAddressAsync(PersonId,
                                                         TestData.CreateAddressData("Hauptplatz 1", "4020", "Linz"));

        result.Value.Should().BeOfType<Success<Address>>().Which.Value.Should().Be(existingAddress);
        person.Address.Should().Be(existingAddress);
        _uowMock.LocationRepository.DidNotReceive().AddAddress(Arg.Any<Address>());
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task UpdatePersonAddressAsync_AddressDoesNotExist_AddsAddressAndSaves()
    {
        var person = TestData.CreatePerson(PersonId);
        _uowMock.PersonRepository.GetPersonByIdAsync(PersonId).Returns(person);
        _uowMock.LocationRepository.FindAddressAsync("Hauptplatz 1", "4020", "Linz").Returns((Address?) null);

        var result = await _sut.UpdatePersonAddressAsync(PersonId,
                                                         TestData.CreateAddressData("Hauptplatz 1", "4020", "Linz"));

        var address = result.Value.Should().BeOfType<Success<Address>>().Which.Value;
        address.AddressName.Should().Be("Hauptplatz 1");
        address.PLZ.Should().Be("4020");
        address.CityName.Should().Be("Linz");
        person.Address.Should().Be(address);
        _uowMock.LocationRepository.Received(1).AddAddress(address);
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task GetNameByIdAsync_PersonNotFound_ReturnsNotFound()
    {
        _uowMock.PersonRepository.GetNameByIdAsync(PersonId).Returns((NameData?) null);

        var result = await _sut.GetNameByIdAsync(PersonId);

        result.Value.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task GetNameByIdAsync_PersonExists_ReturnsName()
    {
        var name = new NameData("Max", "Mustermann");
        _uowMock.PersonRepository.GetNameByIdAsync(PersonId).Returns(name);

        var result = await _sut.GetNameByIdAsync(PersonId);

        result.Value.Should().BeOfType<Success<NameData>>().Which.Value.Should().Be(name);
    }

    [Fact]
    public async Task GetAllSaddlersAsync_ReturnsSaddlersOfRepository()
    {
        var saddlers = new List<SaddlerData> { CreateSaddler(2), CreateSaddler(3) };
        _uowMock.PersonRepository.GetAllSaddlersAsync().Returns(saddlers);

        var result = await _sut.GetAllSaddlersAsync();

        result.Should().Equal(saddlers);
    }

    [Fact]
    public async Task GetFavouriteSaddlersAsync_PersonNotFound_ReturnsNotFound()
    {
        _uowMock.PersonRepository.PersonExistsAsync(PersonId).Returns(false);

        var result = await _sut.GetFavouriteSaddlersAsync(PersonId);

        result.Value.Should().BeOfType<NotFound>();
        await _uowMock.PersonRepository.DidNotReceive().GetFavouriteSaddlersAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task GetFavouriteSaddlersAsync_NoFavourites_ReturnsEmptyList()
    {
        _uowMock.PersonRepository.PersonExistsAsync(PersonId).Returns(true);
        _uowMock.PersonRepository.GetFavouriteSaddlersAsync(PersonId).Returns(new List<SaddlerData>());

        var result = await _sut.GetFavouriteSaddlersAsync(PersonId);

        result.Value.Should().BeOfType<Success<IReadOnlyCollection<SaddlerData>>>().Which.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetFavouriteSaddlersAsync_HasFavourites_ReturnsFavourites()
    {
        var saddlers = new List<SaddlerData> { CreateSaddler(2) };
        _uowMock.PersonRepository.PersonExistsAsync(PersonId).Returns(true);
        _uowMock.PersonRepository.GetFavouriteSaddlersAsync(PersonId).Returns(saddlers);

        var result = await _sut.GetFavouriteSaddlersAsync(PersonId);

        result.Value.Should().BeOfType<Success<IReadOnlyCollection<SaddlerData>>>()
              .Which.Value.Should().Equal(saddlers);
    }

    [Fact]
    public async Task GetContactsAsync_PersonNotFound_ReturnsNotFound()
    {
        _uowMock.PersonRepository.PersonExistsAsync(PersonId).Returns(false);

        var result = await _sut.GetContactsAsync(PersonId);

        result.Value.Should().BeOfType<NotFound>();
        await _uowMock.PersonRepository.DidNotReceive().GetContactsAsync(Arg.Any<int>());
    }

    [Fact]
    public async Task GetContactsAsync_NoContacts_ReturnsEmptyList()
    {
        _uowMock.PersonRepository.PersonExistsAsync(PersonId).Returns(true);
        _uowMock.PersonRepository.GetContactsAsync(PersonId).Returns(new List<ContactData>());

        var result = await _sut.GetContactsAsync(PersonId);

        result.Value.Should().BeOfType<Success<IReadOnlyCollection<ContactData>>>().Which.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetContactsAsync_HasContacts_ReturnsContacts()
    {
        var contacts = new List<ContactData>
        {
            new(2, "Sabine", "Sattler", null, "Steyr", "4400", "https://sattler.example.com", "Custom saddles"),
            new(3, "Anna", "Huber", null, "Linz", "4020", null, null)
        };
        _uowMock.PersonRepository.PersonExistsAsync(PersonId).Returns(true);
        _uowMock.PersonRepository.GetContactsAsync(PersonId).Returns(contacts);

        var result = await _sut.GetContactsAsync(PersonId);

        result.Value.Should().BeOfType<Success<IReadOnlyCollection<ContactData>>>()
              .Which.Value.Should().Equal(contacts);
    }

    [Fact]
    public async Task AddPersonAsync_EmailTaken_ReturnsEmailAlreadyTaken()
    {
        const string Email = "taken@example.com";
        _uowMock.PersonRepository.IsEmailTakenAsync(Email).Returns(true);

        var result = await _sut.AddPersonAsync(TestData.CreateNewPersonData(Email));

        result.Value.Should().BeOfType<IPersonService.EmailAlreadyTaken>();
        _uowMock.PersonRepository.DidNotReceive().AddPerson(Arg.Any<Person>());
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AddPersonAsync_ValidData_AddsPersonWithRoleAndSaves()
    {
        var data = TestData.CreateNewPersonData(role: RoleName.Saddler);

        var result = await _sut.AddPersonAsync(data);

        var person = result.Value.Should().BeOfType<Success<Person>>().Which.Value;
        person.FirstName.Should().Be(data.FirstName);
        person.LastName.Should().Be(data.LastName);
        person.Height.Should().Be(data.Height);
        person.Weight.Should().Be(data.Weight);
        person.DateOfBirth.Should().Be(data.DateOfBirth);
        person.Email.Should().Be(data.Email);
        person.Roles.Should().ContainSingle().Which.Role.Name.Should().Be(RoleName.Saddler);
        _uowMock.PersonRepository.Received(1).AddPerson(person);
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task AddPersonAsync_AddressExists_ReusesAddress()
    {
        var address = TestData.CreateAddress();
        _uowMock.LocationRepository.FindAddressAsync(null, "4400", "Steyr").Returns(address);

        var result = await _sut.AddPersonAsync(TestData.CreateNewPersonData());

        result.Value.Should().BeOfType<Success<Person>>().Which.Value.Address.Should().Be(address);
        _uowMock.LocationRepository.DidNotReceive().AddAddress(Arg.Any<Address>());
    }

    [Fact]
    public async Task AddPersonAsync_AddressDoesNotExist_AddsAddress()
    {
        _uowMock.LocationRepository.FindAddressAsync(null, "4400", "Steyr").Returns((Address?) null);

        var result = await _sut.AddPersonAsync(TestData.CreateNewPersonData());

        var person = result.Value.Should().BeOfType<Success<Person>>().Which.Value;
        person.Address.CityName.Should().Be("Steyr");
        _uowMock.LocationRepository.Received(1).AddAddress(person.Address);
    }

    [Fact]
    public async Task AddPersonAsync_RoleNotSeeded_Throws()
    {
        _uowMock.PersonRepository.GetRolesByNameAsync(Arg.Any<IReadOnlyCollection<RoleName>>())
                .Returns(new List<AccountRole>());

        var act = async () => await _sut.AddPersonAsync(TestData.CreateNewPersonData());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdatePersonAsync_PersonNotFound_ReturnsNotFound()
    {
        _uowMock.PersonRepository.GetPersonByIdAsync(PersonId).Returns((Person?) null);

        var result = await _sut.UpdatePersonAsync(PersonId, new PersonChanges(FirstName: "New"));

        result.Value.Should().BeOfType<NotFound>();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task UpdatePersonAsync_EmailTakenByAnotherPerson_ReturnsEmailAlreadyTaken()
    {
        const string Email = "taken@example.com";
        _uowMock.PersonRepository.GetPersonByIdAsync(PersonId).Returns(TestData.CreatePerson(PersonId));
        _uowMock.PersonRepository.IsEmailTakenAsync(Email, PersonId).Returns(true);

        var result = await _sut.UpdatePersonAsync(PersonId, new PersonChanges(Email: Email));

        result.Value.Should().BeOfType<IPersonService.EmailAlreadyTaken>();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task UpdatePersonAsync_NoChanges_KeepsPersonUnchanged()
    {
        var person = TestData.CreatePerson(PersonId);
        var expected = TestData.CreatePerson(PersonId);
        _uowMock.PersonRepository.GetPersonByIdAsync(PersonId).Returns(person);

        var result = await _sut.UpdatePersonAsync(PersonId, new PersonChanges());

        result.Value.Should().BeOfType<Success>();
        person.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task UpdatePersonAsync_SomeChanges_UpdatesOnlyTheseValuesAndSaves()
    {
        var person = TestData.CreatePerson(PersonId);
        _uowMock.PersonRepository.GetPersonByIdAsync(PersonId).Returns(person);

        var result = await _sut.UpdatePersonAsync(PersonId, new PersonChanges(LastName: "Musterfrau", Height: 185M,
                                                                              WebsiteLink: "https://example.com"));

        result.Value.Should().BeOfType<Success>();
        person.FirstName.Should().Be("Max");
        person.LastName.Should().Be("Musterfrau");
        person.Height.Should().Be(185M);
        person.Weight.Should().Be(75M);
        person.WebsiteLink.Should().Be("https://example.com");
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task UpdatePersonAsync_WithAddress_SetsFoundAddress()
    {
        var person = TestData.CreatePerson(PersonId);
        var newAddress = TestData.CreateAddress(5, "Hauptplatz 1", "4020", "Linz");
        _uowMock.PersonRepository.GetPersonByIdAsync(PersonId).Returns(person);
        _uowMock.LocationRepository.FindAddressAsync("Hauptplatz 1", "4020", "Linz").Returns(newAddress);

        var result = await _sut.UpdatePersonAsync(PersonId, new PersonChanges(
                                                      Address: TestData.CreateAddressData("Hauptplatz 1", "4020",
                                                                                          "Linz")));

        result.Value.Should().BeOfType<Success>();
        person.Address.Should().Be(newAddress);
    }

    [Fact]
    public async Task UpdatePersonAsync_WithRoles_ReplacesRoles()
    {
        var person = TestData.CreatePerson(PersonId, address: TestData.CreateAddress(addressName: "Werkstattgasse 1"));
        person.Roles = [new PersonRoleAssignment { Role = TestData.CreateRole(RoleName.Equestrian), RoleId = 10 }];
        _uowMock.PersonRepository.GetPersonByIdAsync(PersonId).Returns(person);

        var result = await _sut.UpdatePersonAsync(PersonId, new PersonChanges(Roles: [RoleName.Saddler]));

        result.Value.Should().BeOfType<Success>();
        person.Roles.Should().ContainSingle().Which.Role.Name.Should().Be(RoleName.Saddler);
    }

    [Fact]
    public async Task UpdatePersonAsync_WithExistingRole_KeepsExistingAssignment()
    {
        var person = TestData.CreatePerson(PersonId, address: TestData.CreateAddress(addressName: "Werkstattgasse 1"));
        var existingAssignment = new PersonRoleAssignment { Role = TestData.CreateRole(RoleName.Equestrian), RoleId = 10 };
        person.Roles = [existingAssignment];
        _uowMock.PersonRepository.GetPersonByIdAsync(PersonId).Returns(person);

        var result = await _sut.UpdatePersonAsync(PersonId,
                                                  new PersonChanges(Roles: [RoleName.Equestrian, RoleName.Saddler]));

        result.Value.Should().BeOfType<Success>();
        person.Roles.Should().HaveCount(2).And.Contain(existingAssignment);
    }

    [Fact]
    public async Task UpdatePersonAsync_WithEmptyRoles_KeepsExistingRoles()
    {
        var person = TestData.CreatePerson(PersonId);
        var existingAssignment = new PersonRoleAssignment { Role = TestData.CreateRole(RoleName.Equestrian), RoleId = 10 };
        person.Roles = [existingAssignment];
        _uowMock.PersonRepository.GetPersonByIdAsync(PersonId).Returns(person);

        var result = await _sut.UpdatePersonAsync(PersonId, new PersonChanges(Roles: []));

        result.Value.Should().BeOfType<Success>();
        person.Roles.Should().ContainSingle().Which.Should().Be(existingAssignment);
    }

    [Fact]
    public async Task DeletePersonAsync_PersonNotFound_ReturnsNotFound()
    {
        _uowMock.PersonRepository.GetPersonByIdAsync(PersonId).Returns((Person?) null);

        var result = await _sut.DeletePersonAsync(PersonId);

        result.Value.Should().BeOfType<NotFound>();
        _uowMock.PersonRepository.DidNotReceive().RemovePerson(Arg.Any<Person>());
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task DeletePersonAsync_PersonExists_RemovesPersonAndSaves()
    {
        var person = TestData.CreatePerson(PersonId);
        _uowMock.PersonRepository.GetPersonByIdAsync(PersonId).Returns(person);

        var result = await _sut.DeletePersonAsync(PersonId);

        result.Value.Should().BeOfType<Success>();
        _uowMock.PersonRepository.Received(1).RemovePerson(person);
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task DeletePersonAsync_PersonWithRelationships_RemovesRelationshipsAndPerson()
    {
        const int SaddlerId = 2;
        var saddler = TestData.CreatePerson(SaddlerId, role: RoleName.Saddler);
        var relationship = new PersonRelationship { EquestrianId = PersonId, SaddlerId = SaddlerId, IsContact = true };
        _uowMock.PersonRepository.GetPersonByIdAsync(SaddlerId).Returns(saddler);
        _uowMock.PersonRepository.GetRelationshipsOfPersonAsync(SaddlerId).Returns(new List<PersonRelationship> { relationship });

        var result = await _sut.DeletePersonAsync(SaddlerId);

        result.Value.Should().BeOfType<Success>();
        _uowMock.PersonRepository.Received(1).RemoveRelationship(relationship);
        _uowMock.PersonRepository.Received(1).RemovePerson(saddler);
    }

    [Fact]
    public async Task UpdatePersonAddressAsync_SaddlerWithoutStreet_ReturnsStreetRequired()
    {
        var saddler = TestData.CreatePerson(PersonId, role: RoleName.Saddler);
        _uowMock.PersonRepository.GetPersonByIdAsync(PersonId).Returns(saddler);

        var result = await _sut.UpdatePersonAddressAsync(PersonId, TestData.CreateAddressData(addressName: null));

        result.Value.Should().BeOfType<IPersonService.StreetRequiredForSaddler>();
        saddler.Address.AddressName.Should().Be("Werkstattgasse 1");
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task UpdatePersonAsync_SaddlerRemovesStreet_ReturnsStreetRequired()
    {
        _uowMock.PersonRepository.GetPersonByIdAsync(PersonId)
                .Returns(TestData.CreatePerson(PersonId, role: RoleName.Saddler));

        var result = await _sut.UpdatePersonAsync(PersonId,
                                                  new PersonChanges(Address: TestData.CreateAddressData(addressName: null)));

        result.Value.Should().BeOfType<IPersonService.StreetRequiredForSaddler>();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task UpdatePersonAsync_EquestrianWithoutStreetBecomesSaddler_ReturnsStreetRequired()
    {
        _uowMock.PersonRepository.GetPersonByIdAsync(PersonId)
                .Returns(TestData.CreatePerson(PersonId, role: RoleName.Equestrian));

        var result = await _sut.UpdatePersonAsync(PersonId, new PersonChanges(Roles: [RoleName.Saddler]));

        result.Value.Should().BeOfType<IPersonService.StreetRequiredForSaddler>();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AddFavouriteAsync_SaddlerNotFound_ReturnsNotFound()
    {
        const int SaddlerId = 2;
        _uowMock.PersonRepository.GetPersonByIdAsync(PersonId)
                .Returns(TestData.CreatePerson(PersonId, role: RoleName.Equestrian));
        _uowMock.PersonRepository.GetPersonByIdAsync(SaddlerId).Returns((Person?) null);

        var result = await _sut.AddFavouriteAsync(PersonId, SaddlerId);

        result.Value.Should().BeOfType<NotFound>();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Theory]
    [InlineData(RoleName.Equestrian, RoleName.Equestrian)]
    [InlineData(RoleName.Saddler, RoleName.Saddler)]
    [InlineData(RoleName.Saddler, RoleName.Equestrian)]
    public async Task AddFavouriteAsync_NotEquestrianAndSaddler_ReturnsInvalidRelationship(RoleName personRole,
                                                                                           RoleName otherRole)
    {
        const int OtherId = 2;
        _uowMock.PersonRepository.GetPersonByIdAsync(PersonId).Returns(TestData.CreatePerson(PersonId, role: personRole));
        _uowMock.PersonRepository.GetPersonByIdAsync(OtherId).Returns(TestData.CreatePerson(OtherId, role: otherRole));

        var result = await _sut.AddFavouriteAsync(PersonId, OtherId);

        result.Value.Should().BeOfType<IPersonService.InvalidRelationship>();
        _uowMock.PersonRepository.DidNotReceive().AddRelationship(Arg.Any<PersonRelationship>());
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task AddFavouriteAsync_NoRelationshipYet_AddsFavourite()
    {
        const int SaddlerId = 2;
        var equestrian = TestData.CreatePerson(PersonId, role: RoleName.Equestrian);
        var saddler = TestData.CreatePerson(SaddlerId, role: RoleName.Saddler);
        _uowMock.PersonRepository.GetPersonByIdAsync(PersonId).Returns(equestrian);
        _uowMock.PersonRepository.GetPersonByIdAsync(SaddlerId).Returns(saddler);
        _uowMock.PersonRepository.GetRelationshipAsync(PersonId, SaddlerId).Returns((PersonRelationship?) null);

        var result = await _sut.AddFavouriteAsync(PersonId, SaddlerId);

        result.Value.Should().BeOfType<Success>();
        _uowMock.PersonRepository.Received(1).AddRelationship(Arg.Is<PersonRelationship>(r =>
            r.Equestrian == equestrian && r.Saddler == saddler && r.IsFavourite && !r.IsContact));
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task AddFavouriteAsync_AlreadyContact_MarksExistingRelationshipAsFavourite()
    {
        const int SaddlerId = 2;
        var relationship = new PersonRelationship { EquestrianId = PersonId, SaddlerId = SaddlerId, IsContact = true };
        _uowMock.PersonRepository.GetPersonByIdAsync(PersonId)
                .Returns(TestData.CreatePerson(PersonId, role: RoleName.Equestrian));
        _uowMock.PersonRepository.GetPersonByIdAsync(SaddlerId)
                .Returns(TestData.CreatePerson(SaddlerId, role: RoleName.Saddler));
        _uowMock.PersonRepository.GetRelationshipAsync(PersonId, SaddlerId).Returns(relationship);

        var result = await _sut.AddFavouriteAsync(PersonId, SaddlerId);

        result.Value.Should().BeOfType<Success>();
        relationship.IsFavourite.Should().BeTrue();
        relationship.IsContact.Should().BeTrue();
        _uowMock.PersonRepository.DidNotReceive().AddRelationship(Arg.Any<PersonRelationship>());
    }

    [Fact]
    public async Task RemoveFavouriteAsync_NoFavourite_ReturnsNotFound()
    {
        const int SaddlerId = 2;
        var relationship = new PersonRelationship { EquestrianId = PersonId, SaddlerId = SaddlerId, IsContact = true };
        _uowMock.PersonRepository.GetRelationshipAsync(PersonId, SaddlerId).Returns(relationship);

        var result = await _sut.RemoveFavouriteAsync(PersonId, SaddlerId);

        result.Value.Should().BeOfType<NotFound>();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task RemoveFavouriteAsync_AlsoContact_KeepsRelationshipAsContact()
    {
        const int SaddlerId = 2;
        var relationship = new PersonRelationship
        {
            EquestrianId = PersonId,
            SaddlerId = SaddlerId,
            IsContact = true,
            IsFavourite = true
        };
        _uowMock.PersonRepository.GetRelationshipAsync(PersonId, SaddlerId).Returns(relationship);

        var result = await _sut.RemoveFavouriteAsync(PersonId, SaddlerId);

        result.Value.Should().BeOfType<Success>();
        relationship.IsFavourite.Should().BeFalse();
        relationship.IsContact.Should().BeTrue();
        _uowMock.PersonRepository.DidNotReceive().RemoveRelationship(Arg.Any<PersonRelationship>());
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task RemoveFavouriteAsync_OnlyFavourite_RemovesRelationship()
    {
        const int SaddlerId = 2;
        var relationship = new PersonRelationship { EquestrianId = PersonId, SaddlerId = SaddlerId, IsFavourite = true };
        _uowMock.PersonRepository.GetRelationshipAsync(PersonId, SaddlerId).Returns(relationship);

        var result = await _sut.RemoveFavouriteAsync(PersonId, SaddlerId);

        result.Value.Should().BeOfType<Success>();
        _uowMock.PersonRepository.Received(1).RemoveRelationship(relationship);
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task AddContactAsync_EquestrianAddsSaddler_AddsContactWithEquestrianAndSaddlerSide()
    {
        const int SaddlerId = 2;
        var equestrian = TestData.CreatePerson(PersonId, role: RoleName.Equestrian);
        var saddler = TestData.CreatePerson(SaddlerId, role: RoleName.Saddler);
        _uowMock.PersonRepository.GetPersonByIdAsync(PersonId).Returns(equestrian);
        _uowMock.PersonRepository.GetPersonByIdAsync(SaddlerId).Returns(saddler);

        var result = await _sut.AddContactAsync(PersonId, SaddlerId);

        result.Value.Should().BeOfType<Success>();
        await _uowMock.PersonRepository.Received(1).MarkAsContactAsync(equestrian, saddler);
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task AddContactAsync_SaddlerAddsEquestrian_AddsContactWithEquestrianAndSaddlerSide()
    {
        const int EquestrianId = 2;
        var saddler = TestData.CreatePerson(PersonId, role: RoleName.Saddler);
        var equestrian = TestData.CreatePerson(EquestrianId, role: RoleName.Equestrian);
        _uowMock.PersonRepository.GetPersonByIdAsync(PersonId).Returns(saddler);
        _uowMock.PersonRepository.GetPersonByIdAsync(EquestrianId).Returns(equestrian);

        var result = await _sut.AddContactAsync(PersonId, EquestrianId);

        result.Value.Should().BeOfType<Success>();
        // the saddler added the contact, but the sides stay equestrian and saddler
        await _uowMock.PersonRepository.Received(1).MarkAsContactAsync(equestrian, saddler);
    }

    [Fact]
    public async Task AddContactAsync_FavouriteExistsInOtherDirection_MarksExistingRelationshipAsContact()
    {
        const int EquestrianId = 2;
        var relationship = new PersonRelationship { EquestrianId = EquestrianId, SaddlerId = PersonId, IsFavourite = true };
        _uowMock.PersonRepository.GetPersonByIdAsync(PersonId)
                .Returns(TestData.CreatePerson(PersonId, role: RoleName.Saddler));
        _uowMock.PersonRepository.GetPersonByIdAsync(EquestrianId)
                .Returns(TestData.CreatePerson(EquestrianId, role: RoleName.Equestrian));
        _uowMock.PersonRepository.GetRelationshipAsync(PersonId, EquestrianId).Returns((PersonRelationship?) null);
        _uowMock.PersonRepository.GetRelationshipAsync(EquestrianId, PersonId).Returns(relationship);

        var result = await _sut.AddContactAsync(PersonId, EquestrianId);

        result.Value.Should().BeOfType<Success>();
        relationship.IsContact.Should().BeTrue();
        relationship.IsFavourite.Should().BeTrue();
        await _uowMock.PersonRepository.DidNotReceive().MarkAsContactAsync(Arg.Any<Person>(), Arg.Any<Person>());
    }

    [Theory]
    [InlineData(RoleName.Equestrian, RoleName.Equestrian)]
    [InlineData(RoleName.Saddler, RoleName.Saddler)]
    [InlineData(RoleName.Guest, RoleName.Saddler)]
    public async Task AddContactAsync_NotEquestrianAndSaddler_ReturnsInvalidRelationship(RoleName personRole,
                                                                                         RoleName otherRole)
    {
        const int OtherId = 2;
        _uowMock.PersonRepository.GetPersonByIdAsync(PersonId).Returns(TestData.CreatePerson(PersonId, role: personRole));
        _uowMock.PersonRepository.GetPersonByIdAsync(OtherId).Returns(TestData.CreatePerson(OtherId, role: otherRole));

        var result = await _sut.AddContactAsync(PersonId, OtherId);

        result.Value.Should().BeOfType<IPersonService.InvalidRelationship>();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task RemoveContactAsync_NoContact_ReturnsNotFound()
    {
        var result = await _sut.RemoveContactAsync(PersonId, 2);

        result.Value.Should().BeOfType<NotFound>();
        await _uowMock.DidNotReceive().SaveChangesAsync();
    }

    [Fact]
    public async Task RemoveContactAsync_FromSaddlerSide_RemovesRelationship()
    {
        const int EquestrianId = 2;
        var relationship = new PersonRelationship { EquestrianId = EquestrianId, SaddlerId = PersonId, IsContact = true };
        _uowMock.PersonRepository.GetRelationshipAsync(PersonId, EquestrianId).Returns((PersonRelationship?) null);
        _uowMock.PersonRepository.GetRelationshipAsync(EquestrianId, PersonId).Returns(relationship);

        var result = await _sut.RemoveContactAsync(PersonId, EquestrianId);

        result.Value.Should().BeOfType<Success>();
        _uowMock.PersonRepository.Received(1).RemoveRelationship(relationship);
        await _uowMock.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task RemoveContactAsync_AlsoFavourite_KeepsRelationshipAsFavourite()
    {
        const int SaddlerId = 2;
        var relationship = new PersonRelationship
        {
            EquestrianId = PersonId,
            SaddlerId = SaddlerId,
            IsContact = true,
            IsFavourite = true
        };
        _uowMock.PersonRepository.GetRelationshipAsync(PersonId, SaddlerId).Returns(relationship);

        var result = await _sut.RemoveContactAsync(PersonId, SaddlerId);

        result.Value.Should().BeOfType<Success>();
        relationship.IsContact.Should().BeFalse();
        relationship.IsFavourite.Should().BeTrue();
        _uowMock.PersonRepository.DidNotReceive().RemoveRelationship(Arg.Any<PersonRelationship>());
    }

    private static SaddlerData CreateSaddler(int id) =>
        new(id, "Sabine", "Sattler", null, "Steyr", "4400", "https://sattler.example.com", "Custom saddles");
}